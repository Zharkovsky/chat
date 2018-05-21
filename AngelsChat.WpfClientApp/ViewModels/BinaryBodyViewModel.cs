using AngelsChat.Shared.Data;
using AngelsChat.WpfClientApp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class BinaryBodyViewModel : ViewModelBase
    {
        private readonly FileMessageDto _model;
        public string FileName { get => _model.FileName; }
        public string FileNameForButton { get => "Загрузить файл: " + _model.FileName; }
        public string MessageText { get => _model.MessageText; }
        public string Hash { get => _model.Hash; }
        public BinaryBodyViewModel(FileMessageDto model, MessageViewModel.GetFileDelegate _getFile, MessageViewModel.DownloadFileDelegate _downloadFile, ErrorViewModel errorViewModel)
        {
            _model = model;
            GetFile = _getFile;
            DownloadFile = _downloadFile;
            _errorViewModel = errorViewModel;
            Save = new RelayCommand(SaveAction);
            ProgressVisibility = "Collapsed";
            Progress = 0;
        }
        
        MessageViewModel.GetFileDelegate GetFile;
        MessageViewModel.DownloadFileDelegate DownloadFile;
        ErrorViewModel _errorViewModel;

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

        private RelayCommand save;
        public RelayCommand Save
        {
            get => save;
            private set
            {
                save = value;
                OnPropertyChanged();
            }
        }
        public void SaveAction(object obj = null)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = FileName;
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";
            bool? result = dlg.ShowDialog();
            string FilePath;
            if (result == true)
            {
                Progress = 0;
                ProgressVisibility = "Visible";

                FilePath = dlg.FileName;
                long weight = GetFile(_model);
                byte[] buffer = new byte[weight];
                long recieved = 0;
                while (recieved < weight)
                {
                    Progress = GetPercent(weight, recieved);
                    byte[] partOfFile = DownloadFile(recieved);
                    Array.Copy(partOfFile, 0, buffer, recieved, partOfFile.Length);
                    recieved += partOfFile.LongLength;
                }
                using (FileStream fstream = new FileStream(FilePath, FileMode.OpenOrCreate))
                {
                    fstream.Write(buffer, 0, buffer.Length);
                }

                if (_model.Hash != GetMd5Hash(buffer))
                {
                    _errorViewModel.Show("Не удалось загрузить файл");
                }

                ProgressVisibility = "Collapsed";
            }
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
