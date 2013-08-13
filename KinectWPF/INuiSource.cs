using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectWPF
{
  public interface INuiSource
  {
    System.Windows.Media.ImageSource RawImageSource { get; }
    System.Windows.Media.ImageSource DepthImageSource { get; }
    System.Windows.Media.ImageSource RangeDepthImageSource { get; }

    int VisualAlertDistance { get; set; }

    void Dispose();
  }
}
