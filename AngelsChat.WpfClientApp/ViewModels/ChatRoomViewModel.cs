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
using System.Windows.Controls;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class ChatRoomViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private ClientService _client;

        public ObservableCollection<UserViewModel> Users { get; private set; }
        public ObservableCollection<MessageViewModel> Messages { get; private set; }

        private RoomDto _room;
        public RoomDto Room
        {
            get => _room;
            set
            {
                _room = value;
                OnPropertyChanged();
            }
        }

        private bool _isInviting;
        public bool IsInviting
        {
            get => _isInviting;
            set
            {
                _isInviting = value;
                OnPropertyChanged();
            }
        }

        private bool _isEditingName;
        public bool IsEditingName
        {
            get => _isEditingName;
            set
            {
                _isEditingName = value;
                OnPropertyChanged();
            }
        }

        private string _name;
        public string Name
        {
            get => Room.Name;
            set
            {
                Room.Name = value;
                OnPropertyChanged();
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

        private bool _sendFileViewModelFlag;
        public bool SendFileViewModelFlag
        {
            get => _sendFileViewModelFlag;
            set
            {
                _sendFileViewModelFlag = value;
                OnPropertyChanged();
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

        private ChatRoomViewModel _chatViewModel;
        public ChatRoomViewModel MainChatViewModel
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
                OnPropertyChanged();
            }
        }

        private ViewModelBase _parent;

        ShowMessage ShowMessageInTray;
        public ChatRoomViewModel(RoomDto room, ClientService client, ShowMessage MainWindowShowMessage, ViewModelBase Parent)
        {
            _room = room;
            ShowMessageInTray = MainWindowShowMessage;
            _client = client;
            _parent = Parent;

            Messages = new ObservableCollection<MessageViewModel>();
            Users = new ObservableCollection<UserViewModel>();

            _sendFileViewModel = new SendFileViewModel(_client, this);

            MainChatViewModel = this;
            Body = this;

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
            vp.Init();
            vp.Start();
        }

        private void InitializeInfo()
        {
            Log.Info("Инициализация данных");
            // Get all Users
            _client.GetUsers(_room)?
                .Select(u => new UserViewModel(u)).ToList()
                .ForEach(_ => App.Current.Dispatcher.Invoke(() => Users.Add(_)));
            Log.Debug("Загрузка всех пользователей");


            // Get online Users
            Users.ToList().ForEach(_ => _.Online = _client.IsOnline(_room, _.Name));
            Log.Debug("Загрузка пользователей в онлайн");

            // Get Avatars
            Users.ToList().ForEach(_ => _.Image = _client.GetAvatar(_.Name));
            Log.Debug("Загрузка аватарок всех пользователей");

            // Set current user image
            Log.Trace("Установка фото текущего пользователя");

            //Load messages
            AddLoadedMessages(_client.LoadMessages(_room, lastMessageDate, 10));
        }

        public RelayCommand RenameCommand { get; private set; }
        public RelayCommand RenameRoomCommand { get; private set; }
        public RelayCommand InviteCommand { get; private set; }
        public RelayCommand MuteAllCommand { get; private set; }
        public RelayCommand LeaveRoomCommand { get; private set; }
        public RelayCommand TurnOffMicrophoneCommand { get; private set; }


        private void InitializeCommands()
        {
            EndScrollCommand = new RelayCommand(EndScrollAction);
            OpenImage = new RelayCommand(OpenImageAction);
            MakeVideo = new RelayCommand(MakeVideoAction);
            SendMessage = new RelayCommand(SendMessageAction);
            DeleteMessage = new RelayCommand(DeleteMessageAction);
            SendFileMessage = new RelayCommand(SendFileMessageAction);
            DeleteFile = new RelayCommand(DeleteFileAction);
            RenameCommand = new RelayCommand(RenameAction);
            RenameRoomCommand = new RelayCommand(RenameRoomAction);
            InviteCommand = new RelayCommand(InviteAction);
            MuteAllCommand = new RelayCommand(MuteAllAction);
            LeaveRoomCommand = new RelayCommand(LeaveRoomAction);
            TurnOffMicrophoneCommand = new RelayCommand(TurnOffMicrophoneAction);
        }

        private void TurnOffMicrophoneAction(object obj)
        {
            if (_parent is ChatRoomsViewModel chatRooms)
            {
                chatRooms.StopRecording(_room);
            }
        }

        private void LeaveRoomAction(object obj)
        {
            if(_parent is ChatRoomsViewModel chatRooms)
            {
                chatRooms.Rooms.Remove(this);
                _client.KickUser(Room, _client.User);
            }
        }

        private void MuteAllAction(object obj)
        {
            Users.ToList().ForEach(_ => _.Muted = true);
        }

        public void InviteAction(object obj)
        {
            _isInviting = !_isInviting;
            var chatRooms = (ChatRoomsViewModel)_parent;
            if (chatRooms == null) return;
            if (_isInviting)
            {
                chatRooms.SearchUserViewModel = new SearchUserViewModel(_client, this);
            }
            else
            {
                chatRooms.SearchUserViewModel = null;
            }
        }

        private void RenameAction(object obj)
        {
            IsEditingName = !IsEditingName;
        }

        public void RenameRoomAction(object parameter)
        {
            if(IsEditingName)
            {
                if (parameter is TextBox textBox)
                {
                    Room.Name = textBox.Text;
                    _client.UpdateRoom(Room);
                    RenameAction(null);
                }
            }
        }

        public void MessageRecived(MessageDto message)
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
        }

        public void UserInvited(UserDto user)
        {
            var comingUser = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
            if (comingUser == null)
                App.Current.Dispatcher.Invoke(() => Users.Add(new UserViewModel(user, true)));
            Log.Trace("Пользователь приглашен");
        }

        public void UserRemoved(UserDto user)
        {
            var leavingUser = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
                App.Current.Dispatcher.Invoke(() => Users.Remove(leavingUser));
            Log.Trace("Пользователь удален");
        }


        public void UserEntered(UserDto user)
        {
            Log.Trace("Пользователь вошел");
            var comingUser = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
            if (comingUser == null)
                App.Current.Dispatcher.Invoke(() => Users.Add(new UserViewModel(user, true)));
            else
                comingUser.Online = true;
        }

        public void UserOut(UserDto user)
        {
            Log.Trace("Пользователь вышел");
            var leavingUser = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
            if (leavingUser != null)
                leavingUser.Online = false;
        }

        public void UserVideo(List<byte[]> video, UserDto user)
        {
            if (user == null || video == null) return;
            var _user = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
            if (_user != null && !_user.CallPaused)
            {
                video.ForEach(_ => _user.Video = _);
            }
        }

        public void UserImage(ImageDto image, UserDto user)
        {
            Users.Where(_ => _.Name == user.Name).FirstOrDefault().Image = image;
            Log.Trace("Пользователь {0} сменил изображение", user.Name);
        }

        public void UserSound(List<byte[]> sound, UserDto user)
        {
            if (user == null || sound == null) return;
            var _user = Users.Where(_ => _.Name == user.Name).FirstOrDefault();
            if (_user != null && !_user.Muted && !_user.CallPaused)
            {
                vp.OnDataReceived(sound);
            }
        }

        private void InitializeEvents()
        {

        }

        public VoicePlayer vp = new VoicePlayer();

        public string FileName
        {
            get => !string.IsNullOrWhiteSpace(_sendFileViewModel.FilePath) ? "Прикреплен: " + SendFileViewModel.FileName : null;
            set
            {
                OnPropertyChanged();
            }
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
                    Hash = _sendFileViewModel.FileHash,
                    Room = Room
                });
                DeleteFileAction();
                EnteredMessage = "";
            }
            else if (!string.IsNullOrWhiteSpace(obj?.ToString()))
            {
                Log.Trace("Начало отправки сообщения");
                _client.SendMessage(new MessageDto(obj.ToString()) { Room = Room });
                EnteredMessage = "";
                _sendFileViewModel.FilePath = "";
                Log.Trace("Конец отправки сообщения");
            }
        }

        private RelayCommand deleteMessage;
        public RelayCommand DeleteMessage
        {
            get => deleteMessage;
            private set
            {
                deleteMessage = value;
                OnPropertyChanged();
            }
        }
        public void DeleteMessageAction(object obj = null)
        {
            EnteredMessage = "";
            _sendFileViewModel.FilePath = "";
        }

        public void StopImageStreams()
        {
            if (_isGrabbing)
                makeVideo.Execute(null);
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
            if(_parent is ChatRoomsViewModel chatRooms)
            {
                chatRooms.MakeVideo(_room);
            }
            //vr.StartRecord();
            //Log.Trace("Нажатие кнопки видео");
            //if (!_isGrabbing)
            //{
            //    Task.Factory.StartNew(() =>
            //    {
            //        Log.Debug("Запуск потока видео");
            //        _isGrabbing = true;
            //        try
            //        {
            //            Capture capture = new Capture();
            //            UserViewModel curUser = Users.Where(_ => _.Name == _client.User.Name).FirstOrDefault();
            //            while (_isGrabbing)
            //            {
            //                curUser.Video = capture.QueryFrame().ToImage<Bgr, Byte>().ToJpegData();
            //                Task.Delay(40).Wait();
            //                _client.SendVideo(_room, curUser.Video);
            //            }
            //            Log.Debug("Остановка потока видео");
            //            capture.Dispose();
            //        }
            //        catch (Exception e)
            //        {
            //            Log.Error(e);
            //        }
            //        UserViewModel user = Users.Where(_ => _.Name == Name).FirstOrDefault();
            //        if (user != null) user.Video = null;
            //        _client.SendVideo(_room, null);

            //    });
            //}
            //else
            //{
            //    _isGrabbing = false;
            //}
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
                List<MessageDto> messages = _client.LoadMessages(_room, lastMessageDate);
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
            SendFileViewModelFlag = true;
        }

        private RelayCommand _deleteFile;
        public RelayCommand DeleteFile
        {
            get => _deleteFile;
            private set
            {
                _deleteFile = value;
                OnPropertyChanged();
            }
        }
        private void DeleteFileAction(object obj = null)
        {
            _client.DeleteFile();
            SendFileViewModel.FilePath = null;
            OnPropertyChanged(nameof(FileName));
        }

        private UserViewModel GetOwnerForMessage(MessageDto msg)
        {
            return Users.Where(_ => _.Name == msg.Owner.Name).FirstOrDefault();
        }


    }
}
