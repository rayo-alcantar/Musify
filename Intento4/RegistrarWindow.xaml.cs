using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Windows;

namespace Intento4
{
    public partial class RegistrarWindow : Window
    {
        private IMongoDatabase _database;

        public RegistrarWindow()
        {
            InitializeComponent();

            // Cadena de conexión a MongoDB Atlas
            string connectionString = "mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority"; // Actualiza con tus credenciales y base de datos
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("Musify"); // Nombre de la base de datos
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar los datos ingresados
                if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtCorreo.Text) ||
                    string.IsNullOrWhiteSpace(txtContraseña.Password) || dpFecha.SelectedDate == null)
                {
                    MessageBox.Show("Por favor, completa todos los campos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Preparar los datos para insertar
                var coleccion = _database.GetCollection<BsonDocument>("Usuario");

                var User = new BsonDocument
                {
                    { "nombre", txtNombre.Text },
                    { "correo_electronico", txtCorreo.Text },
                    { "contraseña", txtContraseña.Password },
                    { "fecha_nacimiento", new BsonDateTime(dpFecha.SelectedDate.Value) }
                };

                // Insertar en la colección
                coleccion.InsertOne(User);

                // Confirmar al usuario
                MessageBox.Show("Usuario registrado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); // Cierra la ventana después de registrar
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar usuario: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


