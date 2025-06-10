using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Intento4.Models;
using Intento4.Servicios;

namespace Intento4
{
    /// <summary>
    /// Página para explorar la biblioteca de canciones y gestionar playlists personales.
    /// </summary>
    public partial class BibliotecaPage : Page
    {
        private GridFSBucket gridFS;
        /// <summary>
        /// Referencia a la ventana principal para acceder a funcionalidades comunes como CurrentUser o el reproductor.
        /// </summary>
        private MainWindow _mainWindow;
        private List<Cancion> CancionesOriginales;
        private MongoDBService _mongoDBService;

        public BibliotecaPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this._mainWindow = mainWindow;
            _mongoDBService = new MongoDBService();

            // Conectar a MongoDB usando la cadena centralizada para GridFS (utilizado para cargar todas las canciones)
            var client = new MongoClient(MongoDBService.ConnectionString);
            var database = client.GetDatabase("Musify"); // Database name can also be centralized
            gridFS = new GridFSBucket(database);

            ObtenerCancionestotales();
            CargarPlaylistsDelUsuario();
        }

        /// <summary>
        /// Carga todas las canciones disponibles desde GridFS en la base de datos.
        /// </summary>
        private async void ObtenerCancionestotales()
        {
            try
            {
                var files = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();
                if (files.Count == 0)
                {
                    // No mostrar MessageBox aquí, puede ser normal no tener canciones al inicio.
                    return;
                }
                CancionesOriginales = files.Select(file => new Cancion
                {
                    // Asegurarse de que los metadatos existen antes de intentar acceder a ellos.
                    titulo = file.Metadata.Contains("titulo") ? file.Metadata["titulo"].AsString : "Título Desconocido",
                    artista = file.Metadata.Contains("artista") ? file.Metadata["artista"].AsString : "Artista Desconocido",
                    album = file.Metadata.Contains("album") ? file.Metadata["album"].AsString : "Álbum Desconocido",
                    _id = file.Id
                }).ToList();

                if (_mainWindow != null)
                {
                    _mainWindow.ActualizarListaReproduccion(CancionesOriginales);
                }
                ListaCanciones.ItemsSource = CancionesOriginales;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener canciones totales: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja la selección de una canción en la lista general de canciones.
        /// Actualmente, no realiza ninguna acción directa, el play se maneja por MainWindow si es necesario
        /// o por el contexto del click derecho.
        /// </summary>
        private void ListaCanciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Podría usarse para mostrar detalles de la canción o habilitar/deshabilitar opciones.
            // La reproducción se inicia desde MainWindow o el context menu.
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarCanciones(SearchBox.Text);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FiltrarCanciones(SearchBox.Text);
        }

        private void FiltrarCanciones(string query)
        {
            if (CancionesOriginales == null) return;
            var cancionesFiltradas = CancionesOriginales
                .Where(c => (c.titulo != null && c.titulo.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                            (c.artista != null && c.artista.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            ListaCanciones.ItemsSource = cancionesFiltradas;
        }

        /// <summary>
        /// Maneja el evento Click del botón "Crear Nueva Playlist".
        /// Crea una nueva playlist para el usuario actual con un nombre generado automáticamente.
        /// </summary>
        private void CrearPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindow == null || _mainWindow.CurrentUser == null)
            {
                MessageBox.Show("Error: No se pudo obtener la información del usuario actual.", "Error de Usuario", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                var nuevaPlaylist = new Playlist
                {
                    nombre = "Nueva Playlist " + DateTime.Now.ToString("yyyyMMddHHmmssfff"), // Nombre único temporal
                    usuarioId = _mainWindow.CurrentUser._id,
                    canciones = new List<ObjectId>() // Inicializa la lista de canciones vacía
                };
                _mongoDBService.GuardarPlaylist(nuevaPlaylist);
                MessageBox.Show($"Playlist '{nuevaPlaylist.nombre}' creada exitosamente.", "Playlist Creada", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarPlaylistsDelUsuario(); // Refresca la lista de playlists mostrada
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear la playlist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Carga y muestra las playlists pertenecientes al usuario actualmente logueado.
        /// </summary>
        private void CargarPlaylistsDelUsuario()
        {
            if (_mainWindow == null || _mainWindow.CurrentUser == null)
            {
                PlaylistsListBox.ItemsSource = null;
                return;
            }
            try
            {
                var playlists = _mongoDBService.ObtenerPlaylistsPorUsuario(_mainWindow.CurrentUser._id);
                PlaylistsListBox.ItemsSource = playlists;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las playlists: {ex.Message}", "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                PlaylistsListBox.ItemsSource = null;
            }
        }

        /// <summary>
        /// Maneja el cambio de selección en la lista de playlists.
        /// (Futura implementación: podría cargar las canciones de la playlist seleccionada).
        /// </summary>
        private void PlaylistsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Implementar lógica para cuando una playlist es seleccionada.
            // Por ejemplo, cargar las canciones de esta playlist en otra lista.
            // O habilitar un botón "Ver canciones de playlist".
        }

        /// <summary>
        /// Maneja el evento Opening del menú contextual de la lista de canciones.
        /// Habilita o deshabilita la opción "Añadir a Playlist" según si hay una playlist seleccionada.
        /// </summary>
        private void SongContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            if (SongContextMenu.Items.Count == 0) return;

            var menuItem = (MenuItem)SongContextMenu.Items[0]; // Asume que es el único item.
            var selectedPlaylist = PlaylistsListBox.SelectedItem as Playlist;

            if (selectedPlaylist != null)
            {
                menuItem.IsEnabled = true;
                menuItem.Header = $"Añadir a '{selectedPlaylist.nombre}'";
            }
            else
            {
                menuItem.IsEnabled = false;
                menuItem.Header = "Añadir a Playlist Seleccionada"; // Texto genérico
            }
        }

        /// <summary>
        /// Maneja el evento Click para añadir la canción seleccionada (de ListaCanciones)
        /// a la playlist seleccionada (de PlaylistsListBox).
        /// </summary>
        private void AddSongToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var selectedSong = ListaCanciones.SelectedItem as Cancion;
            var selectedPlaylist = PlaylistsListBox.SelectedItem as Playlist;

            if (selectedSong == null)
            {
                MessageBox.Show("Por favor, selecciona una canción de la lista.", "Canción no Seleccionada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedPlaylist == null)
            {
                MessageBox.Show("Por favor, selecciona una playlist de 'Mis Playlists' a la cual añadir la canción.", "Playlist no Seleccionada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedPlaylist.canciones == null)
            {
                selectedPlaylist.canciones = new List<ObjectId>(); // Inicializa si es null
            }

            if (selectedPlaylist.canciones.Contains(selectedSong._id))
            {
                MessageBox.Show($"La canción '{selectedSong.titulo}' ya está en la playlist '{selectedPlaylist.nombre}'.", "Canción ya Existe", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            selectedPlaylist.canciones.Add(selectedSong._id);

            try
            {
                _mongoDBService.ActualizarPlaylist(selectedPlaylist);
                MessageBox.Show($"'{selectedSong.titulo}' añadida exitosamente a la playlist '{selectedPlaylist.nombre}'.", "Canción Añadida", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al añadir la canción a la playlist: {ex.Message}", "Error de Actualización", MessageBoxButton.OK, MessageBoxImage.Error);
                selectedPlaylist.canciones.Remove(selectedSong._id); // Revertir el cambio local si falla la DB
            }
        }
    }
}
