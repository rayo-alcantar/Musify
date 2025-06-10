using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Intento4
{
    /// <summary>
    /// Lógica de interacción para InicioPage.xaml
    /// </summary>
    public partial class InicioPage : Page
    {
        private MainWindow reproductor; // Almacena la instancia de MainWindow

        public InicioPage(MainWindow mainWindow)
        {
            InitializeComponent();
            reproductor = mainWindow; // Guardar la instancia de MainWindow
        }

        private void JusticeButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Justice, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new Justice(reproductor));
        }
        //UVSTButton_Click
        private void UVSTButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Bad Bunny, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new UVST(reproductor));
        }

        private void _1989Button_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Taylor, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new _1989(reproductor));
        }

        private void SabrinaButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Sabrina, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new Sabrina(reproductor));
        }

        private void RauwButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Rauw, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new Rauw(reproductor));
        }
        private void JadenButton_Click(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Rauw, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new Jaden(reproductor));
        }

       
    }


}
