using AngelsChat.WpfClientApp.Helpers;
using System.Windows.Controls;

namespace AngelsChat.WpfClientApp.Views
{
    /// <summary>
    /// Логика взаимодействия для EditUserProfileView.xaml
    /// </summary>
    public partial class EditUserProfileView : UserControl, IHavePassword, IHaveName
    {
        public EditUserProfileView()
        {
            InitializeComponent();
        }

        public System.Security.SecureString Password
        {
            get
            {
                return UserPassword.SecurePassword;
            }
        }

        public string UserName
        {
            get
            {
                return UserNameInVM.Text;
            }
        }
    }
}
