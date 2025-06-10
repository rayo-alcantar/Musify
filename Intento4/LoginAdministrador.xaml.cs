using System;
using System.Windows;

namespace Intento4
{
    public partial class LoginAdministrador : Window
    {
        public LoginAdministrador()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Botón “Ingresar” de la ventana de administrador.
        /// Valida credenciales, abre el dashboard y oculta el login.
        /// </summary>
        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string usuario     = txtNombre.Text.Trim();
            string contraseña  = txtContraseña.Password;

            if (usuario.Equals("Itzel", StringComparison.OrdinalIgnoreCase) &&
                contraseña == "Spiderman")
            {
                MessageBox.Show("Acceso concedido.",
                                "Bienvenido", MessageBoxButton.OK, MessageBoxImage.Information);

                // Abre el panel de administración
                var adminDashboard = new AdminDashboard();
                adminDashboard.Show();

                // Convierte el dashboard en la nueva ventana principal
                Application.Current.MainWindow = adminDashboard;

                // Oculta (NO cierra) la ventana de login para evitar que WPF apague la app
                this.Hide();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
