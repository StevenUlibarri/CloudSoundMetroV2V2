using CloudSoundMetroV2;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for CreateAccountWindow.xaml
    /// </summary>
    public partial class CreateAccountWindow : Window
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public CreateAccountWindow()
        {
            InitializeComponent();
            UserName = null;
            Password = null;
            UserNameBox.Focus();

        }
        private void CreateAcc_Click(object sender, RoutedEventArgs e)
        {
            ValidateUserName(this.UserNameBox.Text, this.PasswordBox.Password);
            try
            {
                if (!string.IsNullOrEmpty(UserNameBox.Text) && !string.IsNullOrEmpty(PasswordBox.Password) && !string.IsNullOrEmpty(PasswordBoxConfrim.Password))
                {

                    UserName = UserNameBox.Text;
                    Password = PasswordBox.Password;
                    if (Password != PasswordBoxConfrim.Password)
                    {
                        PasswordInvalid.Visibility = Visibility.Visible;
                        UserName = null;
                        Password = null;
                        PasswordBox.Password = "";
                        PasswordBoxConfrim.Password = "";

                    }
                    else
                    {
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("User Name and Password must not be blank.");
                }
            }
            catch (StackOverflowException)
            {
                
                
            }
        }
        public void ValidateUserName(string usrName, string pass)
        {
            using (var context = new CloudMp3Context())
            {
                var query = (from u in context.Users
                             where u.U_UserName == usrName
                             select u).SingleOrDefault();
                if (query == null)
                {
                    User u = new User();
                    u.U_UserName = usrName;
                    u.U_Password = pass;
                    context.Users.Add(u);
                    context.SaveChanges();

                }
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
