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
        public void SendReply(MessageDto message)
        {
            if(authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Отправка сообщения пользователю");
                        callback.PrintMessage(message);
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
                MainServer.Current.SetImage(this, image);
                return User.ToUserDto(User);
            }
            else
            {
                Log.Trace("Установка изображения не удалась, пользователь не авторизован");
                return null;
            }
        }

        public void ChangeUserImage(UserDto user, ImageDto image)
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
            var request = MainServer.Current.SignUp(this, login);
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
        public List<UserDto> GetOnlineUsers()
        {
            Log.Trace("Получение пользователей");
            var task = Task.Factory.StartNew(() => MainServer.Current.GetOnlineUsers(this));
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Video stream
        /// </summary>
        /// <param name="video"></param>
        public void SendVideo(List<byte[]> video)
        {
            if(authorized)
                try
                {
                    MainServer.Current.SendVideo(this, video);
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
        public void SendVideoForUser(List<byte[]> video, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.ShowVideo(video, user);
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }
        
        public void AddOnlineUser(UserDto user)
        {
            if(authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        Task.Factory.StartNew(() => { callback.AddOnlineUser(user); });
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }
        public void RemoveOnlineUser(UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", user.Name);
                        Task.Factory.StartNew(() => callback.RemoveOnlineUser(user));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public List<MessageDto> LoadMessages(int number, System.DateTime? date)
        {
            Log.Trace("Загрузка сообщений");
            var task = Task.Factory.StartNew(()=>MainServer.Current.LoadMessages(this, number, date));
            task.Wait();
            return task.Result;
        }

        public ImageDto GetAvatar(string name)
        {
            Log.Trace("Загрузка аватарки {0}", name);
            return MainServer.Current.GetAvatar(name);
        }

        public List<UserDto> GetUsers()
        {
            Log.Trace("Получение пользователей");
            return MainServer.Current.GetUsers();
        }
        public bool IsOnline(string name)
        {
            Log.Trace("{0} онлайн?", name);
            return MainServer.Current.IsOnline(name);
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

        public void Logout()
        {
            MainServer.Current.Logout(this);
            MainServer.Current.UserLeave(this);
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

        public void SendVoiceForUser(List<byte[]> voice, UserDto user)
        {
            if (authorized && user.Name != User.Name)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.PlaySound(voice, user);
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }
    }
}