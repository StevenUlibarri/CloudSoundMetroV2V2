using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

namespace Cloudmp3.Windows
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }

        public Login()
        {
            InitializeComponent();
            UserNameBox.Focus();
            UserName = null;
            Password = null;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(UserNameBox.Text) && !string.IsNullOrEmpty(PasswordBox.Password))
            {

                UserName = UserNameBox.Text;
                Password = PasswordBox.Password;
                while (Password != PasswordBoxConfrim.Password)
                {
                    PasswordInvalid.Visibility = Visibility.Visible;
                    LoginButton_Click(null, null);
                }
                this.Close();
            }
            else
            {
                MessageBox.Show("User Name and Password must not be blank.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }



        private void CreateAccBox_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            CreateAccountWindow c = new CreateAccountWindow();
            c.Top = this.Top + 50;
            c.Left = this.Left + 50;
            c.ShowDialog();
        }
    }
}
