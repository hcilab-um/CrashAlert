using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace BumperServer
{
  class BumperTypeToDoubleConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (!(value is BumperServer.BumperType) || !(parameter is String))
        return 0;

      BumperServer.BumperType type = (BumperServer.BumperType)Enum.Parse(typeof(BumperServer.BumperType), parameter.ToString());
      BumperServer.BumperType selected = (BumperServer.BumperType)value;

      if (type == selected)
        return 1;
      return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
