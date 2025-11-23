using System;
using System.Windows;

namespace ContentSyncApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Globale Exception-Handler einrichten
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                MessageBox.Show(
                    $"Ein unerwarteter Fehler ist aufgetreten:\n\n{exception?.Message ?? "Unbekannter Fehler"}\n\n" +
                    "Die Anwendung wird beendet.",
                    "Kritischer Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    $"Ein Fehler ist aufgetreten:\n\n{args.Exception.Message}\n\n" +
                    "Sie können versuchen fortzufahren.",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                args.Handled = true;
            };
        }
    }
}
