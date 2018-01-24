using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Shared.Data
{
    public class LoginDto
    {
        public string Name { get; set; }
        public string Password { get; set; }

        public LoginDto() { }
        public LoginDto(string name, string password)
        {
            Password = password;
            Name = name;
        }
    }
}
