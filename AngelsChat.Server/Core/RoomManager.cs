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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AngelsChat.Server.Communication;

namespace AngelsChat.Server.Core
{
    public class RoomManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ObservableCollection<IContract> UserContracts;

        public Room Room;

        public RoomManager(Room room)
        {
            Room = room;
            UserContracts = new ObservableCollection<IContract>();
        }

        public void Open()
        {
            UserContracts = new ObservableCollection<IContract>();
        }

        public void Close()
        {
            UserContracts.ToList().ForEach(_ =>
            {
                try
                {
                    _.RoomRemoved(Room.ToRoomDto(Room));
                }
                catch (Exception e) { }
            });
        }

        public void UpdateRoom(RoomDto room)
        {
            Room.Name = room.Name;
            UserContracts.ToList().ForEach(_ =>
            {
                try
                {
                    _.RoomUpdated(room);
                }
                catch (Exception e) { }
            });
        }

        #region Users

        public bool IsOnline(string name)
        {
            var result = UserContracts.Where(_ => _.User.Name == name && name != "Сервер").FirstOrDefault() == null ? false : true;
            return result;
        }

        public void AddUser(IContract userContract, Room room)
        {
            userContract.AddUser(Room.ToRoomDto(Room), User.ToUserDto(userContract.User));

            if (GetUsers().ToList().Where(_ => _.Name == userContract.User.Name).Count() == 0)
            {
                UserContracts.ToList().ForEach(_ =>
                {
                    try
                    {
                        _.AddUser(Room.ToRoomDto(Room), User.ToUserDto(userContract.User));
                    }
                    catch (Exception e) { }
                });
            }
        }

        public void RemoveUser(IContract userContract, Room room)
        {
            UserContracts.ToList().ForEach(_ =>
            {
                try
                {
                    _.RemoveUser(Room.ToRoomDto(Room), User.ToUserDto(userContract.User));
                }
                catch (Exception e) { }
            });
        }

        public void AddOnlineUser(IContract userContract) => UserContracts.ToList().ForEach(user => user.AddOnlineUser(Room.ToRoomDto(Room), User.ToUserDto(userContract.User)));
        public void RemoveOnlineUser(IContract userContract) => UserContracts.ToList().ForEach(user => user.RemoveOnlineUser(Room.ToRoomDto(Room), User.ToUserDto(userContract.User)));

        public void UserEnter(IContract userContract)
        {
            if (GetUsers().ToList().Where(_ => _.Name == userContract.User.Name).Count() != 0)
                UserContracts.Add(userContract);
        }
        public void UserLeave(IContract userContract)
        {
            UserContracts.Remove(userContract);
            RemoveOnlineUser(userContract);
        }

        public List<UserDto> GetUsers()
        {
            //using (Context db = new Context(ServerHost.Settings.Ef))
            //{
            //    try
            //    {
            //        var users = db.Rooms.Include(_ => _.Users).Where(_ => _.Id == Room.Id).FirstOrDefault().Users.Select(_ => User.ToUserDto(_)).ToList();
            //        //var u1 = db.Users.Include(_ => _.Rooms).ToList();
            //        //var users = u1
            //        //    .Where(_=>_.Rooms.Select(r => r.Id).Contains(Room.Id))
            //        //    .Select(_ => User.ToUserDto(_))
            //        //    .ToList();
            //        return users;
            //    }
            //    catch (Exception e)
            //    {
            //        Log.Error(e);
            //    }
            //    return null;
            //}
            return Room.Users.Select(_ => User.ToUserDto(_)).ToList();
        }

        private User SearchUser(Context db, string name) =>
            db.Users
                .Include(_ => _.Rooms)
                .Include(_ => _.Messages)
                .Include(_ => _.Image)
                .Where(b => b.Name == name)
                .FirstOrDefault();

        public void InviteUser(UserDto person)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                var user = SearchUser(db, person.Name);
                var room = db.Rooms.Include(_ => _.Users).Where(_ => _.Id == Room.Id).FirstOrDefault();
                user.Rooms.Add(room);
                db.SaveChanges();
                Room.Users.Add(user);
            }
        }

        public void KickUser(UserDto person)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                var user = SearchUser(db, person.Name);
                var room = db.Rooms.Include(_ => _.Users).Where(_ => _.Id == Room.Id).FirstOrDefault();
                user.Rooms.Remove(room);
                db.SaveChanges();
                Room.Users.Remove(user);
            }
        }

        public void UpdateUserProfile(User user, string name)
        {
            UserContracts.ToList().ForEach(_ => _.UpdateProfileOnClientSide(User.ToUserDto(user), name));
        }
        #endregion

        #region Messages

        public void SendMessage(IContract userContract, MessageDto message)
        {
            message.Date = DateTime.Now;
            message.Room = Room.ToRoomDto(Room);
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User existUser = SearchUser(db, userContract.User.Name);
                var existRoom = db.Rooms.Where(_ => _.Id == Room.Id).FirstOrDefault();

                if (message is FileMessageDto fileMessage)
                {
                    FileMessage userMessage = new FileMessage
                    {
                        Date = fileMessage.Date,
                        FileName = fileMessage.FileName,
                        FileWeight = fileMessage.FileWeight,
                        Text = fileMessage.MessageText,
                        User = existUser,
                        //UserName = existUser.Name,
                        Room = existRoom,
                        //RoomId = existRoom.Id
                    };
                    userMessage.Content = new BinaryContent { Content = userContract.GetUserFile(), Message = userMessage };
                    if (userMessage.Content.Content != null && fileMessage.Hash == GetMd5Hash(userMessage.Content.Content))
                    {
                        db.FileMessages.Add(userMessage);
                        db.Messages.Add(userMessage);
                    }
                    else
                    {
                        db.Messages.Add(new Message(existRoom, existUser, message));
                    }
                }
                else
                {
                    //existUser.Messages.Add(new Message(Room, existUser, message));
                    //var u = db.Users.Where(_ => _.Name == userContract.User.Name).FirstOrDefault();
                    db.Messages.Add(new Message(existRoom, existUser, message));
                }
                db.SaveChanges();
            }
            UserContracts.ToList().ForEach(user =>
            {
                try
                {
                    user.SendReply(Room.ToRoomDto(Room), message);
                }
                catch { }
            });
        }
        public List<MessageDto> LoadMessages(IContract userContract, int number, DateTime? date)
        {
            if (date == null) date = DateTime.Now;
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                try
                {
                    return db.Messages
                        .Include(_ => _.User.Image).Include(_ => _.User).Include(_ => _.Room)
                        .Where(_ => _.Room.Id == Room.Id && _.Date < date && _.Date > userContract.User.Date)
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
                                        .Include(fileMessage => fileMessage.Room)
                                        .Where(fileMessage => fileMessage.Date == message.Date &&
                                                              fileMessage.User.Name == message.User.Name &&
                                                              fileMessage.FileName == message.FileName)
                                        .ToList()
                                        .Select(fileMessage => FileMessage.ToFileMessageDto(fileMessage))
                                        .FirstOrDefault();
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

        #endregion

        #region Actions

        public void SetAvatar(IContract userContract, ImageDto image)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User existUser = SearchUser(db, userContract.User.Name);
                if (existUser != null && existUser.Image != null)
                    db.Images.Remove(existUser.Image);
                Image userImage = new Image(existUser, image);
                existUser.Image = userImage;
                db.Images.Add(userImage);
                db.SaveChanges();
                userContract.User = existUser;
            }
            UserContracts.Where(_ => _.User.Name != "Сервер").ToList().ForEach(user => user.ChangeUserImage(Room.ToRoomDto(Room), User.ToUserDto(userContract.User), image));
        }

        public void SendVoice(IContract userContract, List<byte[]> voice)
        {
            Task.Factory.StartNew(() => SaveVoice(userContract, voice));
            UserContracts.Where(_ => _.User.Name != "Сервер").ToList().ForEach(user => user.SendVoiceForUser(Room.ToRoomDto(Room), voice, User.ToUserDto(userContract.User)));
        }

        public void SendVideo(IContract userContract, List<byte[]> video)
        {
            Task.Factory.StartNew(() => SaveVideo(userContract, video));
            UserContracts.Where(_ => _.User.Name != "Сервер").ToList().ForEach(user => user.SendVideoForUser(Room.ToRoomDto(Room), video, User.ToUserDto(userContract.User)));
        }

        public List<UserDto> GetOnlineUsers() => UserContracts.Select(u => User.ToUserDto(u.User)).ToList();

        private void SaveVoice(IContract userContract, List<byte[]> video) { }
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

        #endregion
    }
}