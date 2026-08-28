using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.Content;
using Android.Provider;
using Android.Runtime;
using Android.Util;

namespace AURA.Mobile
{
    /// <summary>
    /// Sistema de log de bootstrap para o AURA Android.
    /// Redige automaticamente padrões de API key / Bearer antes de gravar.
    /// </summary>
    public static class AuraLog
    {
        private const string LogcatTag = "AURA";

        private static readonly object Sync = new object();
        private static readonly StringBuilder PendingBuffer = new StringBuilder(8192);
        private static string _filePath = string.Empty;
        private static bool _fileReady;

        private static Android.Net.Uri? _downloadUri;
        private static StreamWriter? _downloadWriter;
        private static Context? _appContext;

        private static Java.Lang.Thread.IUncaughtExceptionHandler? _previousUncaughtHandler;

        // Prefixos comuns de API keys — nunca logar valor completo
        private static readonly Regex SecretPatterns = new Regex(
            @"(?ix)
            (?:Bearer\s+)[A-Za-z0-9._\-]{8,}
            |AIza[0-9A-Za-z_\-]{10,}
            |sk-or-[A-Za-z0-9_\-]{8,}
            |gsk_[A-Za-z0-9_\-]{8,}
            |sk-[A-Za-z0-9]{16,}
            |AQ\.[A-Za-z0-9_\-]{10,}
            ",
            RegexOptions.Compiled);

        public static void Init(Context context)
        {
            try
            {
                lock (Sync)
                {
                    if (_fileReady)
                        return;

                    string baseDir =
                        context.GetExternalFilesDir(null)?.AbsolutePath
                        ?? context.FilesDir?.AbsolutePath;

                    if (!string.IsNullOrEmpty(baseDir))
                    {
                        string logsDir = Path.Combine(baseDir, "logs");
                        Directory.CreateDirectory(logsDir);

                        _filePath = Path.Combine(
                            logsDir,
                            string.Format("aura_{0:yyyyMMdd_HHmmss}.log", DateTime.Now));

                        _fileReady = true;

                        if (PendingBuffer.Length > 0)
                        {
                            File.AppendAllText(_filePath, PendingBuffer.ToString());
                            PendingBuffer.Clear();
                        }
                    }

                    _appContext = context;
                    TryCreateDownloadMirror(context);
                }
            }
            catch
            {
            }
        }

        private static void TryCreateDownloadMirror(Context context)
        {
            try
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(29))
                    return;

                string fileName = string.Format("aura_{0:yyyyMMdd_HHmmss}.log", DateTime.Now);

                var values = new ContentValues();
                values.Put(MediaStore.Downloads.InterfaceConsts.DisplayName, fileName);
                values.Put(MediaStore.Downloads.InterfaceConsts.MimeType, "text/plain");
                values.Put(MediaStore.Downloads.InterfaceConsts.RelativePath, "Download/AURA");

                Android.Net.Uri? uri =
                    context.ContentResolver.Insert(MediaStore.Downloads.ExternalContentUri, values);

                if (uri == null)
                    return;

                Stream? stream = context.ContentResolver.OpenOutputStream(uri, "wa");
                if (stream == null)
                    return;

                _downloadUri = uri;
                _downloadWriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                Write("INFO ", "Espelho em Download/AURA/" + fileName);
            }
            catch
            {
            }
        }

        public static void WireGlobalExceptionHandlers()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                {
                    if (args.ExceptionObject is Exception ex)
                        Exception("AppDomain.UnhandledException", ex);
                    else
                        Error("AppDomain.UnhandledException (não-Exception): " + (args.ExceptionObject?.ToString() ?? "null"));
                };

                TaskScheduler.UnobservedTaskException += (_, args) =>
                {
                    Exception("TaskScheduler.UnobservedTaskException", args.Exception);
                };

                AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
                {
                    Exception("AndroidEnvironment.UnhandledExceptionRaiser", args);
                };

                _previousUncaughtHandler = Java.Lang.Thread.DefaultUncaughtExceptionHandler;
                Java.Lang.Thread.DefaultUncaughtExceptionHandler = new AuraUncaughtExceptionHandler(_previousUncaughtHandler);
            }
            catch
            {
            }
        }

        public static void Info(string message) => Write("INFO ", message);
        public static void Warning(string message) => Write("WARN ", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Fatal(string message) => Write("FATAL", message);

        public static string LogFilePath
        {
            get
            {
                lock (Sync)
                {
                    return _filePath;
                }
            }
        }

        public static string ReadRecentLog(int maxLines = 500)
        {
            try
            {
                lock (Sync)
                {
                    if (!_fileReady || string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                        return PendingBuffer.ToString();
                }

                string[] lines = File.ReadAllLines(_filePath);
                if (lines.Length <= maxLines)
                    return string.Join(Environment.NewLine, lines);

                var sb = new StringBuilder();
                sb.AppendLine($"... (log truncado de {lines.Length} linhas, mostrando as últimas {maxLines}) ...");
                for (int i = lines.Length - maxLines; i < lines.Length; i++)
                    sb.AppendLine(lines[i]);

                return sb.ToString();
            }
            catch
            {
                return "(falha ao ler o log)";
            }
        }

        public static void Exception(string where, Exception? ex)
        {
            if (ex == null)
            {
                Exception(where, new Exception("null"));
                return;
            }

            Write("EXCPT", string.Format("{0}: {1}", where, Redact(ex.ToString())));

            Exception? inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth < 10)
            {
                Write("EXCPT", string.Format("  inner[{0}]: {1}", depth, Redact(inner.ToString())));
                inner = inner.InnerException;
                depth++;
            }
        }

        /// <summary>Remove padrões de segredo antes de persistir / logcat.</summary>
        public static string Redact(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return message ?? string.Empty;

            try
            {
                return SecretPatterns.Replace(message, "[REDACTED]");
            }
            catch
            {
                return message;
            }
        }

        private static void Write(string level, string message)
        {
            string safe = Redact(message);
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}",
                DateTime.Now, level, safe);

            lock (Sync)
            {
                if (_fileReady)
                {
                    try
                    {
                        File.AppendAllText(_filePath, line + Environment.NewLine);
                    }
                    catch
                    {
                    }

                    try
                    {
                        _downloadWriter?.WriteLine(line);
                    }
                    catch
                    {
                    }
                }
                else
                {
                    PendingBuffer.AppendLine(line);
                    if (PendingBuffer.Length > 32768)
                        PendingBuffer.Remove(0, 16384);
                }
            }

            try
            {
                if (level == "ERROR" || level == "FATAL" || level == "EXCPT")
                    Log.Error(LogcatTag, line);
                else if (level == "WARN ")
                    Log.Warn(LogcatTag, line);
                else
                    Log.Info(LogcatTag, line);
            }
            catch
            {
            }

            try
            {
                Console.WriteLine(line);
            }
            catch
            {
            }
        }

        private sealed class AuraUncaughtExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
        {
            private readonly Java.Lang.Thread.IUncaughtExceptionHandler? _next;

            public AuraUncaughtExceptionHandler(Java.Lang.Thread.IUncaughtExceptionHandler? next)
            {
                _next = next;
            }

            public void UncaughtException(Java.Lang.Thread? thread, Java.Lang.Throwable? throwable)
            {
                string threadName = thread?.Name ?? "(unknown-thread)";
                Write("JVM  ", string.Format("UncaughtException [{0}]: {1}", threadName, throwable));

                if (_next != null && !ReferenceEquals(_next, this))
                {
                    try
                    {
                        _next.UncaughtException(thread, throwable);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
