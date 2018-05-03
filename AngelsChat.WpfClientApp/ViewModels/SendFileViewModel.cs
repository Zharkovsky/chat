using AngelsChat.Client;
using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class SendFileViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ClientService _client;
        ChatRoomViewModel _chatViewModel;
        public SendFileViewModel(ClientService client, ChatRoomViewModel chatViewModel)
        {
            _client = client;
            _chatViewModel = chatViewModel;
            ProgressVisibility = "Collapsed";
            Progress = 0;
            Back = new RelayCommand(BackAction);
            Send = new RelayCommand(SendAction);
            Choose = new RelayCommand(ChooseAction);
        }

        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        private string _progressVisibility;
        public string ProgressVisibility
        {
            get => _progressVisibility;
            set
            {
                _progressVisibility = value;
                OnPropertyChanged();
            }
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                if (value != _progress)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FileName { get; set; }
        public long FileWeight { get; set; }
        public string FileHash { get; set; }

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
        private void BackAction(object obj)
        {
            FilePath = "";
            _chatViewModel.SendFileViewModelFlag = false;
            ProgressVisibility = "Collapsed";
            Log.Trace("Команда вернуться");
        }

        private RelayCommand send;
        public RelayCommand Send
        {
            get => send;
            private set
            {
                send = value;
                OnPropertyChanged();
            }
        }
        private void SendAction(object obj)
        {
            Progress = 0;
            ProgressVisibility = "Visible";

            long MaxStreamCapacity = _client.GetStreamCapacity();
            long recieved = 0, length = 0;
            byte[] buffer = File.ReadAllBytes(FilePath);
            byte[] partOfBuffer = new byte[MaxStreamCapacity];

            string[] splitpath = FilePath.Split('\\');
            FileName = splitpath[splitpath.Length - 1];
            FileWeight = buffer.LongLength;
            FileHash = GetMd5Hash(buffer);

            _client.PrepareFile(FileWeight);

            for (; recieved < FileWeight; recieved += length, Progress = GetPercent(FileWeight, recieved))
            {
                length = Math.Min(MaxStreamCapacity, FileWeight - recieved);
                Array.Copy(buffer, recieved, partOfBuffer, 0, length);
                _client.SendPartOfFile(new FilePartDto { Buffer = partOfBuffer, StartIndex = recieved, Length = length });
            }

            ProgressVisibility = "Collapsed";
            _chatViewModel.OnPropertyChanged(nameof(_chatViewModel.FileName));
            _chatViewModel.SendFileViewModelFlag = false;
        }

        private RelayCommand choose;
        public RelayCommand Choose
        {
            get => choose;
            private set
            {
                choose = value;
                OnPropertyChanged();
            }
        }

        private void ChooseAction(object obj)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";
            bool? result = dlg.ShowDialog();
            if (result == true)
                FilePath = dlg.FileName;
        }

        private string GetMd5Hash(byte[] array)
        {
            //определение хэша
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(array);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            return sBuilder.ToString();
        }

        public Int32 GetPercent(long b, long a)
        {
            if (b == 0) return 0;

            return (Int32)(a / (b / 100M));
        }
    }
}
