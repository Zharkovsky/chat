using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Shared.Data
{
    public class FilePartDto
    {
        public DateTime Date { get; set; }
        public string FileName { get; set; }
        public byte[] Buffer { get; set; }
        public long StartIndex { get; set; }
        public long Length { get; set; }
        public string Hash { get; set; }
        public long Weight { get; set; }
    }
}
