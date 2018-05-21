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
using System.IO;

namespace AngelsChat.Server.ContractImplementations
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class UserContract : IContract
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public UserContract() {}

        private IChatDuplexCallback callback;
        public User User { get; set; }
        private bool authorized = false;

        /// <summary>
        /// Start work when logged
        /// </summary>
        public void GetStarted()
        {
            Log.Info("Инициализация UserContract");
            MainServer.Current.UserEnter(this);
            OperationContext.Current.InstanceContext.Closing += (e, a) => MainServer.Current.UserLeave(this);
            callback = OperationContext.Current.GetCallbackChannel<IChatDuplexCallback>();
            authorized = true;
        }
        
        /// <summary>
        /// Message for user
        /// </summary>
        public void SendReply(RoomDto room, MessageDto message)
        {
            if(authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Отправка сообщения пользователю");
                        callback.PrintMessage(room, message);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
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
        /// Change user image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public UserDto SetImage(ImageDto image)
        {
            if (authorized)
            {
                Log.Trace("Отправка изображения на сервер");
                Task.Factory.StartNew(() => MainServer.Current.SetAvatar(this, image));
                return User.ToUserDto(User);
            }
            else
            {
                Log.Trace("Установка изображения не удалась, пользователь не авторизован");
                return null;
            }
        }

        public void ChangeUserImage(RoomDto room, UserDto user, ImageDto image)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Изменить изображение");
                        callback.ChangeUserImage(user, image);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public UserDto UpdateProfile(LoginDto login)
        {
            var request = MainServer.Current.UpdateProfile(this, login);
            return request;
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

        /// <summary>
        /// Get online users
        /// </summary>
        /// <returns></returns>
        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            Log.Trace("Получение пользователей");
            var task = Task.Factory.StartNew(() => MainServer.Current.GetOnlineUsers(room));
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Video stream
        /// </summary>
        /// <param name="video"></param>
        public void SendVideo(RoomDto room, List<byte[]> video)
        {
            if(authorized)
                try
                {
                    MainServer.Current.SendVideo(room, this, video);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                
        }

        public void AddRoomForVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.AddRoomForVideo(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

        }

        public void RemoveRoomFromVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.RemoveRoomFromVideo(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

        }
        /// <summary>
        /// Video for user
        /// </summary>
        /// <param name="video"></param>
        /// <param name="user"></param>
        public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.ShowVideo(room, video, user);
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void AddUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        Task.Factory.StartNew(() => { callback.AddUser(room, user); });
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RemoveUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", user.Name);
                        Task.Factory.StartNew(() => callback.RemoveUser(room, user));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            if(authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        Task.Factory.StartNew(() => { callback.AddOnlineUser(room, user); });
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
                        Task.Factory.StartNew(() => callback.RemoveOnlineUser(room, user));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RoomRemoved(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", room.Name);
                        Task.Factory.StartNew(() => callback.RoomRemoved(room));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RoomUpdated(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", room.Name);
                        Task.Factory.StartNew(() => callback.RoomUpdated(room));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void UpdateProfileOnClientSide(UserDto user, string name)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.UpdateProfileOnClientSide(user, name);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public List<MessageDto> LoadMessages(RoomDto room, int number, System.DateTime? date)
        {
            Log.Trace("Загрузка сообщений");
            var task = Task.Factory.StartNew(()=>MainServer.Current.LoadMessages(room, this, number, date));
            task.Wait();
            return task.Result;
        }

        public ImageDto GetAvatar(string name)
        {
            Log.Trace("Загрузка аватарки {0}", name);
            return MainServer.Current.GetAvatar(name);
        }

        public List<UserDto> GetUsers(RoomDto room)
        {
            Log.Trace("Получение пользователей");
            return MainServer.Current.GetUsers(room);
        }

        public List<UserDto> GetAllUsers()
        {
            Log.Trace("Получение пользователей");
            return MainServer.Current.GetAllUsers();
        }

        public bool IsOnline(RoomDto room, string name)
        {
            Log.Trace("{0} онлайн?", name);
            return MainServer.Current.IsOnline(room, name);
        }

        public int GetMaxFileLength()
        {
            var task = Task.Factory.StartNew(() => 1024);
            task.Wait();
            return task.Result;
        }

        private byte[] userFile;
        public byte[] GetUserFile() => userFile;
        public void ClearUserFile() => Array.Clear(userFile, 0, userFile.Length);

        public void PrepareFile(long length) => userFile = new byte[length];
        public void SendPartOfFile(FilePartDto file) => Task.Factory.StartNew(() => Array.Copy(file.Buffer, 0, userFile, file.StartIndex, file.Length));
        
        byte[] serverFile;
        public long GetFile(FileMessageDto file)
        {
            var task = Task.Factory.StartNew(() => MainServer.Current.GetFile(this, file));
            task.Wait();
            serverFile = task.Result;
            return serverFile.Length;
        }
        public byte[] DownloadFile(long recieved)
        {
            long length = Math.Min(GetMaxFileLength(), serverFile.Length - recieved);
            byte[] partOfFile = new byte[length];
            Array.Copy(serverFile, recieved, partOfFile, 0, length);
            return partOfFile;
        }
        public void DeleteFile()
        {
            serverFile = null;
        }

        public void Logout()
        {
            MainServer.Current.Logout(this);
        }

        public void SendVoice(List<byte[]> voice)
        {
            if (authorized)
                try
                {
                    MainServer.Current.SendVoice(this, voice);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
        {
            if (authorized && user.Name != User.Name)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.PlaySound(room, voice, user);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public List<RoomDto> GetRooms()
        {
            if (authorized)
                try
                {
                    return MainServer.Current.GetRooms(this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            return null;
        }

        public RoomDto CreateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    return MainServer.Current.CreateRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            return null;
        }

        public void RemoveRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.RemoveRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void UpdateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.UpdateRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void InviteUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    MainServer.Current.InviteUser(room, user, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }
        public void KickUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    MainServer.Current.KickUser(room, user, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }
    }
}