using System.ServiceModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using System;
using AngelsChat.Shared.Data;
using AngelsChat.Shared.Operations;
using AngelsChat.Server.Data.Entities;
using AngelsChat.Server.Core;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNet.SignalR;

namespace AngelsChat.Server.ContractImplementations
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ServerContract : Hub, IContract
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public ServerContract()
        {
            LoginDto loginDto = new LoginDto("Сервер", "123");
            UserDto user = Login(loginDto);
            if (user == null)
                user = SignUp(loginDto);
            User = new User(user);
            MainServer.Current.ServerContract = this;
           
        }

        public User User { get; set; }
        private bool authorized = false;

        /// <summary>
        /// Start work when logged
        /// </summary>
        public void GetStarted()
        {
            Log.Info("Инициализация ServerContract");
            MainServer.Current.UserEnter(this);
            
            authorized = true;
        }

        /// <summary>
        /// Message from user
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(MessageDto message)
        {
            if (authorized)
            {
                Log.Trace("Отправка сообщения на сервер");
                MainServer.Current.SendMessage(this, message);
            }
            else
                Log.Trace("Отправка сообщения не удалась, пользователь не авторизован");
        }

        /// <summary>
        /// Authorization
        /// </summary>
        public UserDto Login(LoginDto login)
        {
            var request = MainServer.Current.Login(this, login);
            if (request != null)
            {
                GetStarted();
                Log.Info("Авторизация успешна, {0}", login.Name);
            }
            else
            {
                Log.Info("Авторизация не удалась, {0}", login.Name);
            }
            return request;
        }

        /// <summary>
        /// Registration
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public UserDto SignUp(LoginDto login)
        {
            var request = MainServer.Current.Registration(this, login);
            if (request != null)
            {
                GetStarted();
                Log.Info("Регистрация успешна, {0}", login.Name);
            }
            else
            {
                Log.Info("Регистрация не удалась, {0}", login.Name);
            }
            return request;
        }

        public bool CheckConnection()
        {
            Log.Debug("Соединение успешно");
            return true;
        }

        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        SendMessage(new MessageDto { Date = DateTime.Now, MessageText = $"Пользователь {user.Name} вошел в чат", Owner = User.ToUserDto(User), Room = room });
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }
        public void RemoveOnlineUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", user.Name);
                        SendMessage(new MessageDto { Date = DateTime.Now, MessageText = $"Пользователь {user.Name} вышел из чата", Owner = User.ToUserDto(User), Room = room });
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void SendReply(RoomDto room, MessageDto message)
        {
            throw new NotImplementedException();
        }

        public UserDto SetImage(ImageDto image)
        {
            throw new NotImplementedException();
        }

        public void ChangeUserImage(RoomDto room, UserDto user, ImageDto image)
        {
            throw new NotImplementedException();
        }

        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public void SendVideo(RoomDto room, List<byte[]> video)
        {
            throw new NotImplementedException();
        }

        public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
        {
            throw new NotImplementedException();
        }

        public List<MessageDto> LoadMessages(RoomDto room, int number, DateTime? date)
        {
            throw new NotImplementedException();
        }

        public ImageDto GetAvatar(string name)
        {
            throw new NotImplementedException();
        }

        public List<UserDto> GetUsers(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public bool IsOnline(RoomDto room, string name)
        {
            throw new NotImplementedException();
        }

        public byte[] GetUserFile()
        {
            throw new NotImplementedException();
        }

        public void ClearUserFile()
        {
            throw new NotImplementedException();
        }

        public long GetFile(FileMessageDto file)
        {
            throw new NotImplementedException();
        }

        public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
        {
            throw new NotImplementedException();
        }

        public int GetMaxFileLength()
        {
            throw new NotImplementedException();
        }

        public void PrepareFile(long length)
        {
            throw new NotImplementedException();
        }

        public void SendPartOfFile(FilePartDto file)
        {
            throw new NotImplementedException();
        }

        public byte[] DownloadFile(long recieved)
        {
            throw new NotImplementedException();
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public void SendVoice(RoomDto room, List<byte[]> voice)
        {
            throw new NotImplementedException();
        }

        public List<RoomDto> GetRooms()
        {
            throw new NotImplementedException();
        }

        public RoomDto CreateRoom(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public void RemoveRoom(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public List<UserDto> GetAllUsers()
        {
            throw new NotImplementedException();
        }

        public void UpdateRoom(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public void InviteUser(RoomDto room, UserDto user)
        {
            throw new NotImplementedException();
        }

        public void AddUser(RoomDto room, UserDto user)
        {
            throw new NotImplementedException();
        }

        public void RemoveUser(RoomDto room, UserDto user)
        {
            throw new NotImplementedException();
        }

        public void RoomRemoved(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public void RoomUpdated(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile()
        {
            throw new NotImplementedException();
        }

        public void KickUser(RoomDto room, UserDto user)
        {
            throw new NotImplementedException();
        }

        public UserDto UpdateProfile(LoginDto login)
        {
            throw new NotImplementedException();
        }

        public void UpdateProfileOnClientSide(UserDto uer, string name)
        {
            throw new NotImplementedException();
        }

        public void AddRoomForVideo(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public void RemoveRoomFromVideo(RoomDto room)
        {
            throw new NotImplementedException();
        }

        public void SendVoice(List<byte[]> voice)
        {
            throw new NotImplementedException();
        }
    }
}
