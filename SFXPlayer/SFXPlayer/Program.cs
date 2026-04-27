using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SFXPlayer.classes;
using SFXPlayer.Properties;

namespace SFXPlayer {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            AppLogger.Info("Application starting");

            // Catch any unhandled exceptions on the UI thread.
            Application.ThreadException += (s, e) =>
                AppLogger.Error("Unhandled UI thread exception", e.Exception);

            // Catch unhandled exceptions on background threads.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                AppLogger.Error("Unhandled AppDomain exception", e.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SplashScreen ss = new SplashScreen();
#if !DEBUG
            ss.Show();
            ss.Refresh();
#endif  
            Settings.Default.Upgrade();
            AppLogger.Info($"Log file: {AppLogger.LogFilePath}");
            mainForm = new SFXPlayer();
            Application.Run(mainForm);
            AppLogger.Info("Application exiting");
        }

        public static SFXPlayer mainForm;
    }
}
