using AngelsChat.Client;
using Emgu.CV;
using System;
using System.Collections.ObjectModel;
using Emgu.CV.Structure;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NLog;
using AngelsChat.WpfClientApp.Helpers;
using AngelsChat.Shared.Data;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private ClientService _client;
        private string Name => _loginViewModel.UserName;

        public ObservableCollection<UserViewModel> Users { get; private set; }
        public ObservableCollection<MessageViewModel> Messages { get; private set; }

        private PhotoViewModel _makePhotoViewModel;
        public PhotoViewModel MakePhotoViewModel
        {
            get => _makePhotoViewModel;
            set
            {
                _makePhotoViewModel = value;
                OnPropertyChanged("PhotoVM");
            }
        }

        private SignUpViewModel _signUpViewModel;
        public SignUpViewModel SignUpViewModel
        {
            get => _signUpViewModel;
            set
            {
                _signUpViewModel = value;
                OnPropertyChanged("SignUpVM");
            }
        }

        private LoginViewModel _loginViewModel;
        public LoginViewModel LoginVM
        {
            get => _loginViewModel;
            set
            {
                _loginViewModel = value;
                OnPropertyChanged("LoginVM");
            }
        }

        private SettingsViewModel _settingsViewModel;
        public SettingsViewModel SettingsViewModel
        {
            get => _settingsViewModel;
            set
            {
                _settingsViewModel = value;
                OnPropertyChanged("SettingsViewModel");
            }
        }

        private ErrorViewModel _errorViewModel;
        public ErrorViewModel ErrorViewModel
        {
            get => _errorViewModel;
            set
            {
                _errorViewModel = value;
                OnPropertyChanged("ErrorViewModel");
            }
        }

        private SendFileViewModel _sendFileViewModel;
        public SendFileViewModel SendFileViewModel
        {
            get => _sendFileViewModel;
            set
            {
                _sendFileViewModel = value;
                OnPropertyChanged("SendFileViewModel");
            }
        }

        private ChatViewModel _chatViewModel;
        public ChatViewModel MainChatViewModel
        {
            get => _chatViewModel;
            set
            {
                _chatViewModel = value;
                OnPropertyChanged();
            }
        }

        private ViewModelBase _body;
        public ViewModelBase Body
        {
            get => _body;
            set
            {
                _body = value;
                OnPropertyChanged("Body");
            }
        }

        public delegate void ShowMessage(string text);
        ShowMessage ShowMessageInTray;
        public ChatViewModel(ClientService client, ShowMessage MainWindowShowMessage)
        {
            ShowMessageInTray = MainWindowShowMessage;
            _client = client;

            Messages = new ObservableCollection<MessageViewModel>();
            Users = new ObservableCollection<UserViewModel>();

            _makePhotoViewModel = new PhotoViewModel(_client, this);
            _signUpViewModel = new SignUpViewModel(_client, this);
            _loginViewModel = new LoginViewModel(_client, _signUpViewModel, this);
            _settingsViewModel = new SettingsViewModel(this);
            _errorViewModel = new ErrorViewModel(_client, _loginViewModel, _signUpViewModel, this);
            _sendFileViewModel = new SendFileViewModel(_client, this);

            MainChatViewModel = this;
            Body = LoginVM;

            Log.Info("Инициализация ChatViewModel");
        }

        private ViewModelBase _messageRule;
        public ViewModelBase MessageRule
        {
            get { return _messageRule; }
            set
            {
                _messageRule = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Инициализация данных:
        /// Все пользователи, их фото, статус онлайн
        /// </summary>
        public void StartInit()
        {
            InitializeInfo();
            InitializeCommands();
            InitializeEvents();
            vr.Init(_client.SendVoice);
            vp.Init();
            vp.Start();

        }

        private void InitializeInfo()
        {
            Log.Info("Инициализация данных");
            // Get all Users
            _client.GetUsers()
                .Select(u => new UserViewModel(u)).ToList()
                .ForEach(_ => App.Current.Dispatcher.Invoke(() => Users.Add(_)));
            Log.Debug("Загрузка всех пользователей");


            // Get online Users
            Users.ToList().ForEach(_ => _.Online = _client.IsOnline(_.Name));
            Log.Debug("Загрузка пользователей в онлайн");

            // Get Avatars
            Users.ToList().ForEach(_ => _.Image = _client.GetAvatar(_.Name));
            Log.Debug("Загрузка аватарок всех пользователей");

            // Set current user image
            _makePhotoViewModel.Image = Users.Where(_ => _.Name == Name).Select(_ => _.Image).FirstOrDefault();
            Log.Trace("Установка фото текущего пользователя");

            //Load messages
            AddLoadedMessages(_client.LoadMessages(lastMessageDate, 10));
        }

        private void InitializeCommands()
        {
            EndScrollCommand = new RelayCommand(EndScrollAction);
            OpenImage = new RelayCommand(OpenImageAction);
            MakeVideo = new RelayCommand(MakeVideoAction);
            MakePhoto = new RelayCommand(MakePhotoAction);
            SendMessage = new RelayCommand(SendMessageAction);
            SendFileMessage = new RelayCommand(SendFileMessageAction);
        }

        private void InitializeEvents()
        {
            _client.MessageRecived += ((message) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    MessageViewModel ms = new MessageViewModel(message, GetOwnerForMessage(message), _client.GetFile, _client.DownloadFile, _errorViewModel);
                    MessageRule = ms;
                    Log.Trace("Сообщение доставлено");
                    var user = Users.Where(_ => _.Name == message.Owner.Name).FirstOrDefault();
                    if (user == null) user = new UserViewModel(message.Owner.Name);
                    Messages.Insert(0, ms);
                    Log.Trace("Сообщение обработано");
                    ShowMessageInTray(message.Owner.Name + ": " + message.MessageText);
                });
            });
            _client.UserEntered += ((user) =>
            {
                Log.Trace("Пользователь вошел");
                var comingUser = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
                if (comingUser == null)
                    App.Current.Dispatcher.Invoke(() => Users.Add(new UserViewModel(user, true)));
                else
                    comingUser.Online = true;
            });
            _client.UserOut += ((user) =>
            {
                Log.Trace("Пользователь вышел");
                var leavingUser = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
                if (leavingUser != null)
                    leavingUser.Online = false;
            });
            _client.UserVideo += ((video, user) =>
            {
                var _user = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
                if (_user != null)
                {
                    video.ForEach(_ => _user.Video = _);
                }
            });
            _client.UserImage += ((image, user) =>
            {
                Users.Where(_ => _.Name == user.Name).FirstOrDefault().Image = image;
                Log.Trace("Пользователь {0} сменил изображение", user.Name);
            });
            _client.Error += ((error, reconnect, logout) => { _errorViewModel.Show(error, reconnect, logout); });

            _client.UserSound += ((sound, user) =>
            {
                vp.OnDataReceived(sound);
            });
        }

        public VoiceRecorder vr = new VoiceRecorder();
        public VoicePlayer vp = new VoicePlayer();

        public string FileName
        {
            get => !string.IsNullOrWhiteSpace(_sendFileViewModel.FilePath) ? "Прикреплен: " + SendFileViewModel.FileName : null;
        }

        // Сообщение в блоке ввода текста
        private string enteredMessage;
        public string EnteredMessage
        {
            get => enteredMessage;
            set
            {
                enteredMessage = value;
                OnPropertyChanged();
            }
        }

        // Команда отправить сообщение
        private RelayCommand sendMessage;
        public RelayCommand SendMessage
        {
            get => sendMessage;
            private set
            {
                sendMessage = value;
                OnPropertyChanged();
            }
        }

        public void SendMessageAction(object obj = null)
        {
            if (!string.IsNullOrWhiteSpace(_sendFileViewModel.FilePath))
            {
                _client.SendMessage(new FileMessageDto
                {
                    MessageText = !string.IsNullOrWhiteSpace(obj?.ToString()) ? obj.ToString() : "",
                    FileName = _sendFileViewModel.FileName,
                    FileWeight = _sendFileViewModel.FileWeight,
                    Hash = _sendFileViewModel.FileHash
                });
                _sendFileViewModel.FilePath = "";
                EnteredMessage = "";
            }
            else if (!string.IsNullOrWhiteSpace(obj?.ToString()))
            {
                Log.Trace("Начало отправки сообщения");
                _client.SendMessage(new MessageDto(obj.ToString()));
                EnteredMessage = "";
                _sendFileViewModel.FilePath = "";
                Log.Trace("Конец отправки сообщения");
            }
        }

        public void StopImageStreams()
        {
            if (_isGrabbing)
                makeVideo.Execute(null);
            _makePhotoViewModel.Stop();
        }

        // Команда запуска окна "Сделать фото"
        private RelayCommand makePhoto;
        public RelayCommand MakePhoto
        {
            get => makePhoto;
            private set
            {
                makePhoto = value;
                OnPropertyChanged();
            }
        }

        public void MakePhotoAction(object obj = null)
        {
            Log.Trace("Запуск команды Сделать фото");
            _makePhotoViewModel.Start();
        }

        private bool _isGrabbing;
        // Команда запуска/остановки видео
        private RelayCommand makeVideo;
        public RelayCommand MakeVideo
        {
            get => makeVideo;
            private set
            {
                makeVideo = value;
                OnPropertyChanged();
            }
        }

        public void MakeVideoAction(object obj = null)
        {
            vr.StartRecord();
            Log.Trace("Нажатие кнопки видео");
            if (!_isGrabbing)
            {
                Task.Factory.StartNew(() =>
                {
                    Log.Debug("Запуск потока видео");
                    _isGrabbing = true;
                    try
                    {
                        Capture capture = new Capture();
                        UserViewModel curUser = Users.Where(_ => _.Name == Name).FirstOrDefault();
                        while (_isGrabbing)
                        {
                            curUser.Video = capture.QueryFrame().ToImage<Bgr, Byte>().ToJpegData();
                            Task.Delay(40).Wait();
                            _client.SendVideo(curUser.Video);
                        }
                        Log.Debug("Остановка потока видео");
                        capture.Dispose();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                    UserViewModel user = Users.Where(_ => _.Name == Name).FirstOrDefault();
                    if (user != null) user.Video = null;
                    _client.SendVideo(null);
                    _makePhotoViewModel.Image = _client.GetAvatar(Name);

                });
            }
            else
            {
                _isGrabbing = false;
            }
        }

        // Флаг увеличения фото
        private bool _openImageFlag;
        public bool OpenImageFlag
        {
            get => _openImageFlag;
            set
            {
                _openImageFlag = value;
                OnPropertyChanged();
            }
        }

        // UserViewModel которому передаются данные при увеличении фото
        private UserViewModel _currentUser;
        public UserViewModel CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        // Команда увеличить фото

        private RelayCommand openImage;
        public RelayCommand OpenImage
        {
            get => openImage;
            private set
            {
                openImage = value;
                OnPropertyChanged();
            }
        }
        public void OpenImageAction(object obj = null)
        {
            Log.Trace("Увеличение фото");
            OpenImageFlag = !OpenImageFlag;
            CurrentUser = obj as UserViewModel;
        }

        // Переменные для подгрузки сообщений
        public DateTime? lastMessageDate;
        private bool _historyLoading = false;
        private bool _historyLoaded = false;
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

        // Подгрузка сообщений по приближению скрола к низу страницы
        private RelayCommand _endScrollCommand;
        public RelayCommand EndScrollCommand
        {
            get => _endScrollCommand;
            private set
            {
                _endScrollCommand = value;
                OnPropertyChanged();
            }
        }

        public void EndScrollAction(object obj = null)
        {
            Log.Trace("Запуск подгрузки сообщений");
            if (_historyLoading || _historyLoaded)
            {
                Log.Trace("История загружается или уже загружена");
                return;
            }
            Task.Factory.StartNew(() =>
            {
                Log.Debug("Начало загрузки сообщений");
                OnLoading = true;
                _historyLoading = true;
                List<MessageDto> messages = _client.LoadMessages(lastMessageDate);
                if (messages != null && messages.Count() == 0) _historyLoaded = true;
                try
                {
                    AddLoadedMessages(messages);
                }
                catch (Exception e)
                {
                    Log.Debug(e);
                }
                finally
                {
                    Log.Trace("Конец загрузки сообщений");

                    _historyLoading = false;
                    OnLoading = false;
                }

            });
        }


        private void AddLoadedMessages(List<MessageDto> messages)
        {
            messages.ForEach(message =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    MessageViewModel ms = new MessageViewModel(message, GetOwnerForMessage(message), _client.GetFile, _client.DownloadFile, _errorViewModel);
                    MessageRule = ms;
                    Messages.Add(ms);
                    lastMessageDate = message.Date;
                });
            });
        }

        private RelayCommand _sendFileMessage;
        public RelayCommand SendFileMessage
        {
            get => _sendFileMessage;
            private set
            {
                _sendFileMessage = value;
                OnPropertyChanged();
            }
        }
        private void SendFileMessageAction(object obj = null)
        {
            Body = SendFileViewModel;
        }

        private UserViewModel GetOwnerForMessage(MessageDto msg)
        {
            return Users.Where(_ => _.Name == msg.Owner.Name).FirstOrDefault();
        }


    }
}
