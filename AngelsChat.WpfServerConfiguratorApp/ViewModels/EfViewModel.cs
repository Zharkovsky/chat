using AngelsChat.Server.Core;
using AngelsChat.Server.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.WpfServerConfiguratorApp.ViewModels
{
    public class EfViewModel : ViewModelBase
    {
        private readonly EfSettings _settings;
        public EfViewModel(EfSettings settings)
        {
            _settings = settings;
        }
        public string EFSource
        {
            get => _settings.Source;
            set
            {
                _settings.Source = value;
                OnPropertyChanged();
            }
        }
        public string EFName
        {
            get => _settings.Name;
            set
            {
                _settings.Name = value;
                OnPropertyChanged();
            }
        }
    }
}
