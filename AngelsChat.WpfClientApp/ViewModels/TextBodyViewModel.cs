using AngelsChat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.WpfClientApp.ViewModels
{
    public class TextBodyViewModel : ViewModelBase
    {
        private readonly MessageDto _model;
        public string MessageText { get => _model.MessageText; }
        public TextBodyViewModel(MessageDto model)
        {
            _model = model;
        }
    }
}
