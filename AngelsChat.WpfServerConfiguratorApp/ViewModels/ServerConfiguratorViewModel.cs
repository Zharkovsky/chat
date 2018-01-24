using AngelsChat.WpfServerConfiguratorApp.Helpers;
using System;
using AngelsChat.Server.Settings;

namespace AngelsChat.WpfServerConfiguratorApp.ViewModels
{
    public class ServerConfiguratorViewModel : ViewModelBase
    {
        private Settings _settings;

        private ConnectionViewModel _connectionViewModel;
        public ConnectionViewModel ConnectionViewModel
        {
            get => _connectionViewModel;
            set
            {
                _connectionViewModel = value;
                OnPropertyChanged("ConnectionViewModel");
            }
        }

        private LogViewModel _logViewModel;
        public LogViewModel LogViewModel
        {
            get => _logViewModel;
            set
            {
                _logViewModel = value;
                OnPropertyChanged("LogViewModel");
            }
        }

        private EfViewModel _efViewModel;
        public EfViewModel EfViewModel
        {
            get => _efViewModel;
            set
            {
                _efViewModel = value;
                OnPropertyChanged("EfViewModel");
            }
        }

        public ServerConfiguratorViewModel() : this(Settings.Read()) { }

        public ServerConfiguratorViewModel(Settings settings)
        {
            _settings = settings;
            _connectionViewModel = new ConnectionViewModel(settings.Connection);
            _logViewModel = new LogViewModel(settings.Log);
            _efViewModel = new EfViewModel(settings.Ef);
        }

        private string _userMessage;
        public string UserMessage
        {
            get => _userMessage;
            set
            {
                _userMessage = value;
                OnPropertyChanged("UserMessage");
            }
        }

    }
}