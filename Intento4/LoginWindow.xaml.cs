using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Windows;

namespace Intento4
{
    /// <summary>
    /// Ventana de inicio de sesión de usuario.
    /// • Reasigna MainWindow antes de ocultar la ventana de login
    /// • Deja la conexión y la lógica de validación intactas
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly IMongoDatabase _database;

        public LoginWindow()
        {
            InitializeComponent();

            const string connectionString =
                "mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/myDatabase?retryWrites=true&w=majority";

            var client = new MongoClient(connectionString);
            _database  = client.GetDatabase("Musify");
        }

        /* ========== Login de administrador ========== */
        private void LoginAdminButton_Click(object sender, RoutedEventArgs e)
        {
            var adminLogin = new LoginAdministrador();
            adminLogin.Show();

            // 1 → nueva ventana principal
            Application.Current.MainWindow = adminLogin;

            // 2 → oculta el login
            this.Hide();
        }

        /* ========== Login de usuario normal ========== */
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

            var usuarios = _database.GetCollection<BsonDocument>("Usuario");
            var filtro   = Builders<BsonDocument>.Filter.Eq("correo_electronico", username);
            var usuario  = usuarios.Find(filtro).FirstOrDefault();

            if (usuario is null)
            {
                MessageBox.Show("El usuario no existe.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string contrasenaGuardada = usuario["contraseña"].AsString;

            if (password != contrasenaGuardada)          // (usa hashes en producción)
            {
                MessageBox.Show("Contraseña incorrecta.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Inicio de sesión exitoso.",
                            "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            var mainWindow = new MainWindow();
            mainWindow.Show();

            Application.Current.MainWindow = mainWindow; // 1
            this.Hide();                                 // 2
        }

        /* ========== Placeholders accesibles ========== */
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
