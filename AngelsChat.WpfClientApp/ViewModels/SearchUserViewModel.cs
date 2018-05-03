using System;
using System.Collections.ObjectModel;
using AngelsChat.Client;
using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class SearchUserViewModel : ViewModelBase
    {
        private ClientService _client;
        private ChatRoomViewModel _chatRoom;
        private ObservableCollection<UserDto> _users;

        public ObservableCollection<UserDto> Users
        {
            get => _users;
            set
            {
                if (_users == value) return;
                _users = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SearchUsersCommand { get; private set; }
        public RelayCommand InviteUserCommand { get; private set; }
        public RelayCommand DeleteUserCommand { get; private set; }

        public SearchUserViewModel(ClientService client, ChatRoomViewModel chatRoom)
        {
            _client = client;
            _chatRoom = chatRoom;
            Users = new ObservableCollection<UserDto>();

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _client.GetAllUsers().ForEach(_ =>
                {
                    Users.Add(_);
                });
            });
        }

        private void InitializeCommands()
        {
            SearchUsersCommand = new RelayCommand(SearchUsersAction);
            InviteUserCommand = new RelayCommand(InviteUserAction);
            DeleteUserCommand = new RelayCommand(DeleteUserAction);
        }

        private void DeleteUserAction(object obj)
        {
            var user = obj as UserDto;
            if (user == null) return;
            _client.RemoveUser(_chatRoom.Room, user);
        }

        private void InviteUserAction(object obj)
        {
            var user = obj as UserDto;
            if (user == null) return;
            _client.InviteUser(_chatRoom.Room, user);
        }

        private void SearchUsersAction(object obj)
        {
            throw new NotImplementedException();
        }
    }
}
