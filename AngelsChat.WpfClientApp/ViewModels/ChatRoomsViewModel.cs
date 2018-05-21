using AngelsChat.Client;
using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;
using Emgu.CV;
using Emgu.CV.Structure;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using static AngelsChat.WpfClientApp.ViewModels.ChatRoomViewModel;

namespace AngelsChat.WpfClientApp.ViewModels
{ 
    public class ChatRoomsViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ClientService _client;
        private ObservableCollection<ChatRoomViewModel> _rooms;

        Helpers.ShowMessage ShowMessageInTray;

        #region public
        public ObservableCollection<ChatRoomViewModel> Rooms
        {
            get => _rooms;
            set
            {
                if (_rooms == value) return;
                _rooms = value;
                OnPropertyChanged();
            }
        }

        private SearchUserViewModel _searchUserView;
        public SearchUserViewModel SearchUserViewModel
        {
            get => _searchUserView;
            set
            {
                _searchUserView = value;
                OnPropertyChanged();
            }
        }

        private ChatRoomViewModel _selectedRoom;
        public ChatRoomViewModel SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                OnPropertyChanged();
            }
        }

        private bool _editProfile;
        public bool EditProfile
        {
            get => _editProfile;
            set
            {
                _editProfile = value;
                var room = (ChatRoomViewModel)_selectedRoom;
                if (room == null) return;
                EditUserProfileViewModel = new ProfileViewModel(_client, this);
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

        //private RoomSettingsViewModel _roomSettingsViewModel;
        //public RoomSettingsViewModel RoomSettingsViewModel
        //{
        //    get => _roomSettingsViewModel;
        //    set
        //    {
        //        _roomSettingsViewModel = value;
        //        OnPropertyChanged();
        //    }
        //}

        private ProfileViewModel _editUserProfileViewModel;
        public ProfileViewModel EditUserProfileViewModel
        {
            get => _editUserProfileViewModel;
            set
            {
                _editUserProfileViewModel = value;
                OnPropertyChanged();
            }
        }

        private ChatRoomsViewModel _chatViewModel;
        public ChatRoomsViewModel MainChatViewModel
        {
            get => _chatViewModel;
            set
            {
                _chatViewModel = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public ChatRoomsViewModel(ClientService client, ShowMessage MainWindowShowMessage)
        {
            InitCommands();

            ShowMessageInTray = MainWindowShowMessage;
            _client = client;

            MainChatViewModel = this;
            Rooms = new ObservableCollection<ChatRoomViewModel>();

            _signUpViewModel = new SignUpViewModel(_client, this);
            _loginViewModel = new LoginViewModel(_client, _signUpViewModel, this);
            _settingsViewModel = new SettingsViewModel(this);
            _errorViewModel = new ErrorViewModel(_client, _loginViewModel, _signUpViewModel, this);

            Body = LoginVM;
        }

        #region commands
        public RelayCommand RemoveCommand { get; private set; }
        public RelayCommand AddCommand { get; private set; }
        public RelayCommand ToSettingsCommand { get; private set; }

        private void InitCommands()
        {
            ToSettingsCommand = new RelayCommand(ToSettingsAction);
            AddCommand = new RelayCommand(AddAction);
            RemoveCommand = new RelayCommand(RemoveAction);
        }

        

        private void ToSettingsAction(object obj)
        {
            EditProfile = !EditProfile;
        }

        private void AddAction(object obj)
        {
            var room = _client.CreateRoom(new RoomDto() { Name = "New Room" });
            var newChatRoomViewModel = new ChatRoomViewModel(room, _client, ShowMessageInTray, this);
            newChatRoomViewModel.StartInit();
            Rooms.Add(newChatRoomViewModel);
            SelectedRoom = newChatRoomViewModel;
        }

        private void RemoveAction(object obj)
        {
            if (SelectedRoom == null) return;
            var room = (ChatRoomViewModel)SelectedRoom;
            if (room == null) return;
            _client.RemoveRoom(room.Room);
            SelectedRoom = Rooms.FirstOrDefault();
        }
        #endregion

        public void StartInit()
        {
            InitializeInfo();
            InitializeEvents();
        }

        private void InitializeInfo()
        {
            Log.Info("Инициализация данных");
            var rooms = _client.GetRooms().ToList().Select(_ => new ChatRoomViewModel(_, _client, ShowMessageInTray, this)).ToList();
            if (rooms == null) return;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                rooms.ForEach(_ =>
                {
                    _.StartInit();
                    if(Rooms.Where(r => r.Room.Id == _.Room.Id).Count() == 0)
                        Rooms.Add(_);
                });
            });
        }
        
        private void InitializeEvents()
        {
            _client.MessageRecived += ((room, message) => GetRoom(room)?.MessageRecived(message));
            _client.UserEntered += ((room, user) => GetRoom(room)?.UserEntered(user));
            _client.UserOut += ((room, user) => GetRoom(room)?.UserOut(user));
            _client.UserVideo += ((room, video, user) => GetRoom(room)?.UserVideo(video, user));
            _client.UserImage += ((image, user) => Rooms.ToList().ForEach(_ => _.UserImage(image, user)));
            _client.Error += ((error, reconnect, logout) => { _errorViewModel.Show(error, reconnect, logout); });
            _client.UserProfileUpdateEvent += ((user, name) => 
            {
                var userInRooms = Rooms.Select(_ => _.Users.Where(u => u.Name == user.Name).FirstOrDefault()).ToList();
                userInRooms.ForEach(_ => {
                    if(_ != null && _.Name != null)
                    {
                        _.Name = name;
                    }
                });
            });
            _client.UserInvited += ((room, user) =>
            {
                var r = GetRoom(room);
                if (r != null)
                    r.UserInvited(user);
                else
                {
                    Task.Factory.StartNew(() => 
                    {
                        r = new ChatRoomViewModel(room, _client, ShowMessageInTray, this);
                        r.StartInit();
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            Rooms.Add(r);
                        });
                    });
                }
            });
            _client.UserRemoved += ((room, user) =>
            {
                Task.Factory.StartNew(() =>
                {
                    if (user.Name == _loginViewModel.UserName)
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            Rooms.Remove(GetRoom(room));
                        });
                    else
                        GetRoom(room)?.UserRemoved(user);
                });
            });
            _client.RoomRemovedEvent += ((room) =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    Rooms.Remove(GetRoom(room));
                });
            });
            _client.RoomUpdatedEvent += ((room) =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    Rooms.Where(_ => _.Room.Id == room.Id).FirstOrDefault().Name = room.Name;
                });
            });
            _client.UserSound += ((room, sound, user) => GetRoom(room).UserSound(sound, user));
        }

        private ChatRoomViewModel GetRoom(RoomDto room) => Rooms.Where(_ => _.Room.Id == room.Id).FirstOrDefault();

        public VoiceRecorder vr = new VoiceRecorder();
        private bool _isGrabbing;
        private List<RoomDto> roomsForVideo;

        public void MakeVideo(RoomDto room)
        {
            if (roomsForVideo == null)
            {
                vr.Init(_client.SendVoice);
                roomsForVideo = new List<RoomDto>();
            }
            if (roomsForVideo.Contains(room))
            {
                roomsForVideo.Remove(room);

                Rooms.Where(r => r.Room.Id == room.Id).FirstOrDefault().Users.Where(u => u.Name == _client.User.Name).FirstOrDefault().Video = null;
                _client.SendVideo(room, null);

                _client.RemoveRoomFromVideo(room);

                if (roomsForVideo.Count() == 0)
                {
                    vr.StartRecord();
                    _isGrabbing = false;
                }
            }
            else if (!_isGrabbing)
            {
                roomsForVideo.Add(room);
                _client.AddRoomForVideo(room);
                Task.Factory.StartNew(() =>
                {
                    Log.Debug("Запуск потока видео");
                    _isGrabbing = true;
                    try
                    {
                        vr.StartRecord();
                        Capture capture = new Capture();
                        while (_isGrabbing)
                        {
                            var frame = capture.QueryFrame().ToImage<Bgr, Byte>().ToJpegData();
                            Task.Delay(40).Wait();
                            SendVideo(frame);
                        }
                        Log.Debug("Остановка потока видео");
                        capture.Dispose();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
            }
            else
            {
                roomsForVideo.Add(room);
                _client.AddRoomForVideo(room);
            }
        }
        public void StopRecording(RoomDto room) { }

        private void SendVideo(byte[] frame)
        {
            roomsForVideo.ForEach(_ => 
            {
                Rooms.Where(r => r.Room.Id == _.Id).FirstOrDefault().Users.Where(u => u.Name == _client.User.Name).FirstOrDefault().Video = frame;
                _client.SendVideo(_, frame);
            });
        }

        public void StopVideo(RoomDto room)
        {
            
        }
    }
}











