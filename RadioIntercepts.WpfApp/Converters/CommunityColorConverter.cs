// RadioIntercepts.WpfApp/Converters/CommunityColorConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RadioIntercepts.WpfApp.Converters
{
    public class CommunityColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush[] CommunityBrushes =
        {
            new SolidColorBrush(Color.FromRgb(255, 107, 107)),  // Красный
            new SolidColorBrush(Color.FromRgb(78, 205, 196)),   // Бирюзовый
            new SolidColorBrush(Color.FromRgb(255, 209, 102)),  // Желтый
            new SolidColorBrush(Color.FromRgb(6, 214, 160)),    // Зеленый
            new SolidColorBrush(Color.FromRgb(17, 138, 178)),   // Синий
            new SolidColorBrush(Color.FromRgb(239, 71, 111)),   // Розовый
            new SolidColorBrush(Color.FromRgb(38, 84, 124)),    // Темно-синий
            new SolidColorBrush(Color.FromRgb(255, 209, 102)),  // Оранжевый
            new SolidColorBrush(Color.FromRgb(6, 214, 160)),    // Салатовый
            new SolidColorBrush(Color.FromRgb(17, 138, 178))    // Голубой
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int communityId && communityId >= 0 && communityId < CommunityBrushes.Length)
            {
                return CommunityBrushes[communityId];
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}