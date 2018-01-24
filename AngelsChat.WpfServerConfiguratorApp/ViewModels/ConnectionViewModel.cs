using AngelsChat.Server.Core;
using AngelsChat.Server.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.WpfServerConfiguratorApp.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
        private readonly ConnectionSettings _settings;

        public ConnectionViewModel(ConnectionSettings settings)
        {
            _settings = settings;
        }
        public string Ip
        {
            get => _settings.Ip;
            set
            {
                _settings.Ip = value;
                OnPropertyChanged();
            }
        }
        public string Port
        {
            get => _settings.Port;
            set
            {
                _settings.Port = value;
                OnPropertyChanged();
            }
        }
    }
}
