using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngelsChat.Client;
using System.Windows.Controls;
using AngelsChat.WpfClientApp.Helpers;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class SignUpViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        // Логин
        private string _UserName;
        public string UserName
        {
            get => _UserName;
            set
            {
                _UserName = value;
                OnPropertyChanged();
            }
        }

        // Пароль
        private string _Password;
        public string Password
        {
            get => _Password;
            set
            {
                _Password = value;
                OnPropertyChanged();
            }
        }

        IClientService _client;
        ChatRoomsViewModel _chatViewModel;

        public ICommand SignUpCommand { get; private set; }

        public SignUpViewModel(IClientService client, ChatRoomsViewModel chatViewModel)
        {
            _client = client;
            _chatViewModel = chatViewModel;
            SignUpCommand = new RelayCommand(Sign);
            BackCommand = new RelayCommand(BackCommandAction);
        }
        
        

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

        private string _userMessage;
        public string UserMessage
        {
            get { return _userMessage; }
            set { _userMessage = value; OnPropertyChanged("UserMessage"); }
        }

        // Регистрация
        private void Sign(object parameter)
        {
            Log.Trace("Начало регистрации");
            Task.Factory.StartNew(() =>
            {
                OnLoading = true;

                // Подключение
                Settings settings = Settings.Read();

                string endpointAdress = $"net.tcp://{settings.Ip}:{settings.Port}/AngelsChat/";
                _client.Open(endpointAdress);
                if (!_client.CheckConnection())
                {
                    UserMessage = "Ошибка подключения";
                    OnLoading = false;
                    return;
                }
                Log.Debug("Подключение успешно");

                // Регистрация
                IHavePassword passwordContainer = parameter as IHavePassword;
                if (passwordContainer != null)
                    Password = ConvertToUnsecureString(passwordContainer.Password);
                if (_client.Registration(_UserName, _Password) == true)
                {
                    _chatViewModel.Body = _chatViewModel.MainChatViewModel;
                    _chatViewModel.StartInit();
                    Log.Info("Регистрация успешна");
                }
                else
                {
                    UserMessage = "Такой логин уже существует";
                    Log.Info("Регистрация не удалась");
                }

                OnLoading = false;
                Log.Trace("Конец регистрации");
            });
        }

        private RelayCommand backCommand;
        public RelayCommand BackCommand
        {
            get => backCommand;
            private set
            {
                backCommand = value;
                OnPropertyChanged();
            }
        }
        private void BackCommandAction(object obj)
        {
            _chatViewModel.Body = _chatViewModel.LoginVM;
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
