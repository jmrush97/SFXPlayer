using System;
using System.IO;
using log4net;
using log4net.Config;

namespace SFXPlayer.classes
{
    /// <summary>
    /// Application logger for SFX Player, backed by log4net.
    /// Configuration is read from the log4net section of App.config.
    /// The RollingFileAppender writes daily-rolling files to
    /// %LOCALAPPDATA%\SFXPlayer\Logs\SFXPlayer.log and a TraceAppender
    /// mirrors every entry to the VS debug output.
    /// </summary>
    public static class AppLogger
    {
        private static readonly ILog _log;

        static AppLogger()
        {
            // Read log4net configuration from App.config.
            XmlConfigurator.Configure();
            _log = LogManager.GetLogger(typeof(AppLogger));
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>Logs an informational message.</summary>
        public static void Info(string message) => _log.Info(message);

        /// <summary>Logs a warning message.</summary>
        public static void Warning(string message) => _log.Warn(message);

        /// <summary>Logs an error message. When <paramref name="ex"/> is provided
        /// log4net records the full exception type, message, and stack trace
        /// (including the complete inner-exception chain).</summary>
        public static void Error(string message, Exception ex = null)
        {
            if (ex != null)
                _log.Error(message, ex);
            else
                _log.Error(message);
        }

        /// <summary>Returns the base path of the log directory.</summary>
        public static string LogFilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SFXPlayer", "Logs", "SFXPlayer.log");
            }
        }
    }
}
