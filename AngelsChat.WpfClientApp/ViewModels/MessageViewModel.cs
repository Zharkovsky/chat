using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class MessageViewModel : ViewModelBase
    {
        private readonly MessageDto _model;
        private readonly UserViewModel _owner;

        public UserViewModel Owner { get => _owner; }
        public System.DateTime Date { get => _model.Date; }

        public ViewModelBase Body { get; }

        public delegate long GetFileDelegate(FileMessageDto message);
        public delegate byte[] DownloadFileDelegate(long recieved);

        public MessageViewModel(MessageDto model, UserViewModel owner, GetFileDelegate GetFile, DownloadFileDelegate DownloadFile, ErrorViewModel errorViewModel) {
            _model = model;
            _owner = owner;
            if (model is FileMessageDto filemsg)
            {
                Body = new BinaryBodyViewModel(filemsg, GetFile, DownloadFile, errorViewModel);
            }
            else if (model is MessageDto msg)
            {
                Body = new TextBodyViewModel(_model);
            }
        }
    }

}