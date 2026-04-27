using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SFXPlayer.classes
{
    /// <summary>
    /// Thread-safe file logger for SFX Player.
    /// Writes timestamped entries to a rolling log file in
    /// %LOCALAPPDATA%\SFXPlayer\Logs and mirrors every entry to
    /// Debug.WriteLine so the output is visible in the VS debugger.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;

        static AppLogger()
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SFXPlayer", "Logs");
                Directory.CreateDirectory(logDir);
                // One file per calendar day – keeps the directory tidy.
                string fileName = $"SFXPlayer_{DateTime.Now:yyyy-MM-dd}.log";
                _logFilePath = Path.Combine(logDir, fileName);
            }
            catch
            {
                // If we cannot create the log directory (e.g. highly restricted environment)
                // fall back to the temp directory so we never throw from the logger itself.
                _logFilePath = Path.Combine(Path.GetTempPath(), "SFXPlayer.log");
            }
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Logs an informational message.</summary>
        public static void Info(string message) => Write("INFO ", message, null);

        /// <summary>Logs a warning message.</summary>
        public static void Warning(string message) => Write("WARN ", message, null);

        /// <summary>Logs an error message with the full exception stack trace.</summary>
        public static void Error(string message, Exception ex = null) => Write("ERROR", message, ex);

        /// <summary>Returns the full path of the current log file.</summary>
        public static string LogFilePath => _logFilePath;

        // ------------------------------------------------------------------ //
        //  Implementation                                                      //
        // ------------------------------------------------------------------ //

        private static void Write(string level, string message, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.Append(" [");
            sb.Append(level);
            sb.Append("] ");
            sb.Append(message);

            if (ex != null)
            {
                sb.AppendLine();
                sb.Append("  Exception: ");
                sb.Append(ex.GetType().FullName);
                sb.Append(": ");
                sb.AppendLine(ex.Message);
                if (ex.StackTrace != null)
                {
                    sb.Append("  StackTrace: ");
                    sb.AppendLine(ex.StackTrace);
                }
                // Walk the inner-exception chain so nothing is silently lost.
                Exception inner = ex.InnerException;
                int depth = 0;
                while (inner != null && depth < 5)
                {
                    sb.Append("  ---> ");
                    sb.Append(inner.GetType().FullName);
                    sb.Append(": ");
                    sb.AppendLine(inner.Message);
                    if (inner.StackTrace != null)
                    {
                        sb.Append("       ");
                        sb.AppendLine(inner.StackTrace);
                    }
                    inner = inner.InnerException;
                    depth++;
                }
            }

            string entry = sb.ToString();
            Debug.WriteLine(entry);

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, entry + Environment.NewLine);
                }
                catch
                {
                    // Swallow – we must never let the logger crash the application.
                }
            }
        }
    }
}
