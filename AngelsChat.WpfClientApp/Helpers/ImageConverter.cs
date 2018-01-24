using AngelsChat.Shared;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using System.Globalization;
using AngelsChat.Shared.Data;

namespace AngelsChat.WpfClientApp.Helpers
{
    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imageBytes = value as ImageDto;
            if (imageBytes == null) return null;
            PixelFormat pf = PixelFormats.Bgr24;
            int rawStride = (imageBytes.Width * pf.BitsPerPixel + 7) / 8;
            BitmapSource bitmap = BitmapSource.Create(imageBytes.Width, imageBytes.Height, 96, 96, pf, null, imageBytes.Image, rawStride);
            return bitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
