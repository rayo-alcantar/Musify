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
    /// <summary>
    /// Página para generar playlists privadas y reproducir canciones filtradas.
    /// </summary>
    public partial class PlaylistPage : Page
    {
        private readonly GridFSBucket    _gridFS;
        private readonly MongoDBService  _db;
        private readonly MainWindow      _reproductor;
        private List<Cancion>            _cancionesGeneradas = new();

        public PlaylistPage(MainWindow reproductor)
        {
            InitializeComponent();

            _reproductor = reproductor;

            var client   = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
            var database = client.GetDatabase("Musify");
            _gridFS      = new GridFSBucket(database);

            _db = new MongoDBService();
        }

        /* ======= Filtros de categoría ======= */
        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            optionsComboBox.Items.Clear();
            optionsComboBox.IsEnabled = true;

            var cat = (categoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            switch (cat)
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

        /* ======= Generar y guardar playlist privada ======= */
        private async void GenerarPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (optionsComboBox.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecciona una opción.");
                return;
            }

            string cat   = (categoryComboBox.SelectedItem as ComboBoxItem)!.Content.ToString();
            string opt   = optionsComboBox.SelectedItem!.ToString();
            string title = $"Playlist {opt}";

            PlaylistTitle.Text = title;
            await ObtenerCancionesFiltradas(cat, opt);

            if (_cancionesGeneradas.Count == 0) return;

            try
            {
                _db.GuardarPlaylistPrivada(title, _cancionesGeneradas.Select(c => c._id));
                MessageBox.Show("Playlist guardada (solo tú la verás).");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}");
            }
        }

        /* ======= Filtrar canciones en MongoDB ======= */
        private async Task ObtenerCancionesFiltradas(string category, string option)
        {
            try
            {
                var filter = Builders<GridFSFileInfo>.Filter.Eq($"metadata.{category.ToLower()}", option);
                var files  = await _gridFS.Find(filter).ToListAsync();

                _cancionesGeneradas = files.Select(f => new Cancion
                {
                    titulo  = f.Metadata["titulo"].AsString,
                    artista = f.Metadata["artista"].AsString,
                    album   = f.Metadata["album"].AsString,
                    _id     = f.Id
                }).ToList();

                ListaCanciones.ItemsSource = _cancionesGeneradas;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener canciones: {ex.Message}");
            }
        }

        /* ======= Reproducir selección ======= */
        private void ListaCanciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaCanciones.SelectedItem is Cancion song)
                _reproductor?.ReproducirCancionDesdeMongoDB(song._id);
        }
    }
}
