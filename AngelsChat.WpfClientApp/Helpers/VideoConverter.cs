using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace AngelsChat.WpfClientApp.Helpers
{
    public class VideoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imageBytes = value as byte[];
            if (imageBytes == null) return null;

            MemoryStream ms = new MemoryStream(imageBytes);
            var image = new System.Windows.Media.Imaging.BitmapImage();
            image.BeginInit();
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
