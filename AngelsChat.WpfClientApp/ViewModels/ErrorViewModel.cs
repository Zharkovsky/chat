using AngelsChat.WpfClientApp.Helpers;
using System;
using AngelsChat.Shared.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;
using AngelsChat.Client;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class ErrorViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private bool _errorFlag;
        public bool ErrorFlag
        {
            get => _errorFlag;
            set
            {
                _errorFlag = value;
                OnPropertyChanged();
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                Log.Debug("Новое сообщение об ошибке: {0}", value);
                OnPropertyChanged();
            }
        }

        private ClientService _client;
        LoginViewModel _loginViewModel;
        SignUpViewModel _signUpViewModel;
        ChatRoomsViewModel _chatViewModel;

        public ErrorViewModel(ClientService client, LoginViewModel loginViewModel, SignUpViewModel signUpViewModel, ChatRoomsViewModel chatViewModel)
        {
            _client = client;
            _loginViewModel = loginViewModel;
            _signUpViewModel = signUpViewModel;
            _chatViewModel = chatViewModel;
            Close = new RelayCommand(CloseAction);
        }

        public void Show(string error, bool reconnet = false, bool logout = false)
        {
            ErrorFlag = true;
            ErrorMessage = error;
            Log.Trace("Показать сообщение ошибки");
            if (logout)
                _chatViewModel.Body = _chatViewModel.LoginVM;
            else if (reconnet)
                Reconnect();
        }

        private void Reconnect()
        {
            // Подключение
            Settings settings = Settings.Read();
            string endpointAdress = $"net.tcp://{settings.Ip}:{settings.Port}/AngelsChat/";
            _client.Open(endpointAdress);
            App.Current.Dispatcher.Invoke(() =>
            {
                _chatViewModel.Rooms.Clear();
            });
            if (!_client.CheckConnection())
            {
                _loginViewModel.OnLoading = false;
                _chatViewModel.Body = _chatViewModel.LoginVM;
                Log.Debug("Ошибка подключения пользователя");
            }
            else
            {
                Log.Debug("Подключение прошло успешно");

                Log.Trace("Отправка данных пользователя");
                // Авторизация
                if (_client.Authorization(_loginViewModel.UserName, _loginViewModel.PasswordInVM))
                {
                    Log.Info("Авторизация успешна");
                    _chatViewModel.StartInit();
                }
                else
                {
                    Log.Info("Неверные логин и пароль");
                }
            }
        }

        private RelayCommand close;
        public RelayCommand Close
        {
            get => close;
            private set
            {
                close = value;
                OnPropertyChanged();
            }
        }
        private void CloseAction(object obj)
        {
            { ErrorFlag = false; Log.Trace("Закрыть сообщение ошибки"); }
        }
    }
}
