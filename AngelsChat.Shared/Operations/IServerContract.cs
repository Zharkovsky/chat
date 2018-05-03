using AngelsChat.Shared.Data;
using System.ServiceModel;

namespace AngelsChat.Shared.Operations
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IChatDuplexCallback))]
    public interface IServerContract
    {
        [OperationContract]
        void SendReply(RoomDto room, MessageDto message);
        [OperationContract]
        [ServiceKnownType(typeof(FileMessageDto))]
        void SendMessage(MessageDto message);
        [OperationContract]
        UserDto SetImage(ImageDto image);
        [OperationContract]
        UserDto Login(LoginDto login);
        [OperationContract]
        bool CheckConnection();
        [OperationContract]
        UserDto SignUp(LoginDto login);
        [OperationContract]
        System.Collections.Generic.List<UserDto> GetOnlineUsers(RoomDto room);
        [OperationContract]
        void SendVideo(RoomDto room, System.Collections.Generic.List<byte[]> video);
        [OperationContract]
        [ServiceKnownType(typeof(FileMessageDto))]
        System.Collections.Generic.List<MessageDto> LoadMessages(RoomDto room, int number, System.DateTime? date);
        [OperationContract]
        ImageDto GetAvatar(string name);
        [OperationContract]
        System.Collections.Generic.List<UserDto> GetUsers(RoomDto room);
        [OperationContract]
        System.Collections.Generic.List<UserDto> GetAllUsers();
        [OperationContract]
        bool IsOnline(RoomDto room, string name);
        [OperationContract]
        int GetMaxFileLength();
        [OperationContract]
        void PrepareFile(long length);
        [OperationContract]
        void SendPartOfFile(FilePartDto file);
        [OperationContract]
        long GetFile(FileMessageDto file);
        [OperationContract]
        byte[] DownloadFile(long recieved);
        [OperationContract]
        void DeleteFile();
        [OperationContract]
        void Logout();
        [OperationContract]
        void SendVoice(RoomDto room, System.Collections.Generic.List<byte[]> voice);
        [OperationContract]
        System.Collections.Generic.List<RoomDto> GetRooms();
        [OperationContract]
        RoomDto CreateRoom(RoomDto room);
        [OperationContract]
        void RemoveRoom(RoomDto room);
        [OperationContract]
        void UpdateRoom(RoomDto room);
        [OperationContract]
        void RoomRemoved(RoomDto room);
        [OperationContract]
        void RoomUpdated(RoomDto room);
        [OperationContract]
        void InviteUser(RoomDto room, UserDto user);
    }
}
