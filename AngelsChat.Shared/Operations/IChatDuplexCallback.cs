using AngelsChat.Shared.Data;
using System.Collections.Generic;
using System.ServiceModel;

namespace AngelsChat.Shared.Operations
{
    public interface IChatDuplexCallback
    {
        [OperationContract]
        [ServiceKnownType(typeof(FileMessageDto))]
        void PrintMessage(RoomDto room, MessageDto message);
        [OperationContract]
        void AddUser(RoomDto room, UserDto user);
        [OperationContract]
        void RemoveUser(RoomDto room, UserDto user);
        [OperationContract]
        void AddOnlineUser(RoomDto room, UserDto user);
        [OperationContract]
        void RemoveOnlineUser(RoomDto room, UserDto user);
        [OperationContract]
        void ChangeUserImage(UserDto user, ImageDto image);
        [OperationContract]
        void ShowVideo(RoomDto room, List<byte[]> video, UserDto user);
        [OperationContract]
        long GetFile(FileMessageDto message);
        [OperationContract]
        void PlaySound(RoomDto room, List<byte[]> voice, UserDto user);
        [OperationContract]
        RoomDto CreateRoom(RoomDto room);
        [OperationContract]
        void RemoveRoom(RoomDto room);
        [OperationContract]
        void RoomRemoved(RoomDto room);
        [OperationContract]
        void RoomUpdated(RoomDto room);
    }
}
