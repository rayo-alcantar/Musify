using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Windows;
using Intento4.Models;
using Intento4.Servicios;

namespace Intento4
{
    /// <summary>
    /// Ventana de inicio de sesión para usuarios.
    /// Permite a los usuarios ingresar sus credenciales y acceder a la aplicación principal.
    /// </summary>
    public partial class LoginWindow : Window
    {
        // Instancia de la base de datos MongoDB utilizada para la validación de usuarios.
        private readonly IMongoDatabase _database;

        public LoginWindow()
        {
            InitializeComponent();

            // Inicializa la conexión a la base de datos MongoDB.
            // Utiliza la cadena de conexión centralizada definida en MongoDBService.
            var client = new MongoClient(MongoDBService.ConnectionString);
            // Obtiene la base de datos específica ("Musify"). El nombre podría centralizarse también.
            _database  = client.GetDatabase("Musify");
        }

        /* ========== Login de administrador ========== */
        // Este método parece estar fuera del alcance de las modificaciones recientes,
        // pero se mantiene su funcionalidad original.
        private void LoginAdminButton_Click(object sender, RoutedEventArgs e)
        {
            var adminLogin = new LoginAdministrador();
            adminLogin.Show();

            Application.Current.MainWindow = adminLogin;
            this.Hide();
        }

        /* ========== Login de usuario normal ========== */
        /// <summary>
        /// Maneja el evento Click del botón de inicio de sesión.
        /// Valida las credenciales del usuario y, si son correctas,
        /// abre la ventana principal (MainWindow) pasando la información del usuario.
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTextBox.Text.Trim();
            string password = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Por favor, ingrese usuario y contraseña.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var usuarios = _database.GetCollection<Usuario>("Usuario");
            var filtro   = Builders<Usuario>.Filter.Eq(u => u.correo_electronico, username);
            var usuario  = usuarios.Find(filtro).FirstOrDefault();

            if (usuario is null)
            {
                MessageBox.Show("El usuario no existe.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // NOTA: La comparación de contraseñas se realiza en texto plano.
            // En un entorno de producción, se deben utilizar hashes de contraseña.
            if (password != usuario.contraseña)
            {
                MessageBox.Show("Contraseña incorrecta.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Inicio de sesión exitoso.",
                            "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            // Al iniciar sesión correctamente, se crea una instancia de MainWindow
            // y se le pasa el objeto 'usuario' completo. Esto permite a MainWindow
            // acceder a la información del usuario logueado (CurrentUser).
            var mainWindow = new MainWindow(usuario);
            mainWindow.Show();

            Application.Current.MainWindow = mainWindow; // Establece la nueva ventana principal.
            this.Close(); // Cierra la ventana de login.
        }

        /* ========== Placeholders accesibles ========== */
        // Estos métodos manejan la visibilidad de los placeholders en los campos de texto.
        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e) =>
            UsernamePlaceholder.Visibility = Visibility.Collapsed;

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e) =>
            UsernamePlaceholder.Visibility =
                string.IsNullOrEmpty(usernameTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e) =>
            PasswordPlaceholder.Visibility = Visibility.Collapsed;

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e) =>
            PasswordPlaceholder.Visibility =
                string.IsNullOrEmpty(passwordBox.Password) ? Visibility.Visible : Visibility.Collapsed;

        /* ========== Registro ========== */
        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            var registrar = new RegistrarWindow();
            registrar.Show();
        }
    }
}
