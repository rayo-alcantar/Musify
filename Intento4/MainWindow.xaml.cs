using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using MongoDB.Driver;
using Intento4.Models;
using Intento4.Servicios;

namespace Intento4
{
    /// <summary>
    /// Ventana principal de la aplicación. Responsable de la reproducción de música,
    /// la gestión de la lista de reproducción actual y la navegación entre las diferentes páginas (vistas).
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer(); // Objeto para la reproducción de audio.
        private GridFSBucket gridFS; // Para acceder a los archivos de canciones en MongoDB GridFS.

        /// <summary>
        /// Lista de canciones actualmente cargadas en el reproductor.
        /// Puede ser la biblioteca completa o una playlist específica.
        /// </summary>
        public List<Cancion> ListaActual { get; private set; } = new List<Cancion>();
        private int currentSongIndex; // Índice de la canción que se está reproduciendo o está seleccionada en ListaActual.
        private bool isPaused = false; // Indica si la reproducción está actualmente en pausa.

        /// <summary>
        /// Obtiene el objeto Usuario que representa al usuario actualmente logueado.
        /// Esta propiedad se establece durante la inicialización de MainWindow,
        /// recibiendo el usuario desde LoginWindow.
        /// </summary>
        public Usuario CurrentUser { get; private set; }

        /// <summary>
        /// Constructor de la ventana principal.
        /// </summary>
        /// <param name="user">El objeto Usuario que ha iniciado sesión.</param>
        public MainWindow(Usuario user)
        {
            InitializeComponent();
            CurrentUser = user; // Almacena el usuario logueado.

            // Inicializa la conexión a MongoDB GridFS para la carga de canciones.
            // Utiliza la cadena de conexión centralizada de MongoDBService.
            var client = new MongoClient(MongoDBService.ConnectionString);
            var database = client.GetDatabase("Musify"); // El nombre de la BD podría centralizarse.
            gridFS = new GridFSBucket(database);

            ShowInicio2_Click(null, null); // Carga la página de inicio por defecto.
            CargarCancionesDesdeMongoDB(); // Carga la biblioteca inicial de canciones.
        }

        /// <summary>
        /// Carga la lista inicial de canciones desde MongoDB GridFS al iniciar la aplicación.
        /// </summary>
        private async void CargarCancionesDesdeMongoDB()
        {
            try
            {
                var files = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();
                ListaActual = files.Select(file => new Cancion
                {
                    titulo = file.Metadata.Contains("titulo") ? file.Metadata["titulo"].AsString : "Título desconocido",
                    artista = file.Metadata.Contains("artista") ? file.Metadata["artista"].AsString : "Artista desconocido",
                    album = file.Metadata.Contains("album") ? file.Metadata["album"].AsString : "Álbum desconocido",
                    _id = file.Id
                }).ToList();

                PlayerStatus.Text = $"Se cargaron {ListaActual.Count} canciones desde la base de datos.";

                if (ListaActual.Count > 0)
                {
                    currentSongIndex = 0;
                    ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
                }
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar canciones: {ex.Message}";
            }
        }

        /// <summary>
        /// Actualiza la lista de reproducción actual con un nuevo conjunto de canciones.
        /// Esto es útil cuando se cambia de una playlist a otra o se carga una vista diferente.
        /// </summary>
        /// <param name="nuevasCanciones">La nueva lista de canciones a reproducir.</param>
        public void ActualizarListaReproduccion(List<Cancion> nuevasCanciones)
        {
            ListaActual = nuevasCanciones;
            if (ListaActual.Count > 0)
            {
                currentSongIndex = 0;
                ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
            }
            else
            {
                // Si la nueva lista está vacía, detener la reproducción y limpiar UI.
                mediaPlayer.Stop();
                SongTitle.Text = "Sin canción";
                SongArtist.Text = "N/A";
                PlayerStatus.Text = "No hay canciones para reproducir.";
            }
        }

        /// <summary>
        /// Descarga y reproduce una canción específica desde MongoDB GridFS usando su ID.
        /// </summary>
        /// <param name="id">El ObjectId de la canción en GridFS.</param>
        public async void ReproducirCancionDesdeMongoDB(ObjectId id)
        {
            try
            {
                mediaPlayer.Stop();

                var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", id);
                var fileInfo = await gridFS.Find(filter).FirstOrDefaultAsync();

                if (fileInfo == null)
                {
                    PlayerStatus.Text = "Error: Canción no encontrada en GridFS.";
                    return;
                }

                var metadata = fileInfo.Metadata;
                SongTitle.Text = metadata.Contains("titulo") ? metadata["titulo"].AsString : "Título desconocido";
                SongArtist.Text = metadata.Contains("artista") ? metadata["artista"].AsString : "Artista desconocido";

                // Descarga a un archivo temporal para reproducción.
                string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_" + fileInfo.Filename);

                using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await gridFS.DownloadToStreamAsync(id, stream);
                }

                mediaPlayer.Open(new Uri(tempFilePath));
                mediaPlayer.Play();
                PlayerStatus.Text = "Reproduciendo: " + fileInfo.Filename;
                isPaused = false; // Asegura que el estado de pausa se reinicie.

                // Limpia el archivo temporal después de que la canción termine.
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    try
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        // Log o manejar error de limpieza si es necesario.
                        Console.WriteLine($"Error al eliminar archivo temporal: {cleanupEx.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al reproducir la canción: {ex.Message}";
            }
        }

        // --- Métodos de navegación y control de reproducción ---
        private void CargarInicioPage()
        {
            var inicioPage = new InicioPage(this);
            MainFrame.Navigate(inicioPage);
        }
        private void CargarExplorarPage()
        {
            var explorarPage = new ExplorarPage(this);
            MainFrame.Navigate(explorarPage);
        }
        private void NextSong_Click(object sender, RoutedEventArgs e)
        {
            if (ListaActual.Count > 0)
            {
                currentSongIndex = (currentSongIndex + 1) % ListaActual.Count;
                ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
            }
        }

        private void PreviousSong_Click(object sender, RoutedEventArgs e)
        {
            if (ListaActual.Count > 0)
            {
                currentSongIndex = (currentSongIndex - 1 + ListaActual.Count) % ListaActual.Count;
                ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
            }
        }

        private void PauseSong_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.Source == null) return; // No hay canción cargada

            if (isPaused)
            {
                mediaPlayer.Play();
                PlayerStatus.Text = "Reproduciendo";
                isPaused = false;
            }
            else
            {
                mediaPlayer.Pause();
                PlayerStatus.Text = "Pausado";
                isPaused = true;
            }
        }

        // --- Navegación del menú lateral ---
        private void ShowInicio2_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new InicioPage(this));
        }

        private void ShowExplorar_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ExplorarPage(this));
        }

        private void ShowTuBiblioteca_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BibliotecaPage(this));
        }

        private void ShowPlaylist_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PlaylistPage(this));
        }
    }
}
