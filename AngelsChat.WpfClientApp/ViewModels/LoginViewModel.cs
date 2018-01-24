using System;
using System.Windows.Input;
using System.Threading.Tasks;
using AngelsChat.Client;
using AngelsChat.WpfClientApp.Helpers;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        // Логин
        private string _UserName;
        public string UserName
        {
            get
            {
                return _UserName;
            }
            set
            {
                _UserName = value;
                OnPropertyChanged();
            }
        }

        // Пароль
        private string _PasswordInVM;
        public string PasswordInVM
        {
            get
            {
                return _PasswordInVM;
            }
            set
            {
                _PasswordInVM = value;
                OnPropertyChanged("PasswordInVM");
            }
        }

        ClientService _client;
        ChatViewModel _chatViewModel;
        SignUpViewModel _signUpViewModel;

        public ICommand LoginCommand { get; private set; }

        public LoginViewModel(ClientService client, SignUpViewModel signUpViewModel, ChatViewModel chatViewModel)
        {
            _client = client;
            _signUpViewModel = signUpViewModel;
            _chatViewModel = chatViewModel;
            LoginCommand = new RelayCommand(Login);
            SignUp = new RelayCommand(SignUpAction);
            ConnectionSettingsCommand = new RelayCommand(ConnectionSettingsCommandAction);
            OnLoading = false;
        }

        // Команда открытия окна регистрации
        private RelayCommand signUp;
        public RelayCommand SignUp
        {
            get => signUp;
            private set
            {
                signUp = value;
                OnPropertyChanged();
            }
        }

        private void SignUpAction(object obj)
        {
            _chatViewModel.Body = _chatViewModel.SignUpViewModel;
            Log.Trace("Открытие окно ргистрации");
        }

        // Команда открытия окна настроек
        private RelayCommand connectionSettingsCommand;
        public RelayCommand ConnectionSettingsCommand
        {
            get => connectionSettingsCommand;
            private set
            {
                connectionSettingsCommand = value;
                OnPropertyChanged();
            }
        }

        private void ConnectionSettingsCommandAction(object obj)
        {
            _chatViewModel.Body = _chatViewModel.SettingsViewModel;
            Log.Trace("Открытие окна настроек");
        }

        // Флаг прогрузки
        private bool _onLoading;
        public bool OnLoading
        {
            get => _onLoading;
            set
            {
                _onLoading = value;
                OnPropertyChanged();
            }
        }

        // Сообщение от сервера к пользователю
        private string _userMessage;
        public string UserMessage
        {
            get { return _userMessage; }
            set
            {
                _userMessage = value;
                Log.Trace("Сообщения сервера - {0}", _userMessage);
                OnPropertyChanged();
            }
        }

        // Авторизация
        public void Login(object parameter)
        {
            Task.Factory.StartNew(() =>
            {
                Log.Trace("Запуск авторизации");
                OnLoading = true;
                // Подключение
                Settings settings = Settings.Read();
                string endpointAdress = $"net.tcp://{settings.Ip}:{settings.Port}/AngelsChat/";
                _client.Open(endpointAdress);
                if (!_client.CheckConnection())
                {
                    UserMessage = "Ошибка подключения";
                    OnLoading = false;
                    Log.Debug("Ошибка подключения пользователя");
                    return;
                }
                Log.Debug("Подключение прошло успешно");

                Log.Trace("Отправка данных пользователя");
                // Авторизация
                if (parameter is IHavePassword passwordContainer)
                    PasswordInVM = ConvertToUnsecureString(passwordContainer.Password);
                if (_client.Authorization(_UserName, _PasswordInVM))
                {
                    _chatViewModel.Body = _chatViewModel.MainChatViewModel;
                    _chatViewModel.StartInit();
                    Log.Info("Авторизация успешна");
                }
                else
                {
                    UserMessage = "Неверные логин и пароль";
                    Log.Info("Неверные логин и пароль");
                }

                OnLoading = false;
                Log.Trace("Конец авторизации");
            });
        }

        private string ConvertToUnsecureString(System.Security.SecureString securePassword)
        {
            if (securePassword == null)
            {
                return string.Empty;
            }

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }

}
