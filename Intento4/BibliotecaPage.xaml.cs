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
        private GridFSBucket gridFS;
        private MainWindow reproductor;
        private List<Cancion> CancionesOriginales; // Lista original de canciones

        public BibliotecaPage(MainWindow reproductor)
        {
            InitializeComponent();
            this.reproductor = reproductor;
            // Conectar a MongoDB
            var client = new MongoClient("mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority");
            var database = client.GetDatabase("Musify");
            gridFS = new GridFSBucket(database);

            // Obtener todas las canciones al cargar la página
            ObtenerCancionestotales();
        }

        private async void ObtenerCancionestotales()
        {
            try
            {
                // Obtener todas las canciones de la base de datos sin filtro
                var files = await gridFS.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();

                if (files.Count == 0)
                {
                    MessageBox.Show("No se encontraron canciones en la base de datos.");
                    return;
                }

                // Transformar los datos para el Binding
                CancionesOriginales = files.Select(file => new Cancion
                {
                    titulo = file.Metadata["titulo"].AsString,
                    artista = file.Metadata["artista"].AsString,
                    album = file.Metadata["album"].AsString,
                    _id = file.Id
                }).ToList();

                // Actualizar la lista de reproducción en el reproductor
                if (reproductor != null)
                {
                    reproductor.ActualizarListaReproduccion(CancionesOriginales);
                }

                // Establecer las canciones como fuente de datos del ListBox
                ListaCanciones.ItemsSource = CancionesOriginales;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener canciones: {ex.Message}");
            }
        }

        private void ListaCanciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaCanciones.SelectedItem != null)
            {
                // Obtener la canción seleccionada
                var selectedSong = (Cancion)ListaCanciones.SelectedItem;

                // Obtener el ID de la canción
                ObjectId songId = selectedSong._id;

                // Reproducir la canción utilizando el reproductor actual
                if (reproductor != null)
                {
                    reproductor.ReproducirCancionDesdeMongoDB(songId);
                }
                else
                {
                    MessageBox.Show("Error: Reproductor no está inicializado.");
                }
            }
        }

        // Evento para el cambio de texto en el cuadro de búsqueda
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarCanciones(SearchBox.Text);
        }

        // Evento cuando el usuario hace clic en el botón de búsqueda
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FiltrarCanciones(SearchBox.Text);
        }

        // Método para filtrar las canciones en función de la búsqueda
        private void FiltrarCanciones(string query)
        {
            if (CancionesOriginales == null) return;

            var cancionesFiltradas = CancionesOriginales
                .Where(c => c.titulo.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            c.artista.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Actualizar la lista de canciones mostradas en la interfaz
            ListaCanciones.ItemsSource = cancionesFiltradas;
        }
    }
}

