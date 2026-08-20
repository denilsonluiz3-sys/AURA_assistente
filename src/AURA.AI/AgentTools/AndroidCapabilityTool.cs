using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Mobile.Services;

namespace AURA.AI
{
    public sealed class AndroidCapabilityTool : AgentTool
    {
        private readonly IAndroidCapabilityService _service;

        public AndroidCapabilityTool(IAndroidCapabilityService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "android",
            Description = "Acessa APIs nativas do Android: battery, light, accelerometer, gyroscope, magnetometer, location, camera, audio, bluetooth, clipboard, notification, vibrate, network, device, apps, properties, memory, storage, all",
            Parameters =
            {
                ["action"] = new AgentToolParameter { Type = "string", Description = "Acao a executar" },
                ["text"] = new AgentToolParameter { Type = "string", Description = "Texto para clipboard ou notification" },
                ["milliseconds"] = new AgentToolParameter { Type = "string", Description = "Duracao da vibracao em ms" }
            },
            Required = { "action" }
        };

        public override Task<string> ExecuteAsync(string argsJson, CancellationToken ct = default)
        {
            string action = "", text = "";
            int ms = 500;

            try
            {
                using var doc = JsonDocument.Parse(argsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("action", out var a)) action = a.GetString()?.ToLowerInvariant() ?? "";
                if (root.TryGetProperty("text", out var t)) text = t.GetString() ?? "";
                if (root.TryGetProperty("milliseconds", out var m)) int.TryParse(m.GetString(), out ms);
            }
            catch { return Task.FromResult("ERRO: JSON invalido"); }

            if (string.IsNullOrEmpty(action)) return Task.FromResult("ERRO: acao nao especificada");

            try
            {
                string result = action switch
                {
                    "battery" => _service.GetBattery(),
                    "light" => _service.GetLight(),
                    "accelerometer" => _service.GetAccelerometer(),
                    "gyroscope" => _service.GetGyroscope(),
                    "magnetometer" => _service.GetMagnetometer(),
                    "location" => _service.GetLocation(),
                    "camera" => _service.GetCameras(),
                    "audio" => _service.GetAudio(),
                    "bluetooth" => _service.GetBluetooth(),
                    "clipboard" => _service.GetClipboard(),
                    "clipboard_set" => _service.SetClipboard(text),
                    "notification" => _service.Notify("AURA", text),
                    "vibrate" => _service.Vibrate(ms),
                    "network" => _service.GetNetwork(),
                    "device" => _service.GetDevice(),
                    "apps" => _service.GetApps(),
                    "properties" => _service.GetProperties(),
                    "memory" => _service.GetMemory(),
                    "storage" => _service.GetStorage(),
                    "all" => _service.GetAll(),
                    _ => $"Acao desconhecida: {action}"
                };
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult($"ERRO: {ex.Message}");
            }
        }
    }
}
