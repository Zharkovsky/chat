using AngelsChat.Client;
using AngelsChat.Shared;
using Emgu.CV;
using System;
using Emgu.CV.Structure;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;
using AngelsChat.WpfClientApp.Helpers;
using AngelsChat.Shared.Data;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class PhotoViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private bool _sendFlag;
        public bool SendFlag
        {
            get => _sendFlag;
            set
            {
                _sendFlag = value;
                OnPropertyChanged();
            }
        }

        private ClientService _client;

        public PhotoViewModel(ClientService client)
        {
            _client = client;
            SendFlag = false;
            Back = new RelayCommand(BackAction);
            MakePhoto = new RelayCommand(MakePhotoAction);
            SendPhoto = new RelayCommand(SendPhotoAction);
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

        private bool _isGrabbing;
        public void Start()
        {

            OpenImageFlag = !OpenImageFlag;
            Log.Trace("Запуск PhotoViewModel");
            if (!_isGrabbing)
            {
                Task.Factory.StartNew(() =>
                {
                    Log.Debug("Запуск съемки");
                    _isGrabbing = true;
                    try
                    {
                        Capture capture = new Capture();
                        while (_isGrabbing)
                        {
                            var img = capture.QueryFrame().ToImage<Bgr, Byte>();
                            Image = new ImageDto(img.Bytes, img.Width, img.Height);
                            Task.Delay(40).Wait();
                        }
                        Log.Debug("Остановка съемки");
                        capture.Dispose();
                    }
                    catch(Exception e)
                    {
                        Log.Error(e);
                    }
                });
            }
            else
            {
                _isGrabbing = false;
            }
        }
        public void Stop() => _isGrabbing = false;

        private RelayCommand back;
        public RelayCommand Back
        {
            get => back;
            private set
            {
                back = value;
                OnPropertyChanged();
            }
        }

        public void BackAction(object obj)
        {
            _isGrabbing = false;
            SendFlag = false;
            OpenImageFlag = false;
            Image = _client.GetAvatar(_client.User.Name);
            Log.Trace("Команда вернуться");
        }

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
        private void MakePhotoAction(object obj)
        {
            _isGrabbing = false;
            SendFlag = true;
            Log.Trace("Команда сделать фото");
        }

        private RelayCommand sendPhoto;
        public RelayCommand SendPhoto
        {
            get => sendPhoto;
            private set
            {
                sendPhoto = value;
                OnPropertyChanged();
            }
        }
        private void SendPhotoAction(object obj)
        {
            Log.Trace("Команда отправить фото");
            _client.SetImage(Image);
            SendFlag = false;
        }
    }
}
