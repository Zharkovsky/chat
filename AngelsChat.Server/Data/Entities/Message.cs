using AngelsChat.Shared;
using AngelsChat.Shared.Data;

namespace AngelsChat.Server.Data.Entities
{
    public class Message
    {
        public int MessageId { get; set; }
        public string Text { get; set; }
        public System.DateTime Date { get; set; }
        public User User { get; set; }
        public string UserName { get; set; }

        public Message() { }
        public Message(User user, string message)
        {
            Text = message;
            User = user;
            UserName = user.Name;
        }
        public Message(User user, MessageDto viewModel)
        {
            Text = viewModel.MessageText;
            User = user;
            UserName = user.Name;
            Date = viewModel.Date;
        }

        public static MessageDto ToMessageDto(Message message)
        {
            return new MessageDto (User.ToUserDto(message.User), message.Text, message.Date);
        }
    }
}
