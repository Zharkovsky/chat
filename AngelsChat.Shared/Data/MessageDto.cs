using System.ComponentModel;

namespace AngelsChat.Shared.Data
{
    public class MessageDto : INotifyPropertyChanged
    {
        public UserDto Owner { get; set; }
        public string MessageText { get; set; }
        public System.DateTime Date { get; set; }
        public RoomDto Room { get; set; }

        public MessageDto() { }

        public MessageDto(RoomDto room, UserDto owner, string text)
        {
            MessageText = text;
            Owner = owner;
            Room = room;
        }

        public MessageDto(RoomDto room, UserDto owner, string text, System.DateTime date)
        {
            MessageText = text;
            Owner = owner;
            Date = date;
            Room = room;
        }

        public MessageDto(string text)
        {
            MessageText = text;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
