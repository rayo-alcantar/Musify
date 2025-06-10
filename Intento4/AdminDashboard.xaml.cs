using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace Intento4
{
    public partial class AdminDashboard : Window
    {
        // Base de datos y GridFS
        private readonly IMongoDatabase _database;
        private readonly GridFSBucket _gridFS;

        // Lista de canciones para la interfaz
        public ObservableCollection<Cancion> Canciones { get; set; } = new ObservableCollection<Cancion>();

        public AdminDashboard()
        {
            InitializeComponent();

            // Cadena de conexión a MongoDB Atlas
            string connectionString = "mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/myDatabase?retryWrites=true&w=majority";
            var client = new MongoClient(connectionString);

            _database = client.GetDatabase("Musify");
            _gridFS = new GridFSBucket(_database);

            // Cargar las canciones desde la base de datos al iniciar
            CargarCancionesDesdeBaseDeDatos();
        }

        // Método para cargar las canciones desde la base de datos
        private void CargarCancionesDesdeBaseDeDatos()
        {
            try
            {
                Canciones.Clear();
                var coleccion = _database.GetCollection<BsonDocument>("Cancion");
                var cancionesDb = coleccion.Find(new BsonDocument()).ToList();

                foreach (var cancion in cancionesDb)
                {
                    Canciones.Add(new Cancion
                    {
                        titulo = cancion.Contains("titulo") ? cancion["titulo"].AsString : string.Empty,
                        artista = cancion.Contains("artista") ? cancion["artista"].AsString : string.Empty,
                        album = cancion.Contains("album") ? cancion["album"].AsString : string.Empty,
                        genero = cancion.Contains("genero") ? cancion["genero"].AsString : string.Empty,
                        momento_dia = cancion.Contains("momento_dia") ? cancion["momento_dia"].AsString : string.Empty,
                        estado_animo = cancion.Contains("estado_animo") ? cancion["estado_animo"].AsString : string.Empty,
                        actividad = cancion.Contains("actividad") ? cancion["actividad"].AsString : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las canciones: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para agregar una canción
        private async void AgregarCancion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTitulo.Text) || string.IsNullOrWhiteSpace(txtArtista.Text) ||
                    string.IsNullOrWhiteSpace(txtAlbum.Text) || string.IsNullOrWhiteSpace(txtRutaArchivo.Text))
                {
                    MessageBox.Show("Por favor, llena todos los campos obligatorios (Título, Artista, Álbum y Ruta).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var filePath = txtRutaArchivo.Text;
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("El archivo no existe en la ruta especificada.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var fileName = Path.GetFileName(filePath);

                var options = new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { "titulo", txtTitulo.Text },
                        { "artista", txtArtista.Text },
                        { "album", txtAlbum.Text },
                        { "genero", cmbGenero.SelectedItem != null ? ((ComboBoxItem)cmbGenero.SelectedItem).Content.ToString() : string.Empty },
                        { "momento_dia", cmbMomentoDia.SelectedItem != null ? ((ComboBoxItem)cmbMomentoDia.SelectedItem).Content.ToString() : string.Empty },
                        { "estado_animo", cmbEstadoAnimo.SelectedItem != null ? ((ComboBoxItem)cmbEstadoAnimo.SelectedItem).Content.ToString() : string.Empty },
                        { "actividad", cmbActividad.SelectedItem != null ? ((ComboBoxItem)cmbActividad.SelectedItem).Content.ToString() : string.Empty }
                    }
                };

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var archivoId = await _gridFS.UploadFromStreamAsync(fileName, stream, options);

                    MessageBox.Show($"Canción registrada exitosamente con ID: {archivoId}.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    Canciones.Add(new Cancion
                    {
                        titulo = txtTitulo.Text,
                        artista = txtArtista.Text,
                        album = txtAlbum.Text,
                        genero = options.Metadata["genero"].AsString,
                        momento_dia = options.Metadata["momento_dia"].AsString,
                        estado_animo = options.Metadata["estado_animo"].AsString,
                        actividad = options.Metadata["actividad"].AsString
                    });

                    LimpiarFormulario();
                }
            }
            catch (MongoConnectionException connEx)
            {
                MessageBox.Show($"Error de conexión a la base de datos: {connEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para actualizar una canción
        // Método para actualizar una canción
        private async void ActualizarCancion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar que los campos clave estén llenos
                if (string.IsNullOrWhiteSpace(txtTitulo.Text) || string.IsNullOrWhiteSpace(txtArtista.Text) ||
                    string.IsNullOrWhiteSpace(txtAlbum.Text))
                {
                    MessageBox.Show("Por favor, llena los campos obligatorios (Título, Artista y Álbum) para identificar la canción.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Crear el filtro para buscar la canción en la base de datos
                var filtro = Builders<GridFSFileInfo>.Filter.And(
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.titulo", txtTitulo.Text),
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.artista", txtArtista.Text),
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.album", txtAlbum.Text)
                );

                // Buscar el archivo correspondiente
                var archivos = await _gridFS.FindAsync(filtro);
                var archivo = await archivos.FirstOrDefaultAsync();

                if (archivo == null)
                {
                    MessageBox.Show("No se encontró una canción con los datos proporcionados.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Crear un objeto para los nuevos metadatos basados en los valores de la interfaz
                var nuevosMetadatos = new BsonDocument
        {
            { "titulo", txtTitulo.Text },
            { "artista", txtArtista.Text },
            { "album", txtAlbum.Text },
            { "genero", cmbGenero.SelectedItem != null ? ((ComboBoxItem)cmbGenero.SelectedItem).Content.ToString() : string.Empty },
            { "momento_dia", cmbMomentoDia.SelectedItem != null ? ((ComboBoxItem)cmbMomentoDia.SelectedItem).Content.ToString() : string.Empty },
            { "estado_animo", cmbEstadoAnimo.SelectedItem != null ? ((ComboBoxItem)cmbEstadoAnimo.SelectedItem).Content.ToString() : string.Empty },
            { "actividad", cmbActividad.SelectedItem != null ? ((ComboBoxItem)cmbActividad.SelectedItem).Content.ToString() : string.Empty }
        };

                // Actualizar los metadatos del archivo en GridFS (sobrescribir los metadatos sin crear un nuevo archivo)
                var opciones = new GridFSUploadOptions
                {
                    Metadata = nuevosMetadatos
                };

                // Descargar el archivo existente
                var contenidoArchivo = await _gridFS.OpenDownloadStreamAsync(archivo.Id);

                // Sobrescribir el archivo con los nuevos metadatos (sin crear un nuevo archivo)
                using (var stream = new MemoryStream())
                {
                    await contenidoArchivo.CopyToAsync(stream);
                    stream.Position = 0; // Resetear el stream para cargarlo de nuevo

                    // Eliminar el archivo actual (opcional)
                    await _gridFS.DeleteAsync(archivo.Id);

                    // Subir nuevamente el archivo con los nuevos metadatos, sin crear un nuevo documento
                    await _gridFS.UploadFromStreamAsync(archivo.Filename, stream, opciones);
                }

                MessageBox.Show("Canción actualizada exitosamente.",
                                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpiar el formulario tras la actualización
                LimpiarFormulario();
            }
            catch (MongoConnectionException connEx)
            {
                MessageBox.Show($"Error de conexión a la base de datos: {connEx.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        // Método para eliminar una canción
        private async void EliminarCancion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar que los campos clave estén llenos
                if (string.IsNullOrWhiteSpace(txtTitulo.Text) || string.IsNullOrWhiteSpace(txtArtista.Text) ||
                    string.IsNullOrWhiteSpace(txtAlbum.Text))
                {
                    MessageBox.Show("Por favor, llena los campos obligatorios (Título, Artista y Álbum) para identificar la canción.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Crear el filtro para buscar la canción en la base de datos
                var filtro = Builders<GridFSFileInfo>.Filter.And(
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.titulo", txtTitulo.Text),
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.artista", txtArtista.Text),
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.album", txtAlbum.Text)
                );

                // Buscar el archivo correspondiente
                var archivos = await _gridFS.FindAsync(filtro);
                var archivo = await archivos.FirstOrDefaultAsync();

                if (archivo == null)
                {
                    MessageBox.Show("No se encontró una canción con los datos proporcionados.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Eliminar el archivo de GridFS
                await _gridFS.DeleteAsync(archivo.Id);

                MessageBox.Show("Canción eliminada exitosamente.",
                                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpiar el formulario tras la eliminación
                LimpiarFormulario();
            }
            catch (MongoConnectionException connEx)
            {
                MessageBox.Show($"Error de conexión a la base de datos: {connEx.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void BuscarCancion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar que los campos clave estén llenos
                if (string.IsNullOrWhiteSpace(txtTitulo.Text) || string.IsNullOrWhiteSpace(txtArtista.Text) ||
                    string.IsNullOrWhiteSpace(txtAlbum.Text))
                {
                    MessageBox.Show("Por favor, llena los campos obligatorios (Título, Artista y Álbum) para buscar la canción.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Crear el filtro para buscar la canción en la base de datos
                var filtro = Builders<GridFSFileInfo>.Filter.And(
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.titulo", txtTitulo.Text),
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.artista", txtArtista.Text),
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.album", txtAlbum.Text)
                );

                // Buscar el archivo correspondiente
                var archivos = await _gridFS.FindAsync(filtro);
                var archivo = await archivos.FirstOrDefaultAsync();

                if (archivo == null)
                {
                    MessageBox.Show("No se encontró una canción con los datos proporcionados.",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Reflejar los datos en la interfaz
                txtTitulo.Text = archivo.Metadata.GetValue("titulo", "").AsString;
                txtArtista.Text = archivo.Metadata.GetValue("artista", "").AsString;
                txtAlbum.Text = archivo.Metadata.GetValue("album", "").AsString;
                txtRutaArchivo.Text = archivo.Filename; // Mostrar la ruta del archivo, si aplica.

                // Configurar ComboBox para seleccionar el valor correspondiente
                SeleccionarItemComboBox(cmbGenero, archivo.Metadata.GetValue("genero", "").AsString);
                SeleccionarItemComboBox(cmbMomentoDia, archivo.Metadata.GetValue("momento_dia", "").AsString);
                SeleccionarItemComboBox(cmbEstadoAnimo, archivo.Metadata.GetValue("estado_animo", "").AsString);
                SeleccionarItemComboBox(cmbActividad, archivo.Metadata.GetValue("actividad", "").AsString);

                MessageBox.Show("Canción encontrada y cargada en la interfaz.",
                                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MongoConnectionException connEx)
            {
                MessageBox.Show($"Error de conexión a la base de datos: {connEx.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error inesperado: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método auxiliar para seleccionar el ítem correcto en un ComboBox
        private void SeleccionarItemComboBox(ComboBox comboBox, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString().Equals(valor, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }


        // Método auxiliar para encontrar el ComboBoxItem correspondiente en los ComboBoxes
        private ComboBoxItem FindComboBoxItem(ComboBox comboBox, string value)
        {
            foreach (var item in comboBox.Items)
            {
                if ((item as ComboBoxItem)?.Content.ToString() == value)
                {
                    return item as ComboBoxItem;
                }
            }
            return null;
        }


        // Limpiar el formulario después de agregar/actualizar/eliminar
        private void LimpiarFormulario()
        {
            txtTitulo.Text = string.Empty;
            txtArtista.Text = string.Empty;
            txtAlbum.Text = string.Empty;
            cmbGenero.SelectedIndex = -1;
            cmbMomentoDia.SelectedIndex = -1;
            cmbEstadoAnimo.SelectedIndex = -1;
            cmbActividad.SelectedIndex = -1;
        }
    }
}
