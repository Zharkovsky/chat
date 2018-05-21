using AngelsChat.Shared.Data;
using System.Collections.Generic;

namespace AngelsChat.Server.Data.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public User Owner { get; set; }
        public string OwnerName { get; set; }
        //public User User { get; set; }
        //public string UserName { get; set; }

        public ICollection<Message> Messages { get; set; }
        public ICollection<User> Users { get; set; }

        public Room()
        {
            Messages = new List<Message>();
            Users = new List<User>();
        }
        public Room(RoomDto room)
        {
            Id = room.Id;
            Name = room.Name;
            Messages = new List<Message>();
            Users = new List<User>();
        }
        public Room(RoomDto room, User owner) : this(room)
        {
            Owner = owner;
        }

        public static RoomDto ToRoomDto(Room room)
        {
            return new RoomDto(room.Id, room.Name);
        }
    }
}
