using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Shared.Data
{
    public class ImageDto
    {
        public byte[] Image { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        
        public ImageDto() { }

        public ImageDto(byte[] image, int width, int height)
        {
            Image = image;
            Width = width;
            Height = height;
        }
               
    }
}
