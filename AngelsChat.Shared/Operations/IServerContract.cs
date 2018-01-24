using AngelsChat.Shared.Data;
using System.ServiceModel;

namespace AngelsChat.Shared.Operations
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IChatDuplexCallback))]
    public interface IServerContract
    {
        [OperationContract]
        void SendReply(MessageDto message);
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
        System.Collections.Generic.List<UserDto> GetOnlineUsers();
        [OperationContract]
        void SendVideo(System.Collections.Generic.List<byte[]> video);
        [OperationContract]
        [ServiceKnownType(typeof(FileMessageDto))]
        System.Collections.Generic.List<MessageDto> LoadMessages(int number, System.DateTime? date);
        [OperationContract]
        ImageDto GetAvatar(string name);
        [OperationContract]
        System.Collections.Generic.List<UserDto> GetUsers();
        [OperationContract]
        bool IsOnline(string name);
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
        void Logout();
        [OperationContract]
        void SendVoice(System.Collections.Generic.List<byte[]> voice);
    }
}
