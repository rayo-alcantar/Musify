using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDB.Driver;
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
    /// Lógica de interacción para Reggaeton.xaml
    /// </summary>
    public partial class Reggaeton : Page
    {
        private GridFSBucket gridFS;
        private MainWindow reproductor;
        public Reggaeton(MainWindow reproductor)
        {
            InitializeComponent();
            this.reproductor = reproductor;
            // Conectar a MongoDB
            var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
            var database = client.GetDatabase("Musify");
            gridFS = new GridFSBucket(database);

            // Obtener canciones del álbum "Justice"
            ObtenerCancionesReggaeton();
        }
        private async void ObtenerCancionesReggaeton()
        {
            try
            {

                var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.genero", "Reggaeton");
                var files = await gridFS.Find(filter).ToListAsync();



                if (files.Count == 0)
                {
                    MessageBox.Show("No se encontraron canciones Reggaeton.");
                    return;
                }

                // Transformar los datos para el Binding
                var canciones = files.Select(file => new Cancion
                {
                    titulo = file.Metadata["titulo"].AsString,
                    artista = file.Metadata["artista"].AsString,
                    album = file.Metadata["album"].AsString,
                    _id = file.Id
                }).ToList();

                // Actualizar la lista de reproducción en el reproductor
                if (reproductor != null)
                {
                    reproductor.ActualizarListaReproduccion(canciones);
                }


                // Establecer las canciones como fuente de datos del ListBox
                ListaCanciones.ItemsSource = canciones;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener canciones: {ex.Message}");
            }
        }

        private void ListaCanciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaCanciones.SelectedItem != null)
            {
                // Obtener la canción seleccionada
                var selectedSong = (Cancion)ListaCanciones.SelectedItem;

                // Obtener el ID de la canción
                ObjectId songId = selectedSong._id;

                // Reproducir la canción utilizando el reproductor actual
                if (reproductor != null)
                {
                    reproductor.ReproducirCancionDesdeMongoDB(songId);
                }
                else
                {
                    MessageBox.Show("Error: Reproductor no está inicializado.");
                }
            }
        }
    }
}

