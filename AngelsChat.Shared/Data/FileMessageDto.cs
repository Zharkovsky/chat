using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Shared.Data
{
    public class FileMessageDto : MessageDto
    {
        public string FileName { get; set; }
        public long FileWeight { get; set; }
        public string Hash { get; set; }
    }
}
