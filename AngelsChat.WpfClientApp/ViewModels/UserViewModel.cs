using AngelsChat.Client;
using AngelsChat.Shared;
using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;
using System;
using System.Linq;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class UserViewModel: ViewModelBase
    {
        private string _name;
        public string Name { get => _name; set => _name = value; }

        private bool _muted;
        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                OnPropertyChanged();
            }
        }

        private bool _callPaused;
        public bool CallPaused
        {
            get => _callPaused;
            set
            {
                _callPaused = value;
                OnPropertyChanged();
            }
        }

        private ImageDto _image;
        public ImageDto Image
        {
            get => _image;
            set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        private byte[] _video;
        public byte[] Video
        {

            get => _video;
            set
            {
                _video = value;
                OnPropertyChanged();
            }
        }

        private bool _online;
        public bool Online
        {

            get => _online;
            set
            {
                _online = value;
                OnPropertyChanged();
            }
        }

        

        public UserViewModel()
        {
            
        }
        public UserViewModel(UserDto user) : base()
        {
            _name = user.Name;
            _online = false;
            MuteCommand = new RelayCommand(MuteAction);
            PauseCallCommand = new RelayCommand(PauseCallAction);
        }
        public UserViewModel(UserDto user, ImageDto image) : base()
        {
            _name = user.Name;
            _image = image;
            MuteCommand = new RelayCommand(MuteAction);
            PauseCallCommand = new RelayCommand(PauseCallAction);
        }
        public UserViewModel(string name) : base()
        {
            _name = name;
            MuteCommand = new RelayCommand(MuteAction);
            PauseCallCommand = new RelayCommand(PauseCallAction);
        }
        public UserViewModel(UserDto user, ImageDto image, bool online) : base()
        {
            _name = user.Name;
            _image = image;
            _online = online;
            MuteCommand = new RelayCommand(MuteAction);
            PauseCallCommand = new RelayCommand(PauseCallAction);
        }
        public UserViewModel(UserDto user, bool online) : base()
        {
            _name = user.Name;
            _online = online;
            MuteCommand = new RelayCommand(MuteAction);
            PauseCallCommand = new RelayCommand(PauseCallAction);
        }



        public RelayCommand InviteUserCommand { get; private set; }
        public RelayCommand DeleteUserCommand { get; private set; }
        public RelayCommand MuteCommand { get; private set; }
        public RelayCommand PauseCallCommand { get; private set; }

        private ClientService _client;
        private UserDto _user;
        private ChatRoomViewModel _room;

        public UserViewModel(ClientService client, UserDto user, ChatRoomViewModel room) : base()
        {
            _client = client;
            _user = user;
            _room = room;
            Name = user.Name;
            InviteUserCommand = new RelayCommand(InviteUserAction);
            DeleteUserCommand = new RelayCommand(DeleteUserAction);
            MuteCommand = new RelayCommand(MuteAction);
            PauseCallCommand = new RelayCommand(PauseCallAction);
            OnPropertyChanged(nameof(IsInvited));
        }

        private void PauseCallAction(object obj)
        {
            CallPaused = !CallPaused;
        }

        private void MuteAction(object obj)
        {
            Muted = !Muted;
        }

        public bool IsInvited => _room.Users.Select(_ => _.Name).Contains(_user.Name);

        private void DeleteUserAction(object obj)
        {
            _client.KickUser(_room.Room, _user);
        }

        private void InviteUserAction(object obj)
        {
            _client.InviteUser(_room.Room, _user);
        }
    }
}
