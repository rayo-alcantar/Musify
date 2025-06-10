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

        // Manejo del clic en el botón "Ingresar"
        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            // Obtener los valores de los campos de usuario y contraseña
            string usuario = txtNombre.Text;
            string contraseña = txtContraseña.Password;

            // Validar el usuario y la contraseña
            if (usuario == "Itzel" && contraseña == "Spiderman")
            {
                // Si las credenciales son correctas, cerrar esta ventana
                MessageBox.Show("Acceso concedido.", "Bienvenido", MessageBoxButton.OK, MessageBoxImage.Information);

                // Aquí puedes agregar el código para abrir otra ventana o realizar otras acciones
                // Ejemplo:
                // MainWindow mainWindow = new MainWindow();
                AdminDashboard adminDashboard = new AdminDashboard();
                adminDashboard.Show();
                this.Close();
                // mainWindow.Show();
                // this.Close(); // Cierra la ventana actual
            }
            else
            {
                // Si las credenciales son incorrectas, mostrar un mensaje de error
                MessageBox.Show("Usuario o contraseña incorrectos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
