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
        public List<Cancion> ListaActual { get; private set; } = new List<Cancion>();
        private int currentSongIndex;
        private bool isPaused = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Conexión a MongoDB
                var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
                var database = client.GetDatabase("Musify");
                gridFS = new GridFSBucket(database);

                ShowInicio2_Click(null, null);
                CargarCancionesDesdeMongoDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al iniciar la aplicación: {ex.Message}", "Error de inicio", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

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

        public void ActualizarListaReproduccion(List<Cancion> nuevasCanciones)
        {
            try
            {
                ListaActual = nuevasCanciones;

                if (ListaActual.Count > 0)
                {
                    currentSongIndex = 0;
                    ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
                }
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al actualizar la lista de reproducción: {ex.Message}";
            }
        }

        public async void ReproducirCancionDesdeMongoDB(ObjectId id)
        {
            string tempFilePath = null;
            try
            {
                mediaPlayer.Stop();

                var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", id);
                var fileInfo = await gridFS.Find(filter).FirstOrDefaultAsync();

                if (fileInfo == null)
                {
                    PlayerStatus.Text = "Error: Canción no encontrada.";
                    return;
                }

                var metadata = fileInfo.Metadata;
                SongTitle.Text = metadata.Contains("titulo") ? metadata["titulo"].AsString : "Título desconocido";
                SongArtist.Text = metadata.Contains("artista") ? metadata["artista"].AsString : "Artista desconocido";

                tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_" + fileInfo.Filename);

                try
                {
                    using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await gridFS.DownloadToStreamAsync(id, stream);
                    }
                }
                catch (Exception exFile)
                {
                    PlayerStatus.Text = $"Error al descargar el archivo: {exFile.Message}";
                    return;
                }

                try
                {
                    mediaPlayer.Open(new Uri(tempFilePath));
                    mediaPlayer.Play();
                }
                catch (Exception exMp)
                {
                    PlayerStatus.Text = $"Error al reproducir el archivo de audio: {exMp.Message}";
                    try
                    {
                        if (File.Exists(tempFilePath))
                            File.Delete(tempFilePath);
                    }
                    catch { }
                    return;
                }

                PlayerStatus.Text = "Reproduciendo: " + fileInfo.Filename;

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
                try
                {
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch { }
            }
        }

        private void CargarInicioPage()
        {
            try
            {
                var inicioPage = new InicioPage(this);
                MainFrame.Navigate(inicioPage);
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al navegar a la página de inicio: {ex.Message}";
            }
        }
        private void CargarExplorarPage()
        {
            try
            {
                var explorarPage = new ExplorarPage(this);
                MainFrame.Navigate(explorarPage);
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al navegar a la página de explorar: {ex.Message}";
            }
        }

        private void NextSong_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ListaActual.Count > 0)
                {
                    currentSongIndex = (currentSongIndex + 1) % ListaActual.Count;
                    ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
                }
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al pasar a la siguiente canción: {ex.Message}";
            }
        }

        private void PreviousSong_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ListaActual.Count > 0)
                {
                    currentSongIndex = (currentSongIndex - 1 + ListaActual.Count) % ListaActual.Count;
                    ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
                }
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al regresar a la canción anterior: {ex.Message}";
            }
        }

        private void PauseSong_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al pausar/reanudar la canción: {ex.Message}";
            }
        }

        private void ShowInicio2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new InicioPage(this));
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar Inicio: {ex.Message}";
            }
        }

        private void ShowExplorar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new ExplorarPage(this));
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar Explorar: {ex.Message}";
            }
        }

        private void ShowTuBiblioteca_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new BibliotecaPage(this));
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar Tu Biblioteca: {ex.Message}";
            }
        }

        private void ShowPlaylist_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainFrame.Navigate(new PlaylistPage(this));
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar Playlist: {ex.Message}";
            }
        }
    }
}