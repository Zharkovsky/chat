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

namespace AngelsChat.Server.Core
{
    public class MainServer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static MainServer Current { get; } = new MainServer();

        private ObservableCollection<RoomManager> Rooms;
        private ObservableCollection<IContract> UserContracts;

        public MainServer()
        {
            Rooms = new ObservableCollection<RoomManager>();
            UserContracts = new ObservableCollection<IContract>();
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                db.Rooms.Include(_ => _.Users).ToList().ForEach(room => Rooms.Add(new RoomManager(room)));
            }
        }

        #region Rooms

        public RoomManager GetRoom(RoomDto room)
        {
            if (room == null) return null;
            else return Rooms.Where(_ => _.Room.Id == room.Id).FirstOrDefault();
        }
        public List<RoomDto> GetRooms(IContract userContract)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User user = SearchUser(db, userContract.User.Name);
                var rooms = Rooms.Where(_ => _.GetUsers().Select(u => u.Name).Contains(user.Name)).ToList().Select(_ => Room.ToRoomDto(_.Room)).ToList();
                return rooms;
            }
        }

        public RoomDto CreateRoom(RoomDto room, IContract userContract)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User user = SearchUser(db, userContract.User.Name);
                var r = new Room(room, user);
                db.Rooms.Add(r);
                db.SaveChanges();
                var rm = new RoomManager(r);
                rm.InviteUser(User.ToUserDto(user));
                rm.UserEnter(userContract);
                rm.AddOnlineUser(userContract);
                Rooms.Add(rm);
                //db.SaveChanges();
                return Room.ToRoomDto(rm.Room);
            }
        }

        public void RemoveRoom(RoomDto room, IContract userContract)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                var _room = db.Rooms.Where(_ => _.Id == room.Id).FirstOrDefault();
                db.Rooms.Remove(_room);
                db.SaveChanges();
            }
            var r = GetRoom(room);
            r?.Close();
            Rooms.Remove(r);
        }

        public void UpdateRoom(RoomDto room, IContract userContract)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                var _room = db.Rooms.Where(_ => _.Id == room.Id).FirstOrDefault();
                _room.Name = room.Name;
                db.SaveChanges();
            }
            var r = GetRoom(room);
            r.UpdateRoom(room);
        }

        #endregion

        #region Users

        public UserDto Login(IContract userContract, LoginDto login)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                User existUser = SearchUser(db, login.Name);
                bool ok = (existUser != null && 
                           existUser.Password.Equals(login.Password) && 
                           Rooms.Where(_ => _.IsOnline(login.Name)).Count() == 0);
                if (!ok) return null;
                userContract.User = existUser;
                AddToOnlineUsers(userContract);
                return User.ToUserDto(userContract.User);
            }
        }
        public void Logout(IContract contract) => UserLeave(contract)/*RemoveOnlineUser(contract)*/;

        public UserDto Registration(IContract userContract, LoginDto login)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                if (SearchUser(db, login.Name) != null) return null;
                var existUser = new User(login.Name, login.Password) { Date = DateTime.Now };
                db.Users.Add(existUser);
                db.SaveChanges();
                return Login(userContract, login);
            }
        }

        public void UserEnter(IContract userContract)
        {
            Rooms.ToList().ForEach(room => room.UserEnter(userContract));
            if (UserContracts.ToList().Where(_ => _.User.Name == userContract.User.Name).Count() == 0)
                UserContracts.Add(userContract);
        }
        public void UserLeave(IContract userContract)
        {
            Rooms.ToList().ForEach(room => room.UserLeave(userContract));
            UserContracts.Remove(userContract);
        }

        private void AddToOnlineUsers(IContract userContract) => 
            Rooms
            .Where(_ => 
                _.GetUsers()
                 .Where(u=>u.Name.ToLower() == userContract.User.Name.ToLower())
                 .Count() != 0)
            .ToList()
            .ForEach(room => room.AddOnlineUser(userContract));
        private void RemoveOnlineUser(IContract userContract) => Rooms.ToList().ForEach(room => room.RemoveOnlineUser(userContract));

        private User SearchUser(Context db, string name) => db.Users.Include(_ => _.Rooms).Include(_ => _.Image).Where(b => b.Name == name).FirstOrDefault();
        //private User SearchUser(Context db, string name) => db.Users.Include(_=>_.Rooms).Include(_ => _.Image).Where(b => b.Name == name).FirstOrDefault();
        #endregion

        #region Messages

        public void SendMessage(IContract userContract, MessageDto message) => GetRoom(message.Room)?.SendMessage(userContract, message);
        public List<MessageDto> LoadMessages(RoomDto room, IContract userContract, int number, DateTime? date) => GetRoom(room).LoadMessages(userContract, number, date);

        #endregion

        #region Actions

        public void SetAvatar(IContract userContract, ImageDto image) => Rooms.ToList().ForEach(room => room.SetAvatar(userContract, image));
        public ImageDto GetAvatar(string name)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                try
                {
                    return Image.ToImageDto(db.Images.Where(_ => _.User.Name == name).FirstOrDefault());
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                return null;
            }
        }

        public void SendVoice(RoomDto room, IContract userContract, List<byte[]> voice) => GetRoom(room).SendVoice(userContract, voice);
        public void SendVideo(RoomDto room, IContract userContract, List<byte[]> video) => GetRoom(room).SendVideo(userContract, video);

        public List<UserDto> GetOnlineUsers(RoomDto room) => GetRoom(room).GetOnlineUsers();
        public List<UserDto> GetUsers(RoomDto room) => GetRoom(room)?.GetUsers();
        public List<UserDto> GetAllUsers()
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                try
                {
                    return db.Users.ToList().Select(_ => User.ToUserDto(_)).ToList();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                return null;
            }
        }
        public bool IsOnline(RoomDto room, string name) => GetRoom(room).IsOnline(name);

        public byte[] GetFile(UserContract userContract, FileMessageDto file)
        {
            using (Context db = new Context(ServerHost.Settings.Ef))
            {
                byte[] a = db.FileMessages.Where(_ => _.Date == file.Date && _.User.Name == file.Owner.Name && _.FileName == file.FileName).Select(_ => _.Content.Content).FirstOrDefault();
                return a;
            }
        }

        public void InviteUser(RoomDto room, UserDto person, UserContract userContract)
        {
            var rm = Rooms.Where(_ => _.Room.Id == room.Id).FirstOrDefault();
            rm.InviteUser(person);

            var invitedUserContract = UserContracts.Where(_ => _.User.Name == person.Name).FirstOrDefault();
            if (invitedUserContract == null) return;

            rm.AddUser(invitedUserContract, rm.Room);
            rm.UserEnter(invitedUserContract);
            rm.AddOnlineUser(invitedUserContract);
        }
        #endregion
    }
}
