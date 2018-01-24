using AngelsChat.Shared;
using AngelsChat.Shared.Data;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class UserViewModel: ViewModelBase
    {
        private string _name;
        public string Name { get => _name; set => _name = value; }

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

        public UserViewModel() { }
        public UserViewModel(UserDto user)
        {
            _name = user.Name;
            _online = false;
        }
        public UserViewModel(UserDto user, ImageDto image)
        {
            _name = user.Name;
            _image = image;
        }
        public UserViewModel(string name)
        {
            _name = name;
        }
        public UserViewModel(UserDto user, ImageDto image, bool online)
        {
            _name = user.Name;
            _image = image;
            _online = online;
        }
        public UserViewModel(UserDto user, bool online)
        {
            _name = user.Name;
            _online = online;
        }
    }
}
