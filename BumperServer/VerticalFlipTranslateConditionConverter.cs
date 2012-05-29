using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using BumperServer.Properties;
using System.Windows.Controls;

namespace BumperServer
{
  class VerticalFlipTranslateConditionConverter : IMultiValueConverter
  {

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (!(values[0] is double) || double.IsNaN((double)values[0]) || !(values[1] is bool))
        return 0;

      bool verticalFlip = (bool)values[1];
      if (!verticalFlip)
        return 0;

      return (double)values[0];
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
