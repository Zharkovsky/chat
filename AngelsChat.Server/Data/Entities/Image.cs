using AngelsChat.Shared.Data;

namespace AngelsChat.Server.Data.Entities
{
    public class Image
    {
        public int Id { get; set; }
        public User User { get; set; }
        public byte[] Picture { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Image() { }
        public Image(User user, ImageDto image)
        {
            Picture = image?.Image;
            User = user;
            Width = image.Width;
            Height = image.Height;
        }
        public Image(User user, byte[] image)
        {
            Picture = image;
            User = user;
        }

        public static ImageDto ToImageDto(Image image)
        {
            if (image == null)
                return null;
            else
                return new ImageDto { Image = image.Picture, Width = image.Width, Height = image.Height};
        }
    }
}
