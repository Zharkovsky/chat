using AngelsChat.Shared;
using AngelsChat.Shared.Data;
using System.Collections.Generic;

namespace AngelsChat.Server.Data.Entities
{
    public class User
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public System.DateTime Date { get; set; }

        public Image Image { get; set; }


        public ICollection<Message> Messages { get; set; }
        public ICollection<FileMessage> FileMessages { get; set; }
        public ICollection<Video> Videos { get; set; }
        public ICollection<Room> Rooms { get; set; }

        //public int? ImageId { get; set; }

        public User()
        {
            FileMessages = new List<FileMessage>();
            Messages = new List<Message>();
            Videos = new List<Video>();
            Rooms = new List<Room>();
        }
        public User(UserDto viewModel)
        {
            Name = viewModel.Name;
            FileMessages = new List<FileMessage>();
            Messages = new List<Message>();
            Videos = new List<Video>();
            Rooms = new List<Room>();
        }
        public User(string name, string password)
        {
            Name = name;
            Password = password;
            Messages = new List<Message>();
            FileMessages = new List<FileMessage>();
            Videos = new List<Video>();
            Rooms = new List<Room>();
        }
        public static UserDto ToUserDto(User user)
        {
            return new UserDto(user.Name);
        }

        public override bool Equals(object obj) => (obj is User) ? Name == (obj as User).Name : false; 
        public override int GetHashCode() => Name.GetHashCode();
    }
}
