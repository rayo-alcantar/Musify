using System.Configuration;
using System.Data;
using System.Windows;

namespace Intento4
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // mostrará cualquier error futuro en un cuadro
            this.DispatcherUnhandledException += (s, ev) =>
            {
                MessageBox.Show(ev.Exception.ToString(),
                                "Excepción no controlada",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                ev.Handled = true;
            };

            base.OnStartup(e);
        }
    }
}
