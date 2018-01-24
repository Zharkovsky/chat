using System;
using System.Globalization;
using System.Windows.Data;

namespace AngelsChat.WpfClientApp.Helpers
{
    public class MessageDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                if (date.Date == DateTime.Now.Date) return null;
                else if (date.Date == DateTime.Now.Date.AddDays(-1)) return "Вчера";
                else return date.ToShortDateString();
            }
            else return "никогда";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
