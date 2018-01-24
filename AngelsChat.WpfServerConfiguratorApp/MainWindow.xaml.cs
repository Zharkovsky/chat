using AngelsChat.Server.Settings;
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
using AngelsChat.WpfServerConfiguratorApp.ViewModels;

namespace AngelsChat.WpfServerConfiguratorApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Settings _settings;
        public ConnectionViewModel _connectionViewModel;
        public MainWindow()
        {
            InitializeComponent();
            LoadSetting();
        }

        private void LoadSetting()
        {
            _settings = Settings.Read();
            DataContext = new ServerConfiguratorViewModel(_settings);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            LoadSetting();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            _settings.Write();
        }
    }
}
