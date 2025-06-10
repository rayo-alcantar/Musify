using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Intento4
{
    public partial class LoginWindow : Window
    {
        private IMongoDatabase _database;

        public LoginWindow()
        {
            // Cadena de conexión a MongoDB Atlas
            string connectionString = "mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/myDatabase?retryWrites=true&w=majority";
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("Musify");
            InitializeComponent();
        }

        private void LoginAdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Crear la instancia de la ventana de administrador
            LoginAdministrador loginAdministrador = new LoginAdministrador();

            // Mostrar la ventana de administrador
            loginAdministrador.Show();

            // Cerrar la ventana actual (LoginWindow)
            this.Close();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Obtener los valores de los campos
            string username = usernameTextBox.Text;
            string password = passwordBox.Password;

            // Lógica de validación
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Por favor, ingrese usuario y contraseña.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar usuario y contraseña en MongoDB
            var coleccion = _database.GetCollection<BsonDocument>("Usuario"); // Nombre de tu colección

            var filtro = Builders<BsonDocument>.Filter.Eq("correo_electronico", username);
            var usuario = coleccion.Find(filtro).FirstOrDefault();

            if (usuario != null)
            {
                // Validar contraseña
                string contrasenaGuardada = usuario.GetValue("contraseña").AsString;

                if (password == contrasenaGuardada) // Idealmente, usa hashes para mayor seguridad
                {
                    MessageBox.Show("Inicio de sesión exitoso", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Cerrar la ventana de inicio de sesión y abrir la siguiente ventana
                    MainWindow mainWindow = new MainWindow();

                    mainWindow.Show();
                    this.Close(); // Cerrar la ventana de inicio de sesión
                }
                else
                {
                    MessageBox.Show("Contraseña incorrecta", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("El usuario no existe", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Manejo de evento GotFocus y LostFocus para el campo "Usuario"
        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UsernamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(usernameTextBox.Text))
            {
                UsernamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        // Manejo de evento GotFocus y LostFocus para el campo "Contraseña"
        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                PasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }

        // Lógica de la ventana de registro
        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            RegistrarWindow registrarWindow = new RegistrarWindow();

            // Mostrar la ventana de registro
            registrarWindow.Show();
        }
    }

   
    }


