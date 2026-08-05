using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace AURA.Mobile
{
    /// <summary>
    /// Sistema de log de bootstrap para o AURA Android.
    ///
    /// Objetivo: registrar o máximo de informação desde o PRIMEIRO frame de execução
    /// do processo, antes mesmo de qualquer Activity existir. Ele faz duas coisas:
    ///
    ///  1. Escreve para o logcat com a tag "AURA" (visível em adb logcat ou apps
    ///     como Logcat Reader / MatLog sem precisar de root).
    ///  2. Grava num arquivo de texto em FilesDir (pasta privada) e também em
    ///     GetExternalFilesDir (Android/data/... , legível pelo usuário via
    ///     gerenciador de arquivos / MTP) com rotação diária.
    ///
    /// Instala os handlers globais de exceção o mais cedo possível:
    ///   - AppDomain.CurrentDomain.UnhandledException
    ///   - TaskScheduler.UnobservedTaskException
    ///   - AndroidEnvironment.UnhandledExceptionRaiser
    ///   - Java.Lang.Thread.DefaultUncaughtExceptionHandler (exceções JVM nativas)
    ///
    /// Nenhum método lança: logging nunca pode derrubar o app.
    /// </summary>
    public static class AuraLog
    {
        private const string LogcatTag = "AURA";

        private static readonly object Sync = new object();
        private static readonly StringBuilder PendingBuffer = new StringBuilder(8192);
        private static string _filePath = string.Empty;
        private static bool _fileReady;

        private static Java.Lang.Thread.IUncaughtExceptionHandler? _previousUncaughtHandler;

        /// <summary>Inicializa o caminho do arquivo de log (contexto disponível).</summary>
        public static void Init(Context context)
        {
            try
            {
                lock (Sync)
                {
                    if (_fileReady)
                    {
                        return;
                    }

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

                        // Descarrega tudo o que foi logado antes do init no arquivo.
                        if (PendingBuffer.Length > 0)
                        {
                            File.AppendAllText(_filePath, PendingBuffer.ToString());
                            PendingBuffer.Clear();
                        }
                    }
                }
            }
            catch
            {
                // Logging nunca pode derrubar o app.
            }
        }

        /// <summary>Instala os handlers globais de exceção (chamar no início do OnCreate).</summary>
        public static void WireGlobalExceptionHandlers()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                {
                    if (args.ExceptionObject is Exception ex)
                    {
                        Exception("AppDomain.UnhandledException", ex);
                    }
                    else
                    {
                        Error("AppDomain.UnhandledException (não-Exception): " + (args.ExceptionObject?.ToString() ?? "null"));
                    }
                };

                TaskScheduler.UnobservedTaskException += (_, args) =>
                {
                    Exception("TaskScheduler.UnobservedTaskException", args.Exception);
                };

                AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
                {
                    Exception("AndroidEnvironment.UnhandledExceptionRaiser", args.Exception);
                };

                // Exceções lançadas no lado Java/JVM (e.g. dentro de callbacks nativos)
                // chegam aqui. Guardamos o handler anterior (do runtime) para delegar.
                _previousUncaughtHandler = Java.Lang.Thread.DefaultUncaughtExceptionHandler;
                Java.Lang.Thread.DefaultUncaughtExceptionHandler = new AuraUncaughtExceptionHandler(_previousUncaughtHandler);
            }
            catch
            {
                // Logging nunca pode derrubar o app.
            }
        }

        public static void Info(string message) => Write("INFO ", message);
        public static void Warning(string message) => Write("WARN ", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Fatal(string message) => Write("FATAL", message);

        public static void Exception(string where, Exception? ex)
        {
            if (ex == null)
            {
                Exception(where, new Exception("null"));
                return;
            }

            Write("EXCPT", string.Format("{0}: {1}", where, ex));

            Exception? inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth < 10)
            {
                Write("EXCPT", string.Format("  inner[{0}]: {1}", depth, inner));
                inner = inner.InnerException;
                depth++;
            }
        }

        private static void Write(string level, string message)
        {
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}",
                DateTime.Now, level, message);

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
                }
                else
                {
                    PendingBuffer.AppendLine(line);
                    if (PendingBuffer.Length > 32768)
                    {
                        PendingBuffer.Remove(0, 16384);
                    }
                }
            }

            // Sempre tenta o logcat, independente do arquivo.
            try
            {
                if (level == "ERROR" || level == "FATAL" || level == "EXCPT")
                {
                    Log.Error(LogcatTag, line);
                }
                else if (level == "WARN ")
                {
                    Log.Warn(LogcatTag, line);
                }
                else
                {
                    Log.Info(LogcatTag, line);
                }
            }
            catch
            {
            }

            // stdout/stderr vão para o logcat como mono-stdout/mono-stderr (debug).
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

                // Delega para o handler original do runtime (mono/.NET) para manter
                // o comportamento de crash padrão.
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
