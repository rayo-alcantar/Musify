using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Intento4
{
    public partial class BibliotecaPage : Page
    {
        private readonly GridFSBucket   _gridFS;
        private readonly MongoDBService _db;
        private readonly MainWindow     _reproductor;

        private List<Cancion>  CancionesOriginales = new();
        private List<Playlist> PlaylistsPropias    = new();

        public BibliotecaPage(MainWindow reproductor)
        {
            InitializeComponent();
            _reproductor = reproductor;

            var client   = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
            var database = client.GetDatabase("Musify");
            _gridFS      = new GridFSBucket(database);

            _db = new MongoDBService();

            ObtenerCancionestotales();
            ObtenerPlaylistsUsuario();
        }

        /* ========== CANCIONES ========== */
        private async void ObtenerCancionestotales()
        {
            try
            {
                var files = await _gridFS.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();

                CancionesOriginales = files.Select(file => new Cancion
                {
                    titulo  = file.Metadata["titulo"].AsString,
                    artista = file.Metadata["artista"].AsString,
                    album   = file.Metadata["album"].AsString,
                    _id     = file.Id
                }).ToList();

                ListaCanciones.ItemsSource = CancionesOriginales;
                _reproductor?.ActualizarListaReproduccion(CancionesOriginales);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener canciones: {ex.Message}");
            }
        }

        private void ListaCanciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaCanciones.SelectedItem is Cancion sel)
                _reproductor?.ReproducirCancionDesdeMongoDB(sel._id);
        }

        /* ========== PLAYLISTS PRIVADAS ========== */
        private void ObtenerPlaylistsUsuario()
        {
            PlaylistsPropias = _db.ObtenerPlaylistsPorUsuario(Session.UserId);
            PlaylistsListView.ItemsSource = PlaylistsPropias;   // ListView definido en XAML
        }

        private void PlaylistsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistsListView.SelectedItem is Playlist pl)
            {
                var canciones = pl.canciones
                                   .Select(id => CancionesOriginales.FirstOrDefault(c => c._id == id))
                                   .Where(c => c != null)
                                   .ToList();

                _reproductor?.ActualizarListaReproduccion(canciones);
            }
        }

        /* ========== Búsqueda local ========== */
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) =>
            FiltrarCanciones(SearchBox.Text);

        private void SearchButton_Click(object sender, RoutedEventArgs e) =>
            FiltrarCanciones(SearchBox.Text);

        private void FiltrarCanciones(string query)
        {
            var filtradas = CancionesOriginales
                .Where(c => c.titulo.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            c.artista.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ListaCanciones.ItemsSource = filtradas;
        }
    }
}
