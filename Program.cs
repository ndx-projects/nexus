using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace NdxNexus
{
    internal static class Program
    {
        // Fouten die anders onzichtbaar blijven komen in %TEMP%\<naam>-fout.log terecht.
        private static string LogFile { get { return Path.Combine(Path.GetTempPath(), Edition.LogName + "-fout.log"); } }

        private static void LogError(string where, Exception ex)
        {
            try
            {
                File.AppendAllText(LogFile,
                    DateTime.Now.ToString("s") + "  " + where + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception) { }
        }

        private static void Fail(string where, Exception ex)
        {
            LogError(where, ex);
            try { MessageBox.Show(where + ":\n\n" + ex.Message + "\n\n(" + LogFile + ")", Edition.Name); }
            catch (Exception) { }
        }

        [STAThread]
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e)
            {
                Exception ex = e.ExceptionObject as Exception;
                if (ex != null) Fail(L.T("err.unexpected"), ex);
            };
            try
            {
                return Run(args);
            }
            catch (Exception ex)
            {
                Fail(L.T("err.startup"), ex);
                return 2;
            }
        }

        private static int Run(string[] args)
        {
            string self = Environment.ProcessPath;
            Payload payload = Payload.TryOpen(self);
            string dir = Path.GetDirectoryName(self);
            bool silent = args.Any(a => a.Equals("/S", StringComparison.OrdinalIgnoreCase));

            // taal: systeemtaal, te overschrijven met /LANG=nl|en|de|fr|hr
            L.Set(L.Detect());
            foreach (string a in args)
            {
                if (a.StartsWith("/LANG=", StringComparison.OrdinalIgnoreCase)) L.Set(a.Substring(6));
            }

            // 1. Installer-modus: er hangt een payload aan deze exe
            if (payload != null && silent) return InstallEngine.RunSilent(payload, args);

            InstallLog log = payload == null ? UninstallEngine.Load(dir) : null;
            bool uninstall = log != null || args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase));
            if (log != null && !string.IsNullOrEmpty(log.Lang)) L.Set(log.Lang);

            if (log != null && silent)
            {
                UninstallEngine.Run(log, self, delegate { });
                return 0;
            }

            Application app = new Application();

            // vormgeving (Nexus wereldwijd): uit de setup zelf, of uit de map naast de verwijderaar
            // (pas na het maken van Application, want de lettertypen gebruiken pack-adressen)
            if (payload != null) Theme.ApplyFromPayload(payload);
            else if (log != null) Theme.ApplyFromDir(log.Dir, log.Name);
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            app.DispatcherUnhandledException += delegate (object s, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                Fail(L.T("err.generic"), e.Exception);
                e.Handled = true;
            };

            app.Dispatcher.BeginInvoke(new Action(async delegate
            {
                try
                {
                    if (payload != null)
                    {
                        if (payload.Manifest.PerMachine && !InstallEngine.IsAdmin())
                        {
                            // opnieuw starten met beheerdersrechten
                            try
                            {
                                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(self);
                                psi.UseShellExecute = true;
                                psi.Verb = "runas";
                                System.Diagnostics.Process.Start(psi);
                            }
                            catch (Exception) { }
                            app.Shutdown();
                            return;
                        }
                        InstallerWindow win = new InstallerWindow(payload);
                        await SplashWindow.ShowAsync(payload.Manifest.Name, L.T("version", payload.Manifest.Version), false, 1800);
                        app.MainWindow = win;
                        win.Show();
                        win.Activate();
                    }
                    else if (uninstall && log != null)
                    {
                        UninstallerWindow uw = new UninstallerWindow(log, self);
                        app.MainWindow = uw;
                        uw.Show();
                        uw.Activate();
                    }
                    else if (uninstall)
                    {
                        MessageBox.Show(L.T("un.nolog"), Edition.Name);
                        app.Shutdown();
                        return;
                    }
                    else
                    {
                        await SplashWindow.ShowAsync(Edition.Name, "", true, 2200);
                        StudioWindow sw = new StudioWindow();
                        app.MainWindow = sw;
                        sw.Show();
                    }
                    app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }
                catch (Exception ex)
                {
                    Fail(L.T("err.startup"), ex);
                    app.Shutdown();
                }
            }));
            return app.Run();
        }
    }
}
