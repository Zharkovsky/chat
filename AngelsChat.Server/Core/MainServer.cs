using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data.Entity;
using NLog;
using AngelsChat.Shared.Data;
using AngelsChat.Server.Data.Entities;
using AngelsChat.Server.ContractImplementations;
using AngelsChat.Server.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Server.Core
{
    public class MainServer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static MainServer Current { get; } = new MainServer();

        ObservableCollection<IContract> UserContracts;

        public MainServer()
        {
            UserContracts = new ObservableCollection<IContract>();
            Log.Info("Инициализация MainServer");
            
            Log.Info("Аутентификация серверного аккаунта");
        }

        public void Logout(IContract contract)
        {
            UserContracts.Remove(contract);
        }
        
        public void UserEnter(IContract userContract)
        {
            Log.Debug("Пользователь {0} вошел", userContract.User.Name);
            UserContracts.Add(userContract);
        }
        public void UserLeave(IContract userContract)
        {
            Log.Debug("Пользователь {0} вышел", userContract.User.Name);
            UserContracts.Remove(userContract);
            RemoveOnlineUser(userContract);
        }

        private string GetMd5Hash(byte[] array)
        {
            //определение хэша
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(array);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            return sBuilder.ToString();
        }

        public byte[] GetFile( UserContract userContract, FileMessageDto file)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                byte[] a =  db.FileMessages.Where(_ => _.Date == file.Date && _.User.Name == file.Owner.Name && _.FileName == file.FileName).Select(_ => _.Content.Content).FirstOrDefault();
                return a;
            }
        }
        

        public void SendMessage(IContract userContract, MessageDto message)
        {
            Log.Trace("Отправка сообщения на сервере");
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User existUser = SearchUser(db, userContract.User.Name);
                message.Date = DateTime.Now;
                if (message is FileMessageDto fileMessage)
                {
                    FileMessage userMessage = new FileMessage
                    {
                        Date = fileMessage.Date,
                        FileName = fileMessage.FileName,
                        FileWeight = fileMessage.FileWeight,
                        Text = fileMessage.MessageText,
                        User = existUser,
                        UserName = existUser.Name
                    };
                    userMessage.Content = new BinaryContent { Content = userContract.GetUserFile(), Message = userMessage };
                    if (userMessage.Content.Content != null && fileMessage.Hash == GetMd5Hash(userMessage.Content.Content))
                    {
                        db.FileMessages.Add(userMessage);
                        db.Messages.Add(userMessage);
                    }
                    else
                    {
                        db.Messages.Add(new Message(existUser, message));
                    }
                    //userContract.ClearUserFile();
                }
                else
                {
                    db.Messages.Add(new Message(existUser, message));
                }
                db.SaveChanges();
            }

            lock (this)
            {
                Log.Trace("Рассылка сообщения пользователям");
                foreach (var user in UserContracts)
                    try { user.SendReply(message); }
                    catch (Exception e) { Log.Error(e); }
            }
        }

        public void SetImage(IContract userContract, ImageDto image)
        {
            Log.Trace("Установка изображения пользователя на сервере");
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                // Search user
                User existUser = SearchUser(db, userContract.User.Name);

                // Remove existed image
                if (existUser != null && existUser.Image != null)
                    db.Images.Remove(existUser.Image);

                // Create and add new image to DB
                Image userImage = new Image(existUser, image);
                existUser.Image = userImage;
                db.Images.Add(userImage);

                db.SaveChanges();

                userContract.User = existUser;
            }
            lock (this)
            {
                Log.Trace("Рассылка изображения пользователям");
                foreach (var user in UserContracts)
                    try { user.ChangeUserImage(User.ToUserDto(userContract.User), image); }
                    catch (Exception e) { Log.Error(e); }
            }
        }
        
        public UserDto Login(IContract userContract, LoginDto login)
        {
            Log.Trace("Авторизация на сервере");
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User existUser = SearchUser(db, login.Name);
                if (existUser != null && existUser.Password.Equals(login.Password) && !UserContracts.Any(_=>_.User.Name.ToLower() == login.Name.ToLower()))
                {
                    userContract.User = existUser;
                    UserContracts.Remove(UserContracts.Where(_ => _.User.Name == existUser.Name).FirstOrDefault());
                    AddToOnlineUsers(userContract);
                    Log.Debug("Авторизация успешна");
                    return User.ToUserDto(userContract.User);
                }
                else
                {
                    Log.Debug("Авторизация провалена");
                    return null;
                }
            }
        }
        public UserDto SignUp(IContract userContract, LoginDto login)
        {
            Log.Trace("Регистрация на сервере");
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User existUser = SearchUser(db, login.Name);

                if (existUser == null)
                {
                    existUser = new User(login.Name, login.Password);
                    existUser.Date = DateTime.Now;
                    db.Users.Add(existUser);
                    db.SaveChanges();

                    userContract.User = existUser;
                    
                    UserContracts.Remove(UserContracts.Where(_ => _.User.Name == existUser.Name).FirstOrDefault());
                    AddToOnlineUsers(userContract);
                    Log.Debug("Регистрация успешна");
                    return User.ToUserDto(userContract.User);
                }
                else
                {
                    Log.Debug("Регистрация провалена");
                    return null;
                }
            }
        }

        public void SendVoice(IContract userContract, List<byte[]> voice)
        {
            lock (this)
            {
                Task.Factory.StartNew(() => SaveVoice(userContract, voice));

                foreach (var user in UserContracts)
                    try { user.SendVoiceForUser(voice, User.ToUserDto(userContract.User)); }
                    catch (Exception e) { Log.Error(e); }
            }
        }

        private void SaveVoice(IContract userContract, List<byte[]> video) { }

        public void SendVideo(IContract userContract, List<byte[]> video)
        {
            lock (this)
            {
                Task.Factory.StartNew(()=> SaveVideo(userContract, video));
                
                foreach (var user in UserContracts)
                    try
                    {
                        user.SendVideoForUser(video, User.ToUserDto(userContract.User));
                    }
                    catch (Exception e) { Log.Error(e); }
            }
        }
        private void SaveVideo(IContract userContract, List<byte[]> video)
        {
            //using (Context db = new Context(ServerHost.Settings.Ef))
            //{
            //    Video userVideo = db.Videos
            //        .Include(_ => _.VideoList)
            //        .Where(_ => _.User.Name == userContract.User.Name)
            //        .FirstOrDefault();
            //    if (userVideo == null)
            //    {
            //        userVideo = new Video
            //        {
            //            User = db.Users.Where(_ => _.Name == userContract.User.Name).FirstOrDefault(),
            //            Date = DateTime.Now
            //        };
            //        db.Videos.Add(userVideo);
            //        db.SaveChanges();
            //    }
            //    db.VideoElements.AddRange(video.Select(_ => new VideoElement { Data = _, Video = userVideo }));
            //    db.SaveChanges();
            //}
        }

        public List<UserDto> GetOnlineUsers(IContract userContract)
        {
            Log.Trace("Получение онлайн пользователей");
            return UserContracts.Select(u => User.ToUserDto(u.User)).ToList();
        }

        private User SearchUser(Context db, string name)
        {
            try
            {
                Log.Trace("Поиск пользователя");
                User existUser = db.Users.Include(_ => _.Image)
                .Where(b => b.Name == name)
                .FirstOrDefault();
                return existUser;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            return null;
        }
        

        private void AddToOnlineUsers(IContract userContract)
        {
            lock (this)
            {
                Log.Trace("Добавление пользователя ко всем");
                foreach (var user in UserContracts)
                    try
                    {
                        user.AddOnlineUser(User.ToUserDto(userContract.User));
                    }
                    catch (Exception e) { Log.Error(e); }
            }
        }
        private void RemoveOnlineUser(IContract userContract)
        {
            lock(this)
            {
                Log.Trace("Удаление пользователя");
                foreach (var user in UserContracts)
                    try { user.RemoveOnlineUser(User.ToUserDto(userContract.User)); }
                    catch (Exception e) { Log.Error(e); }
            }       
        }

        public List<MessageDto> LoadMessages(IContract userContract, int number, DateTime? date)
        {
            if (date == null) date = DateTime.Now;
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                //db.Database.Log = Console.Write;
                try
                {
                    Log.Trace("Загрука сообщений");

                    return db.Messages.Include(_ => _.User.Image).Include(_ => _.User)
                        .Where(_ => _.Date < date && _.Date > userContract.User.Date)
                        .OrderByDescending(_ => _.Date)
                        .Take(number)
                        .ToList().Select(_ =>
                        {
                            if (_ is FileMessage message)
                            {
                                return db.FileMessages
                                        .Include(fileMessage => fileMessage.Content)
                                        .Include(fileMessage => fileMessage.User.Image)
                                        .Include(fileMessage => fileMessage.User)
                                        .Where(fileMessage => fileMessage.Date == message.Date && fileMessage.UserName == message.UserName && fileMessage.FileName == message.FileName)
                                        .ToList()
                                        .Select(fileMessage => FileMessage.ToFileMessageDto(fileMessage))
                                        .FirstOrDefault()
                                    ;
                            }
                            else
                            {
                                return Message.ToMessageDto(_);
                            }
                        })
                        .ToList();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                return null;
            }
        }

        public ImageDto GetAvatar(string name)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                try
                {
                    Log.Trace("Загрука аватарки {0}", name);
                    return Image.ToImageDto(db.Images.Where(_ => _.User.Name == name).FirstOrDefault());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                return null;
            }
        }
        public List<UserDto> GetUsers()
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                try
                {
                    Log.Trace("Загрука пользователей");
                    return db.Users.ToList().Select(_ => User.ToUserDto(_)).ToList();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                return null;
            }
        }

        public bool IsOnline(string name)
        {
            try { Log.Trace("Пользователь {0} онлайн?", name); return UserContracts.Where(_ => _.User.Name == name && name != "Сервер").FirstOrDefault() == null ? false : true; }
            catch (Exception e) { Log.Error(e); return false; }
        }
        

    }
}
