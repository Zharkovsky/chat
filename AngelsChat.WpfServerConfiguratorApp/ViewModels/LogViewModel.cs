using AngelsChat.Server.Core;
using AngelsChat.Server.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using AngelsChat.WpfServerConfiguratorApp.Helpers;

namespace AngelsChat.WpfServerConfiguratorApp.ViewModels
{
    public class LogViewModel : ViewModelBase
    {
        LogSettings _settings;
        private ViewModelBase _logRule;

        public LogViewModel(LogSettings settings)
        {
            this._settings = settings;

            if (_settings.Rule is FileRuleLogSettings fileRule)
            {
                LogRule = new FileLogSettingsViewModel(fileRule);
            }
            else if (_settings.Rule is DatabaseRuleLogSettings dbRule)
            {
                LogRule = new DatabaseRuleLogSettingsViewModel(dbRule);
            }
        }

        public LogLevel LogLevel
        {
            get => (LogLevel)Enum.Parse(typeof(LogLevel), _settings.Lvl);
            set
            {
                _settings.Lvl = value.ToString();
                OnPropertyChanged();
            }
        }
        public LogTarget LogTarget
        {
            get
            {
                if (_settings.Rule is FileRuleLogSettings)
                {
                    return LogTarget.File;
                }
                else if (_settings.Rule is DatabaseRuleLogSettings)
                {
                    return LogTarget.Db;
                }
                return default(LogTarget);
            }
            set
            {
                switch (value)
                {
                    case LogTarget.File:
                        {
                            var rule = new FileRuleLogSettings();
                            _settings.Rule = rule;
                            LogRule = new FileLogSettingsViewModel(rule);
                            break;
                        }
                    case LogTarget.Db:
                        {
                            var rule = new DatabaseRuleLogSettings();
                            _settings.Rule = rule;
                            LogRule = new DatabaseRuleLogSettingsViewModel(rule);
                            break;
                        }
                }
                OnPropertyChanged();
            }
        }

        public ViewModelBase LogRule
        {
            get { return _logRule; }
            set
            {
                _logRule = value;
                OnPropertyChanged();
            }
        }

        public IList<LogLevel> LogLevels { get { return Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>().ToList(); } }
        public IList<LogTarget> LogTargets { get { return Enum.GetValues(typeof(LogTarget)).Cast<LogTarget>().ToList(); } }
    }
}
