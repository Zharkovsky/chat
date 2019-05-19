using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using AngelsChat.Client;
using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class SearchUserViewModel : ViewModelBase
    {
        private IClientService _client;
        private ChatRoomViewModel _chatRoom;
        private ObservableCollection<UserViewModel> _users;

        public ObservableCollection<UserViewModel> Users
        {
            get => _users;
            set
            {
                if (_users == value) return;
                _users = value;
                OnPropertyChanged();
            }
        }

        public string Name => "Invite to " + _chatRoom.Name;

        public RelayCommand SearchUsersCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }

        public SearchUserViewModel() { }

        public SearchUserViewModel(IClientService client, ChatRoomViewModel chatRoom)
        {
            _client = client;
            _chatRoom = chatRoom;
            Users = new ObservableCollection<UserViewModel>();

            InitializeCommands();

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _client.GetAllUsers().ForEach(_ =>
                {
                    Users.Add(new UserViewModel(_client, _, _chatRoom));
                });
            });
        }

        private void InitializeCommands()
        {
            SearchUsersCommand = new RelayCommand(SearchUsersAction);
            CloseCommand = new RelayCommand(CloseAction);
        }

        private void CloseAction(object obj)
        {
            _chatRoom.InviteAction(obj);
        }

        

        private void SearchUsersAction(object parameter)
        {
            if (parameter is TextBox textBox)
            {
                var text = textBox.Text;
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    Users.Clear();
                    _client.GetAllUsers().Where(_ => _.Name.StartsWith(text)).ToList().ForEach(_ =>
                    {
                        Users.Add(new UserViewModel(_client, _, _chatRoom));
                    });
                    OnPropertyChanged(nameof(Users));
                });
            }
        }
    }
}
