using System;
using System.Windows.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace CrashAlert.Server
{
  class MaximunDistanceFromTopConverter : IMultiValueConverter
  {

    private const int RAW_IMAGE_HEIGHT = 480;

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (values == null || values.Length != 3 || !(values[0] is int) || !(values[1] is int) || !(values[2] is Slider))
        return 0;

      int bumperDistanceFromTop = (int)values[0];
      int bumperHeight = (int)values[1];
      Slider slBumperDistanceFromTop = (Slider)values[2];

      slBumperDistanceFromTop.InvalidateVisual();
      return (double)(RAW_IMAGE_HEIGHT - bumperHeight);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
