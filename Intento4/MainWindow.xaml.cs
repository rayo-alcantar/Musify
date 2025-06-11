// File: MainWindow.xaml.cs
using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using System;
using System.IO;
using System.Linq;               // Para .Select(...)
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Diagnostics;
using System.Windows.Automation;  // Para AutomationProperties

namespace Intento4
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private GridFSBucket gridFS;
        private DispatcherTimer playbackTimer;
        private bool isDraggingSlider = false;
        public List<Cancion> ListaActual { get; private set; } = new List<Cancion>();
        private int currentSongIndex;
        private bool isPaused = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Timer para actualizar el slider cada segundo
                playbackTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                playbackTimer.Tick += PlaybackTimer_Tick;

                // Conexión a MongoDB
                var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
                var database = client.GetDatabase("Musify");
                gridFS = new GridFSBucket(database);

                // Carga inicial
                ShowInicio2_Click(null, null);
                CargarCancionesDesdeMongoDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al iniciar la aplicación: {ex.Message}", 
                                "Error de inicio", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        /// <summary>Lee todas las canciones desde GridFS y las muestra.</summary>
        private async void CargarCancionesDesdeMongoDB()
        {
            try
            {
                var files = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();
                ListaActual = files.Select(file => new Cancion
                {
                    titulo   = file.Metadata.GetValue("titulo",   "Título desconocido").AsString,
                    artista  = file.Metadata.GetValue("artista",  "Artista desconocido").AsString,
                    album    = file.Metadata.GetValue("album",    "Álbum desconocido").AsString,
                    _id      = file.Id
                }).ToList();

                PlayerStatus.Text = $"Se cargaron {ListaActual.Count} canciones.";

                if (ListaActual.Count > 0)
                {
                    currentSongIndex = 0;
                    ReproducirCancionDesdeMongoDB(ListaActual[0]._id);
                }
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar canciones: {ex.Message}";
            }
        }

        /// <summary>Permite que páginas externas actualicen la lista de reproducción.</summary>
        public void ActualizarListaReproduccion(List<Cancion> nuevasCanciones)
        {
            try
            {
                ListaActual = nuevasCanciones;
                PlayerStatus.Text = $"Lista actualizada: {ListaActual.Count} canciones.";

                if (ListaActual.Count > 0)
                {
                    currentSongIndex = 0;
                    ReproducirCancionDesdeMongoDB(ListaActual[0]._id);
                }
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al actualizar playlist: {ex.Message}";
            }
        }

        /// <summary>Descarga y reproduce la canción dada su ObjectId de GridFS.</summary>
        public async void ReproducirCancionDesdeMongoDB(ObjectId id)
        {
            string tempFilePath = null;
            try
            {
                mediaPlayer.Stop();

                var fileInfo = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Eq("_id", id))
                                          .FirstOrDefaultAsync();
                if (fileInfo == null)
                {
                    PlayerStatus.Text = "Canción no encontrada.";
                    return;
                }

                // Mostrar metadatos
                SongTitle.Text  = fileInfo.Metadata.GetValue("titulo",  "Título desconocido").AsString;
                SongArtist.Text = fileInfo.Metadata.GetValue("artista", "Artista desconocido").AsString;

                // Ruta temporal
                tempFilePath = Path.Combine(Path.GetTempPath(),
                                           $"{Guid.NewGuid()}_{fileInfo.Filename}");
                using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await gridFS.DownloadToStreamAsync(id, fs);
                }

                // Ajustar máximo del slider una vez cargue media
                mediaPlayer.MediaOpened += (s, e) =>
                {
                    try
                    {
                        if (mediaPlayer.NaturalDuration.HasTimeSpan)
                            PositionSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    }
                    catch (Exception mex) { Debug.WriteLine($"MediaOpened err: {mex.Message}"); }
                };

                // Reproducir
                mediaPlayer.Open(new Uri(tempFilePath));
                mediaPlayer.Play();
                playbackTimer.Start();

                PlayerStatus.Text = "Reproduciendo: " + fileInfo.Filename;

                // Cuando termine, parar timer y borrar temporal
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    playbackTimer.Stop();
                    try { File.Delete(tempFilePath); }
                    catch (Exception cle) { Debug.WriteLine($"Cleanup err: {cle.Message}"); }
                };
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error reproducción: {ex.Message}";
                try { if (!string.IsNullOrEmpty(tempFilePath)) File.Delete(tempFilePath); }
                catch { }
            }
        }

        /// <summary>Actualiza el slider según avance la canción.</summary>
        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
                    PositionSlider.Value = mediaPlayer.Position.TotalSeconds;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Timer err: {ex.Message}");
            }
        }

        private void PositionSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingSlider = true;
        }

        private void PositionSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isDraggingSlider = false;
                mediaPlayer.Position = TimeSpan.FromSeconds(PositionSlider.Value);
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cambiar posición: {ex.Message}";
            }
        }

        // ----------------------------------------------------
        //  Navegación entre páginas (botones laterales)
        // ----------------------------------------------------
        private void ShowInicio2_Click(object sender, RoutedEventArgs e)
        {
            try { MainFrame.Navigate(new InicioPage(this)); }
            catch (Exception ex) { PlayerStatus.Text = $"Error Inicio: {ex.Message}"; }
        }

        private void ShowExplorar_Click(object sender, RoutedEventArgs e)
        {
            try { MainFrame.Navigate(new ExplorarPage(this)); }
            catch (Exception ex) { PlayerStatus.Text = $"Error Explorar: {ex.Message}"; }
        }

        private void ShowTuBiblioteca_Click(object sender, RoutedEventArgs e)
        {
            try { MainFrame.Navigate(new BibliotecaPage(this)); }
            catch (Exception ex) { PlayerStatus.Text = $"Error Biblioteca: {ex.Message}"; }
        }

        private void ShowPlaylist_Click(object sender, RoutedEventArgs e)
        {
            try { MainFrame.Navigate(new PlaylistPage(this)); }
            catch (Exception ex) { PlayerStatus.Text = $"Error Playlist: {ex.Message}"; }
        }

        // ----------------------------------------------------
        //  Controles de reproducción
        // ----------------------------------------------------
        private void PreviousSong_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ListaActual.Count == 0) return;
                currentSongIndex = (currentSongIndex - 1 + ListaActual.Count) % ListaActual.Count;
                ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error atrás: {ex.Message}";
            }
        }

        private void NextSong_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ListaActual.Count == 0) return;
                currentSongIndex = (currentSongIndex + 1) % ListaActual.Count;
                ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error adelante: {ex.Message}";
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
                PlayerStatus.Text = $"Error pausar: {ex.Message}";
            }
        }
    }
}