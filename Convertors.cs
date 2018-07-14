using Traceabilty_Flex;

namespace MyConverters
{
    public class ObjectBorderVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility v = Visibility.Hidden;

            List<MyObject> myObjects = value as List<MyObject>;
            foreach (Object myobject in myObjects)
            {
                if (myobject.IsVisible)
                    v = Visibility.Visible;
            }
            return v;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("ObjectBorderVisibilityConvertercan only be used OneWay.");
        }
    }
}