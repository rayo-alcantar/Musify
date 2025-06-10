using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
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
    /// Lógica de interacción para ExplorarPage.xaml
    /// </summary>
    public partial class ExplorarPage : Page
    {
        private GridFSBucket gridFS;
        private MainWindow reproductor;
        public ExplorarPage(MainWindow mainWindow)
        {
            var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
            var database = client.GetDatabase("Musify");
            gridFS = new GridFSBucket(database);
            InitializeComponent();
            reproductor = mainWindow;
        }
        private void Boton_Popclick(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Rauw, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new Pop(reproductor));
        }

        private void Boton_Reggaetonclick(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Rauw, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new Reggaeton(reproductor));
        }

        private void Boton_RyBclick(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Rauw, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new RyB(reproductor));
        }
        private void Boton_Rockclick(object sender, RoutedEventArgs e)
        {
            // Navegar a la página Rauw, pasando la instancia de MainWindow
            this.NavigationService.Navigate(new Rock(reproductor));
        }

        private void Boton_APTclick(object sender, RoutedEventArgs e)
        {
            string SongID = "675140070a536382f5dafd49";

            // Convertir el string a ObjectId
            ObjectId objectId = new ObjectId(SongID);

            // Pasar el ObjectId al método
            reproductor.ReproducirCancionDesdeMongoDB(objectId);

        }

        private void Boton_DWSclick(object sender, RoutedEventArgs e)
        {
            string SongID = "675140440a536382f5dafd55";

            // Convertir el string a ObjectId
            ObjectId objectId = new ObjectId(SongID);

            // Pasar el ObjectId al método
            reproductor.ReproducirCancionDesdeMongoDB(objectId);

        }

        private void Boton_Lutherclick(object sender, RoutedEventArgs e)
        {
            string SongID = "6751408f0a536382f5dafd66";

            // Convertir el string a ObjectId
            ObjectId objectId = new ObjectId(SongID);

            // Pasar el ObjectId al método
            reproductor.ReproducirCancionDesdeMongoDB(objectId);

        }
    }
}
