using AngelsChat.WpfClientApp.Helpers;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AngelsChat.WpfClientApp.Views
{
    /// <summary>
    /// Логика взаимодействия для SignUpView.xaml
    /// </summary>
    public partial class SignUpView : UserControl, IHavePassword
    {
        public SignUpView()
        {
            InitializeComponent();
        }
        public System.Security.SecureString Password
        {
            get
            {
                return SignUpPassword.SecurePassword;
            }
        }
    }
}
