using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace CrashAlert.Server
{
  class BumperTypeToDoubleConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (!(value is CrashAlert.Server.BumperType) || !(parameter is String))
        return 0;

      CrashAlert.Server.BumperType type = (CrashAlert.Server.BumperType)Enum.Parse(typeof(CrashAlert.Server.BumperType), parameter.ToString());
      CrashAlert.Server.BumperType selected = (CrashAlert.Server.BumperType)value;

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
