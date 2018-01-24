using AngelsChat.Server.Core;
using AngelsChat.Server.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Server.Data
{
    public class Video
    {
        public int Id { get; set; }
        public List<VideoElement> VideoList { get; set; }
        public User User { get; set; }
        
        public DateTime Date { get; set; }

        public Video() { }
    }

    public class VideoElement
    {
        public int Id { get; set; }

        Encoding enc = Encoding.UTF8;
        
        public byte[] Data
        {
            get;
            set;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string InternalData { get; set; }

        public VideoElement() { }

        public int? VideoId { get; set; }
        public Video Video { get; set; }
    }
}
