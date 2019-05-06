using AngelsChat.Client;
using AngelsChat.WpfClientApp.Helpers;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private ClientService _client;
        private ChatRoomsViewModel _chatRoomsViewModel;

        public string Name
        {
            get => _chatRoomsViewModel.LoginVM.UserName;
            set
            {
                _chatRoomsViewModel.LoginVM.UserName = value;
                OnPropertyChanged();
            }
        }


        public ProfileViewModel(ClientService client, ChatRoomsViewModel chatRoomsViewModel)
        {
            _client = client;
            _chatRoomsViewModel = chatRoomsViewModel;
            _makePhotoViewModel = new PhotoViewModel(_client);

            OnPropertyChanged(nameof(Name));

            _backFromSettings = new RelayCommand(BackFromSettingsAction);
            _saveSettings = new RelayCommand(SaveSettingsAction);
            _makePhoto = new RelayCommand(MakePhotoAction);
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
            _chatRoomsViewModel.EditProfile = false;

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

        private RelayCommand _makePhoto;
        public RelayCommand MakePhoto
        {
            get => _makePhoto;
            private set
            {
                _makePhoto = value;
                OnPropertyChanged();
            }
        }

        private void MakePhotoAction(object obj)
        {
            _makePhotoViewModel.Start();
        }

        private PhotoViewModel _makePhotoViewModel;
        public PhotoViewModel MakePhotoViewModel
        {
            get => _makePhotoViewModel;
            set
            {
                _makePhotoViewModel = value;
                OnPropertyChanged();
            }
        }

        private void SaveSettingsAction(object parameter)
        {
            string password = null, name = null;
            if (parameter is IHavePassword passwordContainer)
                password = LoginViewModel.ConvertToUnsecureString(passwordContainer.Password);
            if (parameter is IHaveName nameContatainer)
                name = nameContatainer.UserName;
            Name = name;
            var result = _client.UpdateProfile(name, password);

            _chatRoomsViewModel.EditProfile = false;
            //_client.UpdateRoom(_chatRoomsViewModel.Room);
        }

    }
}