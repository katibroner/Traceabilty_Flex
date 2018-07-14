using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Traceabilty_Flex;

namespace MyConverters
{
    public class ObjectBorderVisibilityConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility v = Visibility.Hidden;

            TabControl tab = value as TabControl;

            if (tab.SelectedIndex != 0)
                v = Visibility.Visible;
            return v;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility v = Visibility.Visible;

            TabControl tab = value as TabControl;

            if (tab.SelectedIndex != 0)
                v = Visibility.Hidden;
            return v;
        }
    }
}