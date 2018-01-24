using AngelsChat.Server.Data.Entities;
using AngelsChat.Shared.Data;
using AngelsChat.Shared.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Server.ContractImplementations
{
    public interface IContract : IServerContract
    {
        User User { get; set; }

        void GetStarted();

        void SendReply(MessageDto message);

        void SendMessage(MessageDto message);

        UserDto SetImage(ImageDto image);

        void ChangeUserImage(UserDto user, ImageDto image);

        UserDto Login(LoginDto login);

        UserDto SignUp(LoginDto login);

        bool CheckConnection();

        List<UserDto> GetOnlineUsers();

        void SendVideo(List<byte[]> video);

        void SendVideoForUser(List<byte[]> video, UserDto user);

        void AddOnlineUser(UserDto user);

        void RemoveOnlineUser(UserDto user);

        List<MessageDto> LoadMessages(int number, System.DateTime? date);

        ImageDto GetAvatar(string name);

        List<UserDto> GetUsers();

        bool IsOnline(string name);

        byte[] GetUserFile();

        void ClearUserFile();

        long GetFile(FileMessageDto file);

        void SendVoiceForUser(List<byte[]> voice, UserDto user);

    }
}
