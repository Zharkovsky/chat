using AngelsChat.Shared;
using AngelsChat.Shared.Data;
using System.Collections.Generic;

namespace AngelsChat.Server.Data.Entities
{
    public class User
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public System.DateTime Date { get; set; }

        public ICollection<Message> Messages { get; set; }
        public ICollection<FileMessage> FileMessages { get; set; }
        public List<Video> Videos { get; set; }

        public int? ImageId { get; set; }
        public Image Image { get; set; }

        public User() { }
        public User(UserDto viewModel)
        {
            Name = viewModel.Name;
        }
        public User(string name, string password)
        {
            Name = name;
            Password = password;
        }
        public static UserDto ToUserDto(User user)
        {
            return new UserDto(user.Name);
        }

        public override bool Equals(object obj) => (obj is User) ? Name == (obj as User).Name : false; 
        public override int GetHashCode() => Name.GetHashCode();
    }
}
