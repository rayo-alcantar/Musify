// File: MainWindow.xaml.cs
using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
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

        // Lista actual de canciones
        public List<Cancion> ListaActual { get; private set; } = new List<Cancion>();

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Timer para slider de posición
                playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                playbackTimer.Tick += PlaybackTimer_Tick;

                // Volumen inicial
                mediaPlayer.Volume = VolumeSlider.Value;

                // Conexión a MongoDB/GridFS
                var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
                gridFS = new GridFSBucket(client.GetDatabase("Musify"));

                // Carga inicial
                ShowInicio2_Click(null, null);
                CargarCancionesDesdeMongoDB();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}",
                                "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        /// <summary>Carga todas las canciones desde GridFS.</summary>
        private async void CargarCancionesDesdeMongoDB()
        {
            try
            {
                var files = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();
                ListaActual = files.Select(f => new Cancion
                {
                    titulo  = f.Metadata.GetValue("titulo",  "Título desconocido").AsString,
                    artista = f.Metadata.GetValue("artista", "Artista desconocido").AsString,
                    album   = f.Metadata.GetValue("album",   "Álbum desconocido").AsString,
                    _id     = f.Id
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
                PlayerStatus.Text = $"Error al actualizar playlist: {ex.Message}";
            }
        }

        //cambio 2: Manejo de errores en carga de archivos
        /// <summary>Descarga y reproduce la canción, con try-catch robusto.</summary>
        public async void ReproducirCancionDesdeMongoDB(ObjectId id)
        {
            string tempPath = null;
            try
            {
                mediaPlayer.Stop();

                var fileInfo = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Eq("_id", id))
                                          .FirstOrDefaultAsync();
                if (fileInfo == null)
                {
                    PlayerStatus.Text = "Error: Canción no encontrada.";
                    return;
                }

                // Mostrar metadatos
                SongTitle.Text  = fileInfo.Metadata.GetValue("titulo",  "Título desconocido").AsString;
                SongArtist.Text = fileInfo.Metadata.GetValue("artista", "Artista desconocido").AsString;

                // Generar ruta temporal
                tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{fileInfo.Filename}");

                // --- FIX: usar using{} para descargar **
                using (var downloadStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                { 
                    await gridFS.DownloadToStreamAsync(id, downloadStream);
                }

                //cambio 3: Validación de archivos corruptos o no compatibles
                byte[] header = new byte[4];
                using (var sigFs = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
                    sigFs.Read(header, 0, 4);
                string sig = Encoding.ASCII.GetString(header);
                bool isMp3 = sig.StartsWith("ID3") || (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0);
                bool isWav = sig == "RIFF";
                if (!isMp3 && !isWav)
                {
                    PlayerStatus.Text = "Archivo no compatible o corrupto.";
                    File.Delete(tempPath);
                    return;
                }

                // Ajustar máximo del slider
                mediaPlayer.MediaOpened += (s, e) =>
                {
                    if (mediaPlayer.NaturalDuration.HasTimeSpan)
                        PositionSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                };

                // Reproducir y arrancar timer
                mediaPlayer.Open(new Uri(tempPath));
                mediaPlayer.Play();
                playbackTimer.Start();
                PlayerStatus.Text = $"Reproduciendo: {fileInfo.Filename}";

                // Al terminar, parar timer y borrar archivo
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    playbackTimer.Stop();
                    try { File.Delete(tempPath); }
                    catch { }
                };
            }
            catch (Exception ex)
            {
                PlayerStatus.Text = $"Error reproducción: {ex.Message}";
                try { if (!string.IsNullOrEmpty(tempPath)) File.Delete(tempPath); }
                catch { }
            }
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (!isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
                PositionSlider.Value = mediaPlayer.Position.TotalSeconds;
        }
        private void PositionSlider_PreviewMouseDown(object s, MouseButtonEventArgs e) => isDraggingSlider = true;
        private void PositionSlider_PreviewMouseUp(object s, MouseButtonEventArgs e)
        {
            isDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(PositionSlider.Value);
        }

        private void VolumeSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
            => mediaPlayer.Volume = e.NewValue;

        // Navegación entre páginas
        private void ShowInicio2_Click(object s, RoutedEventArgs e)       => MainFrame.Navigate(new InicioPage(this));
        private void ShowExplorar_Click(object s, RoutedEventArgs e)     => MainFrame.Navigate(new ExplorarPage(this));
        private void ShowTuBiblioteca_Click(object s, RoutedEventArgs e) => MainFrame.Navigate(new BibliotecaPage(this));
        private void ShowPlaylist_Click(object s, RoutedEventArgs e)     => MainFrame.Navigate(new PlaylistPage(this));

        //cambio 6: Confirmación de eliminación
        private void DeleteSong_Click(object s, RoutedEventArgs e)
        {
            var res = MessageBox.Show("¿Deseas eliminar esta canción?",
                                      "Confirmar eliminación",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes && ListaActual.Any())
            {
                ListaActual.RemoveAt(currentSongIndex);
                PlayerStatus.Text = "Canción eliminada.";
                if (ListaActual.Any())
                {
                    currentSongIndex %= ListaActual.Count;
                    ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
                }
                else
                {
                    mediaPlayer.Stop();
                    SongTitle.Text = "";
                    SongArtist.Text = "";
                }
            }
        }

        // Reproducción previa, siguiente, pausa
        private void PreviousSong_Click(object s, RoutedEventArgs e)
        {
            if (!ListaActual.Any()) return;
            currentSongIndex = (currentSongIndex - 1 + ListaActual.Count) % ListaActual.Count;
            ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
        }
        private void NextSong_Click(object s, RoutedEventArgs e)
        {
            if (!ListaActual.Any()) return;
            currentSongIndex = (currentSongIndex + 1) % ListaActual.Count;
            ReproducirCancionDesdeMongoDB(ListaActual[currentSongIndex]._id);
        }
        private void PauseSong_Click(object s, RoutedEventArgs e)
        {
            if (isPaused) mediaPlayer.Play();
            else           mediaPlayer.Pause();
            isPaused = !isPaused;
            PlayerStatus.Text = isPaused ? "Pausado" : "Reproduciendo";
        }

        //cambio 7: Prevención de duplicados
        public void AgregarCancion(Cancion nueva)
        {
            if (ListaActual.Any(c => c._id == nueva._id))
            {
                var r = MessageBox.Show("La canción ya existe. ¿Agregar de todos modos?",
                                         "Duplicado", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r == MessageBoxResult.No) return;
            }
            ListaActual.Add(nueva);
            PlayerStatus.Text = "Canción agregada.";
        }
    }
}
