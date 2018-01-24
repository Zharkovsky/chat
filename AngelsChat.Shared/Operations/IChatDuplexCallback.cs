using AngelsChat.Shared.Data;
using System.Collections.Generic;
using System.ServiceModel;

namespace AngelsChat.Shared.Operations
{
    public interface IChatDuplexCallback
    {
        [OperationContract]
        [ServiceKnownType(typeof(FileMessageDto))]
        void PrintMessage(MessageDto message);
        [OperationContract]
        void AddOnlineUser(UserDto user);
        [OperationContract]
        void RemoveOnlineUser(UserDto uesr);
        [OperationContract]
        void ChangeUserImage(UserDto user, ImageDto image);
        [OperationContract]
        void ShowVideo(List<byte[]> video, UserDto user);
        [OperationContract]
        long GetFile(FileMessageDto message);
        [OperationContract]
        void PlaySound(System.Collections.Generic.List<byte[]> voice, UserDto user);
    }
}
