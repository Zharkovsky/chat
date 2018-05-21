using AngelsChat.Client;
using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;
using System;
using System.Collections.ObjectModel;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class RoomSettingsViewModel : ViewModelBase
    {
        private ClientService _client;
        private ChatRoomViewModel chatRoomViewModel;
        private ChatRoomsViewModel chatRoomsViewModel;
        private ObservableCollection<UserDto> _users;

        public string Name
        {
            get => chatRoomViewModel.Room.Name;
            set
            {
                chatRoomViewModel.Room.Name = value;
                OnPropertyChanged();
            }
        }

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

        public RoomSettingsViewModel(ClientService client, ChatRoomViewModel chatRoomViewModel, ChatRoomsViewModel chatRoomsViewModel)
        {
            _client = client;
            this.chatRoomViewModel = chatRoomViewModel;
            this.chatRoomsViewModel = chatRoomsViewModel;

            OnPropertyChanged(nameof(Name));

            _backFromSettings = new RelayCommand(BackFromSettingsAction);
            _saveSettings = new RelayCommand(SaveSettingsAction);
            _inviteCommand = new RelayCommand(InviteCommandAction);

            Users = new ObservableCollection<UserDto>();
            
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _client.GetAllUsers().ForEach(_ =>
                {
                    Users.Add(_);
                });
            });
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
            chatRoomsViewModel.EditProfile = false;

        }

        private RelayCommand _saveSettings;
        public RelayCommand SaveSettings
        {
            get => _saveSettings;
            private set
            {
                _saveSettings = value;
                OnPropertyChanged();
            }
        }
        private void SaveSettingsAction(object obj)
        {
            chatRoomsViewModel.EditProfile = false;
            _client.UpdateRoom(chatRoomViewModel.Room);
        }

        private RelayCommand _inviteCommand;
        public RelayCommand InviteCommand
        {
            get => _inviteCommand;
            private set
            {
                _inviteCommand = value;
                OnPropertyChanged();
            }
        }
        private void InviteCommandAction(object obj)
        {
            var user = obj as UserDto;
            if (user == null) return;
            _client.InviteUser(chatRoomViewModel.Room, user);
        }
    }
}