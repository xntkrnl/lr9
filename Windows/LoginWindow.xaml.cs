using lr9.Code;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace lr9
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            SafeModeCheckBox.IsEnabled = IsRunAsAdmin();
        }

        public static bool IsRunAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpBox.Text;
            string port = PortBox.Text;
            string database = databaseBox.Text;
            string login = UsernameBox.Text;
            string password = PasswordBox.Password;

            bool safeMode = SafeModeCheckBox.IsChecked ?? false;
            DBCommandManager.Instance.safeMode = safeMode;

            LoginButton.IsEnabled = false;
            var result = await DBLoginManager.Instance.TryConnect(ip, port, database, login, password);

            if (result)
            {
                MainWindow main = new MainWindow();
                main.Show();

                this.Close();
            }
            LoginButton.IsEnabled = true;
        }
    }
}
