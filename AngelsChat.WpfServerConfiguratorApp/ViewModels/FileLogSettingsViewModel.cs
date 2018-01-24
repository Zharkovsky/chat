using AngelsChat.Server.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.WpfServerConfiguratorApp.ViewModels
{
    public class FileLogSettingsViewModel : ViewModelBase
    {
        private readonly FileRuleLogSettings _settings;
        public FileLogSettingsViewModel(FileRuleLogSettings settings) => _settings = settings;
        public string FileSource
        {
            get => _settings.FileSource;
            set
            {
                _settings.FileSource = value;
                OnPropertyChanged();
            }
        }
    }
}
