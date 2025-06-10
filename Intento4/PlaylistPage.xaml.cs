using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Intento4
{
    public partial class PlaylistPage : Page
    {
        private GridFSBucket gridFS;
        private MainWindow reproductor;

        public PlaylistPage(MainWindow reproductor)
        {
            InitializeComponent();
            this.reproductor = reproductor;

            // Conectar a MongoDB
            var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
            var database = client.GetDatabase("Musify");
            gridFS = new GridFSBucket(database);
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Limpiar las opciones previas
            optionsComboBox.Items.Clear();
            optionsComboBox.IsEnabled = true;

            // Obtener la selección del ComboBox de categorías
            var selectedCategory = (categoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Agregar las opciones correspondientes según la categoría seleccionada
            switch (selectedCategory)
            {
                case "estado_animo":
                    optionsComboBox.Items.Add("Alegre");
                    optionsComboBox.Items.Add("Triste");
                    optionsComboBox.Items.Add("Relajado");
                    optionsComboBox.Items.Add("Motivado");
                    break;

                case "actividad":
                    optionsComboBox.Items.Add("Estudiar");
                    optionsComboBox.Items.Add("Hacer Ejercicio");
                    optionsComboBox.Items.Add("Relajarse");
                    optionsComboBox.Items.Add("Fiesta");
                    break;

                case "momento_dia":
                    optionsComboBox.Items.Add("Mañana");
                    optionsComboBox.Items.Add("Tarde");
                    optionsComboBox.Items.Add("Noche");
                    break;

                default:
                    optionsComboBox.IsEnabled = false;
                    break;
            }
        }

        private async void GenerarPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (optionsComboBox.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecciona una opción para generar la playlist.");
                return;
            }

            // Obtener la opción seleccionada
            var selectedOption = optionsComboBox.SelectedItem.ToString();

            // Establecer el título de la playlist
            string selectedCategory = (categoryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string playlistTitle = $"Playlist {selectedOption}";

            // Mostrar el título de la playlist
            PlaylistTitle.Text = playlistTitle;

            // Obtener canciones desde MongoDB basadas en el filtro
            await ObtenerCancionesFiltradas(selectedCategory, selectedOption);
        }

        private async Task ObtenerCancionesFiltradas(string category, string option)
        {
            try
            {
                // Mostrar detalles del filtro
                MessageBox.Show($"Aplicando filtro: metadata.{category.ToLower()} = {option}");

                // Construir el filtro según la categoría y opción seleccionada
                var filter = Builders<GridFSFileInfo>.Filter.Eq($"metadata.{category.ToLower()}", option);
                var files = await gridFS.Find(filter).ToListAsync();

                // Verificar resultados
                MessageBox.Show($"Número de canciones encontradas: {files.Count}");

                if (files.Count == 0)
                {
                    MessageBox.Show($"No se encontraron canciones para '{option}'.");
                    ListaCanciones.ItemsSource = null;
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

   

