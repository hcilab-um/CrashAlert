using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using CrashAlert.Server.Properties;
using System.Windows.Controls;

namespace CrashAlert.Server
{
  class VerticalFlipScaleConditionConverter : IMultiValueConverter
  {

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (!(values[0] is Image) || !(values[1] is bool))
        return (double)1;

      bool verticalFlip = (bool)values[1];
      if (!verticalFlip)
        return (double)1;

      return (double)-1;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
