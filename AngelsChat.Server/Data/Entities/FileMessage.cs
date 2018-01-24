using AngelsChat.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Server.Data.Entities
{
    public class FileMessage : Message
    {
        public string FileName { get; set; }
        public long FileWeight { get; set; }
        public BinaryContent Content { get; set; }

        public static FileMessageDto ToFileMessageDto(FileMessage message)
        {
            return new FileMessageDto
            {
                Date = message.Date,
                FileName = message.FileName,
                FileWeight = message.FileWeight,
                Hash = GetMD5Hash(message.Content.Content),
                MessageText = message.Text,
                Owner = User.ToUserDto(message.User)
            };
        }

        private static string GetMD5Hash(byte[]a)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(a);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            return sBuilder.ToString();
        }
    }

    public class BinaryContent
    {
        public int Id { get; set; }
        public byte[] Content { get; set; }
        public FileMessage Message { get; set; }
    }
}
