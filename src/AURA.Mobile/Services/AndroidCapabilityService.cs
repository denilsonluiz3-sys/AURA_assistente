#if ANDROID
using System.Text;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Provider;
using AURA.Abstractions;
using BatteryStatus = Android.OS.BatteryStatus;
using Stream = Android.Media.Stream;

namespace AURA.Mobile.Services;

/// <summary>
/// Implementação Android das capacidades expostas à camada de agentes.
/// </summary>
public sealed class AndroidCapabilityService : IAndroidCapabilityService
{
    private readonly Context _context;

    public AndroidCapabilityService(Context context)
    {
        _context = context?.ApplicationContext ?? throw new ArgumentNullException(nameof(context));
    }

    public string GetBattery()
    {
        try
        {
            using var intent = _context.RegisterReceiver(null, new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged));
            if (intent == null)
                return "Bateria: indisponível.";

            int level = intent.GetIntExtra("level", -1);
            int scale = intent.GetIntExtra("scale", -1);
            int status = intent.GetIntExtra("status", -1);
            int percentage = level >= 0 && scale > 0 ? level * 100 / scale : -1;
            string charging = status switch
            {
                (int)BatteryStatus.Charging => "carregando",
                (int)BatteryStatus.Full => "completa",
                _ => "não carregando"
            };

            return percentage >= 0
                ? $"Bateria: {percentage}% ({charging})."
                : $"Bateria: nível indisponível ({charging}).";
        }
        catch (Exception ex)
        {
            return Failure("bateria", ex);
        }
    }

    public string GetLight() => ReadSensor(SensorType.Light, "Luz");
    public string GetAccelerometer() => ReadSensor(SensorType.Accelerometer, "Acelerômetro");
    public string GetGyroscope() => ReadSensor(SensorType.Gyroscope, "Giroscópio");

    public string GetMagnetometer()
    {
        try
        {
            var manager = (SensorManager?)_context.GetSystemService(Context.SensorService);
            var sensor = manager?.GetDefaultSensor(SensorType.MagneticField);
            if (sensor == null)
                return "Magnetômetro: sensor não disponível.";

            return $"Magnetômetro: disponível ({sensor.Name}), vendor={sensor.Vendor}, max={sensor.MaximumRange:F1} µT.";
        }
        catch (Exception ex)
        {
            return Failure("magnetômetro", ex);
        }
    }

    public string SampleMagnetometer(int durationMs = 400)
    {
        try
        {
            float[] mags = SampleMagnetometerMagnitudes(durationMs);
            if (mags.Length < 4)
                return "Magnetômetro: poucas amostras (" + mags.Length + ").";

            float mean = mags.Average();
            float min = mags.Min();
            float max = mags.Max();
            float sampleRate = mags.Length / Math.Max(0.05f, durationMs / 1000f);
            float? hz = SignalAnalysis.EstimateHzZeroCrossing(mags, sampleRate);
            string? hint = SignalAnalysis.HintMainsHz(hz);

            return $"Magnetômetro amostra ({mags.Length} pts, ~{sampleRate:F0} Hz): " +
                   $"mag média={mean:F2} µT, min={min:F2}, max={max:F2}" +
                   (hint is not null ? $", freq={hint}" : "") + ".";
        }
        catch (Exception ex)
        {
            return Failure("amostra magnetômetro", ex);
        }
    }

    /// <summary>
    /// Coleta magnitudes √(x²+y²+z²) por durationMs usando SENSOR_DELAY_FASTEST.
    /// </summary>
    public float[] SampleMagnetometerMagnitudes(int durationMs = 400)
    {
        var manager = (SensorManager?)_context.GetSystemService(Context.SensorService);
        var sensor = manager?.GetDefaultSensor(SensorType.MagneticField);
        if (manager == null || sensor == null)
            return Array.Empty<float>();

        int ms = Math.Clamp(durationMs, 50, 3000);
        var list = new List<float>(256);
        var listener = new MagListener(list);

        try
        {
            manager.RegisterListener(listener, sensor, SensorDelay.Fastest);
            // bloqueia a thread atual por um pouco — chamado de timer UI em SpectrumPage
            Thread.Sleep(ms);
        }
        finally
        {
            try { manager.UnregisterListener(listener); } catch { /* ignore */ }
        }

        return list.ToArray();
    }

    public string GetLocation()
    {
        try
        {
            var manager = (global::Android.Locations.LocationManager?)_context.GetSystemService(Context.LocationService);
            if (manager == null)
                return "Localização: serviço indisponível.";

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M &&
                _context.CheckSelfPermission(Android.Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted &&
                _context.CheckSelfPermission(Android.Manifest.Permission.AccessCoarseLocation) != Android.Content.PM.Permission.Granted)
            {
                return "Localização: permissão não concedida.";
            }

            global::Android.Locations.Location? best = null;
            foreach (string provider in manager.GetProviders(true))
            {
                try
                {
                    var location = manager.GetLastKnownLocation(provider);
                    if (location != null && (best == null || location.Time > best.Time))
                        best = location;
                }
                catch { }
            }

            return best == null
                ? "Localização: nenhum ponto conhecido disponível."
                : $"Localização: latitude={best.Latitude:F6}, longitude={best.Longitude:F6}, precisão={best.Accuracy:F1}m.";
        }
        catch (Exception ex)
        {
            return Failure("localização", ex);
        }
    }

    public string GetCameras()
    {
        try
        {
            var manager = (Android.Hardware.Camera2.CameraManager?)_context.GetSystemService(Context.CameraService);
            if (manager == null)
                return "Câmeras: serviço indisponível.";

            var ids = manager.GetCameraIdList();
            return ids.Length == 0
                ? "Câmeras: nenhuma câmera encontrada."
                : $"Câmeras: {ids.Length} encontrada(s). IDs: {string.Join(", ", ids)}.";
        }
        catch (Exception ex)
        {
            return Failure("câmeras", ex);
        }
    }

    public string GetAudio()
    {
        try
        {
            var audio = (AudioManager?)_context.GetSystemService(Context.AudioService);
            if (audio == null)
                return "Áudio: serviço indisponível.";

            return $"Áudio: modo={audio.Mode}, música={audio.GetStreamVolume(Stream.Music)}/{audio.GetStreamMaxVolume(Stream.Music)}, chamada={audio.GetStreamVolume(Stream.VoiceCall)}/{audio.GetStreamMaxVolume(Stream.VoiceCall)}.";
        }
        catch (Exception ex)
        {
            return Failure("áudio", ex);
        }
    }

    public string GetBluetooth()
    {
        try
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
            if (adapter == null)
                return "Bluetooth: não suportado.";

            return $"Bluetooth: {(adapter.IsEnabled ? "ativado" : "desativado")}, nome={adapter.Name ?? "indisponível"}.";
        }
        catch (Exception ex)
        {
            return Failure("Bluetooth", ex);
        }
    }

    public string GetClipboard()
    {
        try
        {
            var clipboard = (ClipboardManager?)_context.GetSystemService(Context.ClipboardService);
            if (clipboard == null || !clipboard.HasPrimaryClip)
                return "Área de transferência: vazia.";

            var clip = clipboard.PrimaryClip;
            if (clip == null || clip.ItemCount == 0)
                return "Área de transferência: vazia.";

            return $"Área de transferência: {clip.GetItemAt(0)?.CoerceToText(_context) ?? string.Empty}";
        }
        catch (Exception ex)
        {
            return Failure("área de transferência", ex);
        }
    }

    public string SetClipboard(string text)
    {
        try
        {
            var clipboard = (ClipboardManager?)_context.GetSystemService(Context.ClipboardService);
            if (clipboard == null)
                return "Área de transferência: serviço indisponível.";

            clipboard.PrimaryClip = ClipData.NewPlainText("AURA", text ?? string.Empty);
            return "Área de transferência atualizada.";
        }
        catch (Exception ex)
        {
            return Failure("área de transferência", ex);
        }
    }

    public string Notify(string title, string body)
    {
        try
        {
            const string channelId = "aura_capabilities";
            var manager = (NotificationManager?)_context.GetSystemService(Context.NotificationService);
            if (manager == null)
                return "Notificação: serviço indisponível.";

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "AURA", NotificationImportance.Default);
                manager.CreateNotificationChannel(channel);
            }

            var builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? new Notification.Builder(_context, channelId)
                : new Notification.Builder(_context);

            builder.SetContentTitle(title ?? "AURA")
                .SetContentText(body ?? string.Empty)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true);

            manager.Notify(1001, builder.Build());
            return "Notificação enviada.";
        }
        catch (Exception ex)
        {
            return Failure("notificação", ex);
        }
    }

    public string Vibrate(int ms)
    {
        try
        {
            int duration = Math.Clamp(ms, 1, 10000);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var vibrator = (Vibrator?)_context.GetSystemService(Context.VibratorService);
                if (vibrator == null) return "Vibração: serviço indisponível.";
                vibrator.Vibrate(VibrationEffect.CreateOneShot(duration, VibrationEffect.DefaultAmplitude));
            }
            else
            {
#pragma warning disable CS0618
                var vibrator = (Vibrator?)_context.GetSystemService(Context.VibratorService);
                if (vibrator == null) return "Vibração: serviço indisponível.";
                vibrator.Vibrate(duration);
#pragma warning restore CS0618
            }

            return $"Vibração executada por {duration} ms.";
        }
        catch (Exception ex)
        {
            return Failure("vibração", ex);
        }
    }

    public string GetNetwork()
    {
        try
        {
            var manager = (ConnectivityManager?)_context.GetSystemService(Context.ConnectivityService);
            var info = manager?.ActiveNetworkInfo;
            return info == null
                ? "Rede: sem conexão ativa."
                : $"Rede: conectado={info.IsConnected}, tipo={info.TypeName}, subtipo={info.SubtypeName}.";
        }
        catch (Exception ex)
        {
            return Failure("rede", ex);
        }
    }

    public string GetDevice() =>
        $"Dispositivo: fabricante={Build.Manufacturer}, modelo={Build.Model}, marca={Build.Brand}, Android={Build.VERSION.Release} (API {Build.VERSION.SdkInt}).";

    public string GetApps()
    {
        try
        {
            var packages = _context.PackageManager?.GetInstalledApplications(Android.Content.PM.PackageInfoFlags.MetaData);
            if (packages == null)
                return "Aplicativos: indisponíveis.";

            var names = packages
                .Select(p => p.PackageName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Take(200)
                .ToArray();

            return $"Aplicativos ({names.Length} exibidos):\n{string.Join("\n", names)}";
        }
        catch (Exception ex)
        {
            return Failure("aplicativos", ex);
        }
    }

    public string GetAppCatalog()
    {
        try
        {
            var pm = _context.PackageManager;
            if (pm == null)
                return "Catálogo de apps: indisponível.";

            var packages = pm.GetInstalledApplications(Android.Content.PM.PackageInfoFlags.MetaData);
            if (packages == null)
                return "Catálogo de apps: vazio.";

            var sb = new StringBuilder();
            int count = 0;
            foreach (var p in packages)
            {
                if (string.IsNullOrWhiteSpace(p.PackageName)) continue;
                string label = pm.GetApplicationLabel(p) ?? p.PackageName;
                bool launchable = pm.GetLaunchIntentForPackage(p.PackageName) != null;
                sb.AppendLine($"{label}|{p.PackageName}|{(launchable ? "sim" : "não")}");
                count++;
            }
            return $"Catálogo ({count} apps):\n{sb}";
        }
        catch (Exception ex)
        {
            return Failure("catálogo de apps", ex);
        }
    }

    public string FindApp(string query)
    {
        try
        {
            var pm = _context.PackageManager;
            if (pm == null)
                return "ERRO: PackageManager indisponivel.";

            if (string.IsNullOrWhiteSpace(query))
                return "ERRO: informe o nome do app.";

            string q = query.Trim().ToLowerInvariant();
            var packages = pm.GetInstalledApplications(Android.Content.PM.PackageInfoFlags.MetaData);
            if (packages == null)
                return "App não encontrado: " + query;

            var matches = new List<string>();
            foreach (var p in packages)
            {
                if (string.IsNullOrWhiteSpace(p.PackageName)) continue;
                string label = pm.GetApplicationLabel(p) ?? p.PackageName;
                if (label.ToLowerInvariant().Contains(q) ||
                    p.PackageName.ToLowerInvariant().Contains(q))
                {
                    bool launchable = pm.GetLaunchIntentForPackage(p.PackageName) != null;
                    matches.Add($"{label}|{p.PackageName}|{(launchable ? "sim" : "não")}");
                }
            }

            if (matches.Count == 0)
                return "App não encontrado: " + query;

            return $"Encontrados ({matches.Count}):\n" + string.Join("\n", matches);
        }
        catch (Exception ex)
        {
            return Failure("buscar app " + query, ex);
        }
    }

    public string LaunchApp(string packageName)
    {
        try
        {
            var pm = _context.PackageManager;
            if (pm == null)
                return "ERRO: PackageManager indisponivel.";

            var intent = pm.GetLaunchIntentForPackage(packageName);
            if (intent == null)
                return "App nao pode ser aberto: " + packageName;

            intent.AddFlags(ActivityFlags.NewTask);
            _context.StartActivity(intent);
            return "App aberto: " + packageName;
        }
        catch (Exception ex)
        {
            return Failure("abrir app " + packageName, ex);
        }
    }

    public string GetProperties() =>
        $"Propriedades: SDK={Build.VERSION.SdkInt}; release={Build.VERSION.Release}; fabricante={Build.Manufacturer}; modelo={Build.Model}; produto={Build.Product}; dispositivo={Build.Device}; hardware={Build.Hardware}.";

    public string GetMemory()
    {
        try
        {
            var manager = (ActivityManager?)_context.GetSystemService(Context.ActivityService);
            if (manager == null) return "Memória: serviço indisponível.";

            var info = new ActivityManager.MemoryInfo();
            manager.GetMemoryInfo(info);
            return $"Memória: total={FormatBytes(info.TotalMem)}; disponível={FormatBytes(info.AvailMem)}; baixa={info.LowMemory}.";
        }
        catch (Exception ex)
        {
            return Failure("memória", ex);
        }
    }

    public string GetStorage()
    {
        try
        {
            var stat = new StatFs(global::Android.OS.Environment.ExternalStorageDirectory?.Path ?? _context.FilesDir?.Path ?? "/data");
            return $"Armazenamento: total={FormatBytes(stat.TotalBytes)}; disponível={FormatBytes(stat.AvailableBytes)}.";
        }
        catch (Exception ex)
        {
            return Failure("armazenamento", ex);
        }
    }

    public string GetAll()
    {
        var sb = new StringBuilder();
        sb.AppendLine(GetDevice());
        sb.AppendLine(GetBattery());
        sb.AppendLine(GetNetwork());
        sb.AppendLine(GetMemory());
        sb.AppendLine(GetStorage());
        sb.AppendLine(GetAudio());
        sb.AppendLine(GetBluetooth());
        sb.AppendLine(GetCameras());
        sb.AppendLine(GetLight());
        sb.AppendLine(GetAccelerometer());
        sb.AppendLine(GetGyroscope());
        sb.AppendLine(GetMagnetometer());
        sb.AppendLine(SampleMagnetometer(300));
        return sb.ToString().TrimEnd();
    }

    private string ReadSensor(SensorType type, string name)
    {
        try
        {
            var manager = (SensorManager?)_context.GetSystemService(Context.SensorService);
            var sensor = manager?.GetDefaultSensor(type);
            if (sensor == null)
                return $"{name}: sensor não disponível.";

            return $"{name}: disponível ({sensor.Name}), vendor={sensor.Vendor}, versão={sensor.Version}, consumo={sensor.Power:F2}mA.";
        }
        catch (Exception ex)
        {
            return Failure(name, ex);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KiB", "MiB", "GiB", "TiB" };
        double value = Math.Max(0, bytes);
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:F1} {units[unit]}";
    }

    private static string Failure(string operation, Exception ex) =>
        $"{operation}: indisponível ({ex.GetType().Name}).";

    private sealed class MagListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly List<float> _mags;

        public MagListener(List<float> mags) => _mags = mags;

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count < 3)
                return;
            float x = e.Values[0];
            float y = e.Values[1];
            float z = e.Values[2];
            _mags.Add(MathF.Sqrt(x * x + y * y + z * z));
        }
    }
}
#endif
