using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Shared.Data
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public RoomDto() { }

        public RoomDto(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
