using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Intento4.Servicios; // Added using for Servicios

namespace Intento4
{
    /// <summary>
    /// Página para la generación de playlists dinámicas basadas en categorías.
    /// </summary>
    public partial class PlaylistPage : Page
    {
        private GridFSBucket gridFS;
        private MainWindow _mainWindow; // Renamed for clarity, was 'reproductor'

        public PlaylistPage(MainWindow mainWindow) // Renamed parameter for clarity
        {
            InitializeComponent();
            this._mainWindow = mainWindow;

            // Conectar a MongoDB usando la cadena centralizada
            var client = new MongoClient(MongoDBService.ConnectionString);
            var database = client.GetDatabase("Musify"); // Database name can also be centralized
            gridFS = new GridFSBucket(database);
        }

        /// <summary>
        /// Maneja el cambio de selección en el ComboBox de categorías.
        /// Actualiza las opciones disponibles en el ComboBox de opciones.
        /// </summary>
        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            optionsComboBox.Items.Clear();
            optionsComboBox.IsEnabled = true;

            var selectedCategoryItem = categoryComboBox.SelectedItem as ComboBoxItem;
            if (selectedCategoryItem == null)
            {
                optionsComboBox.IsEnabled = false;
                return;
            }
            var selectedCategory = selectedCategoryItem.Content.ToString();

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

        /// <summary>
        /// Maneja el clic del botón "Generar Playlist".
        /// Obtiene las canciones filtradas según la categoría y opción seleccionada.
        /// </summary>
        private async void GenerarPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (optionsComboBox.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecciona una opción para generar la playlist.");
                return;
            }

            var selectedOption = optionsComboBox.SelectedItem.ToString();
            var selectedCategoryItem = categoryComboBox.SelectedItem as ComboBoxItem;
            if (selectedCategoryItem == null) // Should not happen if optionsComboBox is enabled
            {
                 MessageBox.Show("Por favor, selecciona una categoría válida.");
                return;
            }
            var selectedCategory = selectedCategoryItem.Content.ToString();

            string playlistTitle = $"Playlist {selectedOption}";
            PlaylistTitle.Text = playlistTitle;

            await ObtenerCancionesFiltradas(selectedCategory, selectedOption);
        }

        /// <summary>
        /// Obtiene y muestra las canciones de GridFS que coinciden con la categoría y opción dadas.
        /// </summary>
        private async Task ObtenerCancionesFiltradas(string category, string option)
        {
            try
            {
                //MessageBox.Show($"Aplicando filtro: metadata.{category.ToLower()} = {option}"); // Debug message

                var filter = Builders<GridFSFileInfo>.Filter.Eq($"metadata.{category.ToLower()}", option);
                var files = await gridFS.Find(filter).ToListAsync();

                //MessageBox.Show($"Número de canciones encontradas: {files.Count}"); // Debug message

                if (files.Count == 0)
                {
                    MessageBox.Show($"No se encontraron canciones para '{option}'.");
                    ListaCanciones.ItemsSource = null;
                    return;
                }

                var canciones = files.Select(file => new Cancion
                {
                    titulo = file.Metadata.Contains("titulo") ? file.Metadata["titulo"].AsString : "Título Desconocido",
                    artista = file.Metadata.Contains("artista") ? file.Metadata["artista"].AsString : "Artista Desconocido",
                    album = file.Metadata.Contains("album") ? file.Metadata["album"].AsString : "Álbum Desconocido",
                    _id = file.Id
                }).ToList();

                ListaCanciones.ItemsSource = canciones;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener canciones: {ex.Message}");
                ListaCanciones.ItemsSource = null;
            }
        }

        /// <summary>
        /// Maneja la selección de una canción en la lista para su reproducción.
        /// </summary>
        private void ListaCanciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaCanciones.SelectedItem is Cancion selectedSong)
            {
                if (_mainWindow != null)
                {
                    _mainWindow.ReproducirCancionDesdeMongoDB(selectedSong._id);
                }
                else
                {
                    MessageBox.Show("Error: Reproductor no está inicializado.");
                }
            }
        }
    }
}
