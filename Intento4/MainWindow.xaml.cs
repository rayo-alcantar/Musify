// File: MainWindow.xaml.cs
using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using System;
using System.IO;
using System.Linq;               // Para .Select(...)
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Diagnostics;

namespace Intento4
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private GridFSBucket gridFS;
        private DispatcherTimer playbackTimer;
        private bool isDraggingSlider = false;
        private int currentSongIndex;
        private bool isPaused = false;

        // Lista de canciones actual
        public List<Cancion> ListaActual { get; private set; } = new List<Cancion>();

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // 1) Configurar timer para actualizar el slider de posición
                playbackTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                playbackTimer.Tick += PlaybackTimer_Tick;

                // 2) Inicializar volumen inicial
                mediaPlayer.Volume = VolumeSlider.Value;

                // 3) Conexión a MongoDB y GridFS
                var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
                var database = client.GetDatabase("Musify");
                gridFS = new GridFSBucket(database);

                // 4) Carga inicial de interfaz y datos
                ShowInicio2_Click(null, null);
                CargarCancionesDesdeMongoDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}",
                                "Error Crítico",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        /// <summary>Carga las canciones desde GridFS y las muestra.</summary>
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
                if (ListaActual.Any())
                    ReproducirCancionDesdeMongoDB(ListaActual[0]._id);
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error al cargar canciones: {ex.Message}";
            }
        }

        /// <summary>Actualiza la playlist desde otras páginas.</summary>
        public void ActualizarListaReproduccion(List<Cancion> nuevas)
        {
            try
            {
                ListaActual = nuevas;
                PlayerStatus.Text = $"Playlist actualizada: {nuevas.Count} canciones.";
                if (ListaActual.Any())
                    ReproducirCancionDesdeMongoDB(ListaActual[0]._id);
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error actualizando playlist: {ex.Message}";
            }
        }

        /// <summary>Descarga desde GridFS y reproduce la canción.</summary>
        public async void ReproducirCancionDesdeMongoDB(ObjectId id)
        {
            string tempPath = null;
            try
            {
                mediaPlayer.Stop();

                // Obtener metadatos
                var fileInfo = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Eq("_id", id)).FirstOrDefaultAsync();
                if (fileInfo == null)
                {
                    PlayerStatus.Text = "Canción no encontrada.";
                    return;
                }

                // Mostrar título y artista
                SongTitle.Text  = fileInfo.Metadata.GetValue("titulo",  "Título desconocido").AsString;
                SongArtist.Text = fileInfo.Metadata.GetValue("artista", "Artista desconocido").AsString;

                // Generar archivo temporal y descargar
                tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileInfo.Filename}");
                using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await gridFS.DownloadToStreamAsync(id, fs);

                // Ajustar el máximo del slider una vez abra el media
                mediaPlayer.MediaOpened += (s, e) =>
                {
                    if (mediaPlayer.NaturalDuration.HasTimeSpan)
                        PositionSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                };

                // Iniciar reproducción y timer
                mediaPlayer.Open(new Uri(tempPath));
                mediaPlayer.Play();
                playbackTimer.Start();

                PlayerStatus.Text = $"Reproduciendo: {fileInfo.Filename}";

                // Al finalizar, detener timer y eliminar archivo temporal
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    playbackTimer.Stop();
                    try { File.Delete(tempPath); }
                    catch { /* ignore */ }
                };
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error reproducción: {ex.Message}";
                try { if (!string.IsNullOrEmpty(tempPath)) File.Delete(tempPath); }
                catch { }
            }
        }

        /// <summary>Actualiza el slider de posición cada segundo.</summary>
        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (!isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
                PositionSlider.Value = mediaPlayer.Position.TotalSeconds;
        }

        private void PositionSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingSlider = true;
        }

        private void PositionSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(PositionSlider.Value);
        }

        /// <summary>Maneja el cambio de volumen desde el slider.</summary>
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = e.NewValue;
            // Podrías actualizar PlayerStatus.Text = $"Volumen: {(int)(e.NewValue*100)}%";
        }

        // ---------------- Navegación entre páginas ----------------
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

        // ---------------- Controles de reproducción ----------------
        private void PreviousSong_Click(object sender, RoutedEventArgs e)
        {
            if (!ListaActual.Any()) return;
            currentSongIndex = (currentSongIndex - 1 + ListaActual.Count) % ListaActual.Count;
            ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
        }

        private void NextSong_Click(object sender, RoutedEventArgs e)
        {
            if (!ListaActual.Any()) return;
            currentSongIndex = (currentSongIndex + 1) % ListaActual.Count;
            ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
        }

        private void PauseSong_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {
                mediaPlayer.Play();
                PlayerStatus.Text = "Reproduciendo";
            }
            else
            {
                mediaPlayer.Pause();
                PlayerStatus.Text = "Pausado";
            }
            isPaused = !isPaused;
        }
    }
}
