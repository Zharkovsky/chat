using AngelsChat.Server.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.WpfServerConfiguratorApp.ViewModels
{
    public class DatabaseRuleLogSettingsViewModel : ViewModelBase
    {
        private readonly DatabaseRuleLogSettings _settings;
        public DatabaseRuleLogSettingsViewModel(DatabaseRuleLogSettings settings) => _settings = settings;
        public string MsSQLSource
        {
            get => _settings.MsSQLSource;
            set
            {
                _settings.MsSQLSource = value;
                OnPropertyChanged();
            }
        }
        public string MsSQLName
        {
            get => _settings.MsSQLName;
            set
            {
                _settings.MsSQLName = value;
                OnPropertyChanged();
            }
        }
    }
}
