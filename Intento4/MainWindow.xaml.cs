using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Intento4
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private GridFSBucket gridFS;
        public List<Cancion> ListaActual { get; private set; } = new List<Cancion>(); // Lista actual de reproducción
        private int currentSongIndex; // Índice de la canción actual
        private bool isPaused = false; // Variable para rastrear el estado de la canción

        public MainWindow()
        {
            InitializeComponent();

            // Conexión a MongoDB
            var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
            var database = client.GetDatabase("Musify");
            gridFS = new GridFSBucket(database);
            ShowInicio2_Click(null, null);
            // Inicializar la lista de canciones
            CargarCancionesDesdeMongoDB();
        }

        private async void CargarCancionesDesdeMongoDB()
        {
            try
            {
                // Obtener todos los archivos en la colección fs.files
                var files = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();

                // Transformar los datos para la lista actual
                ListaActual = files.Select(file => new Cancion
                {
                    titulo = file.Metadata.Contains("titulo") ? file.Metadata["titulo"].AsString : "Título desconocido",
                    artista = file.Metadata.Contains("artista") ? file.Metadata["artista"].AsString : "Artista desconocido",
                    album = file.Metadata.Contains("album") ? file.Metadata["album"].AsString : "Álbum desconocido",
                    _id = file.Id
                }).ToList();

                PlayerStatus.Text = $"Se cargaron {ListaActual.Count} canciones desde la base de datos.";

                // Reproducir la primera canción si hay canciones en la lista
                if (ListaActual.Count > 0)
                {
                    currentSongIndex = 0; // Iniciar desde la primera canción
                    ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
                }
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar canciones: {ex.Message}";
            }
        }

        public void ActualizarListaReproduccion(List<Cancion> nuevasCanciones)
        {
            ListaActual = nuevasCanciones;
           

            // Reiniciar el índice a la primera canción de la nueva lista
            if (ListaActual.Count > 0)
            {
                currentSongIndex = 0;
                ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
            }
        }

        public async void ReproducirCancionDesdeMongoDB(ObjectId id)
        {
            try
            {
                mediaPlayer.Stop(); // Detener la canción actual

                // Obtener metadatos de la canción
                var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", id);
                var fileInfo = await gridFS.Find(filter).FirstOrDefaultAsync();

                if (fileInfo == null)
                {
                    PlayerStatus.Text = "Error: Canción no encontrada.";
                    return;
                }

                // Mostrar metadatos en la interfaz
                var metadata = fileInfo.Metadata;
                SongTitle.Text = metadata.Contains("titulo") ? metadata["titulo"].AsString : "Título desconocido";
                SongArtist.Text = metadata.Contains("artista") ? metadata["artista"].AsString : "Artista desconocido";

                // Crear una ruta temporal única para el archivo
                string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_" + fileInfo.Filename);

                // Descargar el archivo desde GridFS
                using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await gridFS.DownloadToStreamAsync(id, stream);
                }

                // Reproducir la canción desde el archivo temporal
                mediaPlayer.Open(new Uri(tempFilePath));
                mediaPlayer.Play();

                // Actualizar el estado del reproductor
                PlayerStatus.Text = "Reproduciendo: " + fileInfo.Filename;

                // Configurar evento para eliminar el archivo temporal después de reproducirlo
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    try
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"Error al eliminar archivo temporal: {cleanupEx.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al reproducir la canción: {ex.Message}";
            }
        }
        private void CargarInicioPage()
        {
            var inicioPage = new InicioPage(this); // Pasar la instancia de MainWindow
            MainFrame.Navigate(inicioPage);        // Navegar a InicioPage
        }
        private void CargarExplorarPage()
        {
            var explorarPage = new ExplorarPage(this); // Pasar la instancia de MainWindow
            MainFrame.Navigate(explorarPage);        // Navegar a InicioPage
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



        // Métodos para cambiar de página (navegación entre páginas de la app)
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
