using AngelsChat.WpfClientApp.Helpers;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class SettingsViewModel: ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        Settings settings;
        public string Ip
        {
            get => settings.Ip;
            set
            {
                settings.Ip = value;
                OnPropertyChanged();
            }
        }
        public string Port
        {
            get => settings.Port;
            set
            {
                settings.Port = value;
                OnPropertyChanged();
            }
        }

        ChatViewModel _chatViewModel;

        public SettingsViewModel(ChatViewModel chatViewModel)
        {
            settings = Settings.Read();
            _chatViewModel = chatViewModel;
            Log.Info("Инициализация SettingsViewModel");
            BackFromSettings = new RelayCommand(BackFromSettingsAction);
            Back = new RelayCommand(BackAction);
        }

        private RelayCommand _backFromSettings;
        public RelayCommand BackFromSettings
        {
            get => _backFromSettings;
            private set
            {
                _backFromSettings = value;
                OnPropertyChanged();
            }
        }
        private void BackFromSettingsAction(object obj)
        {
            Log.Trace("Сохранить и выйти из настроек");
            settings.Write();
            _chatViewModel.Body = _chatViewModel.LoginVM;
        }

        private RelayCommand _back;
        public RelayCommand Back
        {
            get => _back;
            private set
            {
                _back = value;
                OnPropertyChanged();
            }
        }
        private void BackAction(object obj)
        {
            _chatViewModel.Body = _chatViewModel.LoginVM;
            settings = Settings.Read();
            Ip = settings.Ip;
            Port = settings.Port;
            Log.Trace("Выйти из настроек, вернуть прежние данные {Ip}:{Port}");
        }
    }
}
