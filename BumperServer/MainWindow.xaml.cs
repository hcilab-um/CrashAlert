using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenNI;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using KinectWPF;
using System.ComponentModel;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.IO.Ports;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.IO;
using BumperServer.Properties;
using log4net;

namespace BumperServer
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {

    private readonly string SAMPLE_XML_FILE = @"Data/SamplesConfig.xml";
    private static readonly ILog logger = LogManager.GetLogger(typeof(MainWindow));

    private INuiSource _sensor;
    private BackgroundWorker _workerImages = new BackgroundWorker();

    private NetworkManager _network;
    private BackgroundWorker _workerNetwork = new BackgroundWorker();

    private Image<Rgb, byte> _ocvRaw = null;
    private Image<Rgb, byte> _ocvRawCompact = null;
    private Image<Rgb, byte> _ocvDepth = null;
    private Image<Rgb, byte> _ocvDepthCompact = null;
    private Image<Gray, byte> _ocvRawDepth = null;
    private Image<Gray, byte> _ocvRawDepthCompact = null;

    private Image<Rgb, byte> _ocvBlackBumper = null;
    private Image<Rgb, byte> _ocvColorBumper = null;
    private Image<Rgb, byte> _ocvDepthBumper = null;
    private Image<Rgb, byte> _ocvAvgHighDepthColumnsBumper = null;
    private Image<Rgb, byte> _ocvDepthMaskOnColorBumper = null;
    private Image<Rgb, byte> _ocvMaskOnColorBumper = null;

    private Image<Gray, byte> _ocvGrayBumper = null;
    private Image<Gray, byte> _ocvHighDepthBumper = null;
    private Image<Gray, byte> _ocvHighDepthColumnsBumper = null;
    private Image<Gray, byte> _ocvGrayDepthBumper = null;
    private Image<Gray, byte> _ocvClosestObjectRangeBumper = null;
    private Image<Gray, byte> _ocvMaskOnGrayBumper = null;
    private Image<Rgb, byte> _ocvIconicBumper = null;

    private BumperType _bumperSelected = BumperType.Color;
    public BumperType BumperSelected
    {
      get { return _bumperSelected; }
      set
      {
        _bumperSelected = value;
        NotifyPropertyChanged("BumperSelected");
      }
    }

    private int _bumperHeight = 50;
    public int BumperHeight
    {
      get { return _bumperHeight; }
      set
      {
        int oldValue = _bumperHeight;
        _bumperHeight = value;
        _isSnapShotGenerated = false;
        CalculateBumperDistanceFromTop(_bumperHeight - oldValue);
        CalculateSlidingWindow();
        NotifyPropertyChanged("BumperHeight");
      }
    }

    private int _bumperDistanceFromTop = 165;
    public int BumperDistanceFromTop
    {
      get { return _bumperDistanceFromTop; }
      set
      {
        _bumperDistanceFromTop = value;
        _isSnapShotGenerated = false;
        CalculateSlidingWindow();
        NotifyPropertyChanged("BumperDistanceFromTop");
      }
    }

    private int _frameWindowBorder = 3;
    public int FrameWindowBorder
    {
      get { return _frameWindowBorder; }
      set
      {
        _frameWindowBorder = value;
        CalculateSlidingWindow();
        NotifyPropertyChanged("FrameWindowBorder");
      }
    }

    private bool _useCompression = false;
    public bool UseCompression
    {
      get { return _useCompression; }
      set
      {
        _useCompression = value;
        NotifyPropertyChanged("UseCompression");
      }
    }

    private bool _isSnapShotGenerated = false;
    private bool _generateAll = false;
    public bool GenerateAll
    {
      get { return _generateAll; }
      set
      {
        _generateAll = value;
        _isSnapShotGenerated = false;
        NotifyPropertyChanged("GenerateAll");
      }
    }

    private bool _isConnected = false;
    public bool IsConnected
    {
      get { return _isConnected; }
      set
      {
        _isConnected = value;
        NotifyPropertyChanged("IsConnected");
        NotifyPropertyChanged("IsNotConnected");
      }
    }

    public bool IsNotConnected
    {
      get { return !_isConnected; }
    }

    private bool _verticalFlip = false;
    public bool VerticalFlip
    {
      get { return _verticalFlip; }
      set
      {
        _verticalFlip = value;
        _isSnapShotGenerated = false;
        CalculateSlidingWindow();
        NotifyPropertyChanged("VerticalFlip");
      }
    }

    private bool _visualAlerts = true;
    public bool VisualAlerts
    {
      get { return _visualAlerts; }
      set
      {
        _visualAlerts = value;
        _isSnapShotGenerated = false;
        NotifyPropertyChanged("VisualAlerts");
      }
    }

    private int _visualAlertSide = 50;
    public int VisualAlertSide
    {
      get { return _visualAlertSide; }
      set
      {
        _visualAlertSide = value;
        _isSnapShotGenerated = false;
        NotifyPropertyChanged("VisualAlertSide");
      }
    }

    private int _brightness = 25;
    public int Brightness
    {
      get { return _brightness; }
      set
      {
        _brightness = value;
        _isSnapShotGenerated = false;
        NotifyPropertyChanged("Brightness");
      }
    }

    public int VisualAlertDistance
    {
      get
      {
        if (_sensor == null)
          return Settings.Default.VisualAlertDistance;
        return _sensor.VisualAlertDistance;
      }
      set
      {
        _sensor.VisualAlertDistance = value;
        _isSnapShotGenerated = false;
        NotifyPropertyChanged("VisualAlertDistance");
      }
    }

    private bool _phoneVerticalFlip = false;
    private BumperType _phoneBumperType = BumperType.Color;
    private int _phoneBumperHeight = -1;
    private int _phoneBumperDistanceFromTop = -1;
    private int _phoneBrightness = -1;

    public MainWindow()
    {
      log4net.Config.XmlConfigurator.Configure();
      InitializeComponent();

      if (Settings.Default.SimulateKinect)
        _sensor = new KinnectSimulator();
      else
        _sensor = new NuiSensor(SAMPLE_XML_FILE);
      _workerImages.DoWork += new DoWorkEventHandler(WorkerImages_DoWork);
      _workerImages.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WorkerImages_RunWorkerCompleted);

      _network = new NetworkManager(Settings.Default.PhoneBumperGuid, this);
      _workerNetwork.DoWork += new DoWorkEventHandler(WorkerNetwork_DoWork);

      BumperSelected = BumperType.AvgHighDepthColumns;
      UseCompression = Settings.Default.UseCompression;
      GenerateAll = Settings.Default.GenerateAll;
      BumperHeight = Settings.Default.BumperHeight;
      BumperDistanceFromTop = Settings.Default.BumperDistanceFromTop;
      VerticalFlip = Settings.Default.VerticalFlip;
      FrameWindowBorder = Settings.Default.FrameWindowBorder;
      VisualAlerts = Settings.Default.VisualAlerts;
      VisualAlertDistance = Settings.Default.VisualAlertDistance;
      Brightness = Settings.Default.Brightness;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      if (!_workerImages.IsBusy)
        _workerImages.RunWorkerAsync();

      Dispatcher.BeginInvoke(new Action(() =>
        {
          _network.Init();
        }));
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      _sensor.Dispose();
      _network.Dispose();

      //Save settings
      Settings.Default.UseCompression = UseCompression;
      Settings.Default.GenerateAll = GenerateAll;
      Settings.Default.BumperHeight = BumperHeight;
      Settings.Default.BumperDistanceFromTop = BumperDistanceFromTop;
      Settings.Default.VerticalFlip = VerticalFlip;
      Settings.Default.FrameWindowBorder = FrameWindowBorder;
      Settings.Default.VisualAlertDistance = VisualAlertDistance;
      Settings.Default.VisualAlerts = VisualAlerts;
      Settings.Default.Brightness = Brightness;
      Settings.Default.Save();
    }

    void WorkerImages_DoWork(object sender, DoWorkEventArgs e)
    {
      Dispatcher.Invoke((Action)delegate
      {
        try
        {
          if (_sensor.RawImageSource == null || _sensor.DepthImageSource == null || _sensor.RawDepthImageSource == null)
            return; //This happens when the program is closing

          if (iKinectColor.Source == null || iKinectDepth.Source == null)
          {
            iKinectColor.Source = _sensor.RawImageSource;
            WriteableBitmap wbRaw = (WriteableBitmap)iKinectColor.Source;
            _ocvRaw = new Image<Rgb, byte>((int)wbRaw.Width, (int)wbRaw.Height, wbRaw.BackBufferStride, wbRaw.BackBuffer);

            iKinectDepth.Source = _sensor.DepthImageSource;
            WriteableBitmap wbDepth = (WriteableBitmap)iKinectDepth.Source;
            _ocvDepth = new Image<Rgb, byte>((int)wbDepth.Width, (int)wbDepth.Height, wbDepth.BackBufferStride, wbDepth.BackBuffer);

            WriteableBitmap wbRawDepth = (WriteableBitmap)_sensor.RawDepthImageSource;
            _ocvRawDepth = new Image<Gray, byte>((int)wbRawDepth.Width, (int)wbRawDepth.Height, wbRawDepth.BackBufferStride, wbRawDepth.BackBuffer);
          }
        }
        catch (Exception exception)
        { return; }

        SetBumper(_ocvBlackBumper, iBlackBumper);
        SetBumper(_ocvColorBumper, iColorBumper);
        SetBumper(_ocvGrayBumper, iGrayBumper);
        SetBumper(_ocvDepthBumper, iDepthBumper);
        SetBumper(_ocvDepthMaskOnColorBumper, iDepthMaskOnColorBumper);
        SetBumper(_ocvHighDepthBumper, iHighDepthBumper);
        SetBumper(_ocvHighDepthColumnsBumper, iHighDepthColumnsBumper);
        SetBumper(_ocvAvgHighDepthColumnsBumper, iAvgHighDepthColumnsBumper);
        SetBumper(_ocvGrayDepthBumper, iGrayDepthBumper);
        SetBumper(_ocvClosestObjectRangeBumper, iClosestObjectRangeBumper);
        SetBumper(_ocvMaskOnColorBumper, iMaskOnColorBumper);
        SetBumper(_ocvMaskOnGrayBumper, iMaskOnGrayBumper);
        SetBumper(_ocvIconicBumper, iIconicBumper);
      });

      if (_slidingWindowCompact == System.Drawing.Rectangle.Empty || _slidingWindowFrame == System.Drawing.Rectangle.Empty)
        CalculateSlidingWindow();

      _ocvRawCompact = _ocvRaw.Resize(Settings.Default.CompactScale, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
      _ocvDepthCompact = _ocvDepth.Resize(Settings.Default.CompactScale, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
      _ocvRawDepthCompact = _ocvRawDepth.Resize(Settings.Default.CompactScale, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
      if (VerticalFlip)
      {
        _ocvRawCompact = _ocvRawCompact.Flip(Emgu.CV.CvEnum.FLIP.VERTICAL);
        _ocvDepthCompact = _ocvDepthCompact.Flip(Emgu.CV.CvEnum.FLIP.VERTICAL);
        _ocvRawDepthCompact = _ocvRawDepthCompact.Flip(Emgu.CV.CvEnum.FLIP.VERTICAL);
      }

      _ocvRaw.Draw(_slidingWindowFrame, new Rgb(System.Drawing.Color.White), FrameWindowBorder);
      _ocvDepth.Draw(_slidingWindowFrame, new Rgb(System.Drawing.Color.White), FrameWindowBorder);

      CreateVisualAlerts();
      _ocvRawCompact = BrigthenUpImage(_ocvRawCompact);

      if (_generateAll || !_isSnapShotGenerated)
      {
        try
        {
          _ocvBlackBumper = CreateBlackBumper(_ocvRawCompact);
          _ocvColorBumper = CreateColorBumper(_ocvRawCompact);
          _ocvGrayBumper = CreateGrayBumper(_ocvColorBumper);
          _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
          _ocvHighDepthBumper = CreateHighDepthBumper(_ocvDepthBumper);
          _ocvHighDepthColumnsBumper = CreateHighDepthColumnsBumper(_ocvHighDepthBumper);
          _ocvAvgHighDepthColumnsBumper = CreateAvgHighDepthColumnsBumper(_ocvHighDepthBumper);

          _ocvGrayDepthBumper = CreateGrayDepthBumper(_ocvDepthBumper);
          _ocvDepthMaskOnColorBumper = CreateDepthMaskOnColorBumper(_ocvColorBumper, _ocvGrayDepthBumper);
          _ocvClosestObjectRangeBumper = CreateClosestObjectRangeBumper(_ocvGrayDepthBumper, Settings.Default.ClosestObjectRange);
          _ocvMaskOnColorBumper = CreateMaskOnColorBumper(_ocvColorBumper, _ocvHighDepthColumnsBumper);
          _ocvMaskOnGrayBumper = CreateMaskOnGrayBumper(_ocvGrayBumper, _ocvHighDepthColumnsBumper);
          _ocvIconicBumper = CreateIconicBumper(_ocvGrayDepthBumper);

          if (_visualAlerts)
          {
            PaintVisualAlerts(_ocvDepthBumper);
            PaintVisualAlerts(_ocvAvgHighDepthColumnsBumper);
            PaintVisualAlerts(_ocvDepthMaskOnColorBumper);
            PaintVisualAlerts(_ocvMaskOnColorBumper);
          }

          _isSnapShotGenerated = true;
        }
        catch (Exception exception)
        { return; }
      }
      else
      {
        try
        {
          switch (BumperSelected)
          {
            case BumperType.Black:
              _ocvBlackBumper = CreateBlackBumper(_ocvRawCompact);
              break;
            case BumperType.Color:
              _ocvColorBumper = CreateColorBumper(_ocvRawCompact);
              break;
            case BumperType.Gray:
              _ocvColorBumper = CreateColorBumper(_ocvRawCompact);
              _ocvGrayBumper = CreateGrayBumper(_ocvColorBumper);
              break;
            case BumperType.Depth:
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              PaintVisualAlerts(_ocvDepthBumper);
              break;
            case BumperType.DepthMaskOnColor:
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvGrayDepthBumper = CreateGrayDepthBumper(_ocvDepthBumper);
              _ocvDepthMaskOnColorBumper = CreateDepthMaskOnColorBumper(_ocvColorBumper, _ocvGrayDepthBumper);
              PaintVisualAlerts(_ocvDepthBumper);
              PaintVisualAlerts(_ocvDepthMaskOnColorBumper);
              break;
            case BumperType.HighDepth:
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvHighDepthBumper = CreateHighDepthBumper(_ocvDepthBumper);
              break;
            case BumperType.HighDepthColumns:
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvHighDepthBumper = CreateHighDepthBumper(_ocvDepthBumper);
              _ocvHighDepthColumnsBumper = CreateHighDepthColumnsBumper(_ocvHighDepthBumper);
              break;
            case BumperType.AvgHighDepthColumns:
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvHighDepthBumper = CreateHighDepthBumper(_ocvDepthBumper);
              _ocvAvgHighDepthColumnsBumper = CreateAvgHighDepthColumnsBumper(_ocvHighDepthBumper);
              PaintVisualAlerts(_ocvDepthBumper);
              PaintVisualAlerts(_ocvAvgHighDepthColumnsBumper);
              break;
            case BumperType.GrayDepth:
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvGrayDepthBumper = CreateGrayDepthBumper(_ocvDepthBumper);
              break;
            case BumperType.ClosestObjectRange:
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvGrayDepthBumper = CreateGrayDepthBumper(_ocvDepthBumper);
              _ocvClosestObjectRangeBumper = CreateClosestObjectRangeBumper(_ocvGrayDepthBumper, Settings.Default.ClosestObjectRange);
              break;
            case BumperType.MaskOnColor:
              _ocvColorBumper = CreateColorBumper(_ocvRawCompact);
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvHighDepthBumper = CreateHighDepthBumper(_ocvDepthBumper);
              _ocvHighDepthColumnsBumper = CreateHighDepthColumnsBumper(_ocvHighDepthBumper);
              _ocvMaskOnColorBumper = CreateMaskOnColorBumper(_ocvColorBumper, _ocvHighDepthColumnsBumper);
              PaintVisualAlerts(_ocvDepthBumper);
              PaintVisualAlerts(_ocvMaskOnColorBumper);
              break;
            case BumperType.MaskOnGray:
              _ocvColorBumper = CreateColorBumper(_ocvRawCompact);
              _ocvGrayBumper = CreateGrayBumper(_ocvColorBumper);
              _ocvDepthBumper = CreateDepthBumper(_ocvDepthCompact);
              _ocvHighDepthBumper = CreateHighDepthBumper(_ocvDepthBumper);
              _ocvHighDepthColumnsBumper = CreateHighDepthColumnsBumper(_ocvHighDepthBumper);
              _ocvMaskOnGrayBumper = CreateMaskOnGrayBumper(_ocvGrayBumper, _ocvHighDepthColumnsBumper);
              break;
            case BumperType.Iconic:
              _ocvIconicBumper = CreateIconicBumper(_ocvGrayDepthBumper);
              break;
          }
        }
        catch (Exception exception)
        { return; }
      }
    }

    void WorkerImages_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      if (!_workerImages.IsBusy)
        _workerImages.RunWorkerAsync();
      if (!_workerNetwork.IsBusy)
        _workerNetwork.RunWorkerAsync();
    }

    private Image<Rgb, byte> CreateAvgHighDepthColumnsBumper(Image<Gray, byte> ocvHighDepthBumper)
    {
      double lastColumnAvg = 0;
      Rgb columnPixel = new Rgb(lastColumnAvg, lastColumnAvg, lastColumnAvg);

      Image<Rgb, byte> ocvAvgHighDepthColumns = ocvHighDepthBumper.Convert<Rgb, byte>();
      for (int column = 0; column < ocvHighDepthBumper.Width; column++)
      {
        double sum = 0;
        for (int row = 0; row < ocvHighDepthBumper.Height; row++)
        {
          sum += ocvHighDepthBumper[row, column].Intensity;
          ocvAvgHighDepthColumns[row, column] = columnPixel;
        }

        lastColumnAvg = sum / ocvAvgHighDepthColumns.Height;
        columnPixel = new Rgb(lastColumnAvg, lastColumnAvg, lastColumnAvg);
      }
      return ocvAvgHighDepthColumns;
    }

    private Image<Gray, byte> CreateHighDepthColumnsBumper(Image<Gray, byte> ocvHighDepthBumper)
    {
      double lastColumnMax = 0;
      Gray columnPixel = new Gray(lastColumnMax);

      Image<Gray, byte> ocvHighDepthColumns = ocvHighDepthBumper.Copy();
      for (int column = 0; column < ocvHighDepthColumns.Width; column++)
      {
        double max = 0;
        for (int row = 0; row < ocvHighDepthColumns.Height; row++)
        {
          if (ocvHighDepthColumns[row, column].Intensity > max)
            max = ocvHighDepthColumns[row, column].Intensity;
          ocvHighDepthColumns[row, column] = columnPixel;
        }

        lastColumnMax = max;
        columnPixel = new Gray(lastColumnMax);
      }
      return ocvHighDepthColumns;
    }

    private Image<Gray, byte> CreateHighDepthBumper(Image<Rgb, byte> ocvDepthBumper)
    {
      Image<Gray, byte> ocvHighDepthBumper = ocvDepthBumper.Convert<Gray, byte>();
      ocvHighDepthBumper = ocvHighDepthBumper.ThresholdBinary(new Gray(120), new Gray(255));
      return ocvHighDepthBumper;
    }

    private Image<Rgb, byte> CreateAvgDepthColumnsBumper(Image<Rgb, byte> ocvDepthBumper)
    {
      double lastColumnAvg = 0;
      Rgb columnPixel = new Rgb(lastColumnAvg, 0, 0);

      Image<Rgb, byte> ocvAvgDepthColumns = ocvDepthBumper.Copy();
      for (int column = 0; column < ocvAvgDepthColumns.Width; column++)
      {
        double sum = 0;
        for (int row = 0; row < ocvAvgDepthColumns.Height; row++)
        {
          sum += ocvAvgDepthColumns[row, column].Green;
          ocvAvgDepthColumns[row, column] = columnPixel;
        }

        lastColumnAvg = sum / ocvAvgDepthColumns.Height;
        columnPixel = new Rgb(lastColumnAvg, 0, 0);
      }
      return ocvAvgDepthColumns;
    }

    private Image<Rgb, byte> CreateDepthBumper(Image<Rgb, byte> ocvDepth)
    {
      Image<Rgb, byte> ocvDepthBumper = ocvDepth.Copy(_slidingWindowCompact);
      return ocvDepthBumper;
    }

    private Image<Gray, byte> CreateGrayBumper(Image<Rgb, byte> ocvColorBumper)
    {
      Image<Gray, byte> ocvGrayBumper = ocvColorBumper.Convert<Gray, byte>();
      return ocvGrayBumper;
    }

    private Image<Rgb, byte> CreateColorBumper(Image<Rgb, byte> ocvRaw)
    {
      Image<Rgb, byte> ocvColorBumper = ocvRaw.Copy(_slidingWindowCompact);
      return ocvColorBumper;
    }

    private Image<Gray, byte> CreateGrayDepthBumper(Image<Rgb, byte> ocvDepthBumper)
    {
      Image<Gray, byte> ocvDepthGrayBumper = ocvDepthBumper.Convert<Gray, byte>();
      return ocvDepthGrayBumper;
    }

    private Image<Gray, byte> CreateClosestObjectRangeBumper(Image<Gray, byte> ocvGrayDepthBumper, int range)
    {
      double closestIntensity = 0;
      Image<Gray, byte> ocvClosestObjectRange = ocvGrayDepthBumper.Copy();
      for (int column = 0; column < ocvClosestObjectRange.Width; column++)
      {
        for (int row = 0; row < ocvClosestObjectRange.Height; row++)
        {
          if (ocvClosestObjectRange[row, column].Intensity > closestIntensity)
            closestIntensity = ocvClosestObjectRange[row, column].Intensity;
        }
      }
      ocvClosestObjectRange = ocvClosestObjectRange.ThresholdBinary(new Gray(closestIntensity - range), new Gray(255));

      return ocvClosestObjectRange;
    }

    private Image<Rgb, byte> CreateMaskOnColorBumper(Image<Rgb, byte> ocvColorBumper, Image<Gray, byte> ocvHighDepthColumnsBumper)
    {
      Image<Rgb, byte> ocvMaskOnColorBumper = ocvColorBumper.Copy(ocvHighDepthColumnsBumper);
      return ocvMaskOnColorBumper;
    }

    private Image<Gray, byte> CreateMaskOnGrayBumper(Image<Gray, byte> ocvGrayBumper, Image<Gray, byte> ocvHighDepthColumnsBumper)
    {
      Image<Gray, byte> ocvMaskOnGrayBumper = ocvGrayBumper.Copy(ocvHighDepthColumnsBumper);
      return ocvMaskOnGrayBumper;
    }

    private Image<Rgb, byte> CreateIconicBumper(Image<Gray, byte> ocvGrayDepthBumper)
    {
      return null;
    }

    private Image<Rgb, byte> CreateDepthMaskOnColorBumper(Image<Rgb, byte> ocvColorBumper, Image<Gray, byte> ocvGrayDepthBumper)
    {
      Image<Rgb, byte> ocvDepthMaskOnColorBumper = ocvColorBumper.Copy();

      for (int column = 0; column < ocvDepthMaskOnColorBumper.Width; column++)
      {
        for (int row = 0; row < ocvDepthMaskOnColorBumper.Height; row++)
        {
          //New_R = CInt((255 - R) * (A / 255.0) + R)
          //New_G = CInt((255 - G) * (A / 255.0) + G)
          //New_B = CInt((255 - G) * (A / 255.0) + B)
          Rgb pixel = ocvDepthMaskOnColorBumper[row, column];
          double alpha = ocvGrayDepthBumper[row, column].Intensity / 255;
          if (alpha < 0.45)
            alpha = 0;
          double newR = pixel.Red * alpha;
          double newG = pixel.Green * alpha;
          double newB = pixel.Blue * alpha;
          ocvDepthMaskOnColorBumper[row, column] = new Rgb(newR, newG, newB);
        }
      }
      return ocvDepthMaskOnColorBumper;
    }

    private Image<Rgb, byte> CreateBlackBumper(Image<Rgb, byte> ocvRawCompact)
    {
      Image<Rgb, byte> ocvBlackBumper = new Image<Rgb, byte>(_slidingWindowCompact.Width, _slidingWindowCompact.Height, new Rgb(0, 0, 0));
      return ocvBlackBumper;
    }

    private Image<Rgb, byte> BrigthenUpImage(Image<Rgb, byte> ocvImage)
    {
      return ocvImage.Add(new Rgb(_brightness, _brightness, _brightness));
    }

    private Contour<System.Drawing.Point> possibleCollisions = null;
    private void CreateVisualAlerts()
    {
      if (!_visualAlerts)
        return;
      _ocvRawDepthCompact = _ocvRawDepthCompact.Copy(_slidingWindowCompact);
      possibleCollisions = _ocvRawDepthCompact.FindContours();
    }

    private void PaintVisualAlerts(Image<Rgb, byte> ocvColorImage)
    {
      if (!_visualAlerts || possibleCollisions == null)
        return;

      while (possibleCollisions.HNext != null)
      {
        if (possibleCollisions.Area < 100)
        {
          possibleCollisions = possibleCollisions.HNext;
          continue;
        }

        ocvColorImage.Draw(new System.Drawing.Rectangle(
          possibleCollisions.BoundingRectangle.Left + possibleCollisions.BoundingRectangle.Width / 2 - VisualAlertSide / 2,
          ocvColorImage.Height / 2 - VisualAlertSide / 2,
          VisualAlertSide, VisualAlertSide), new Rgb(255, 0, 0), 0);

        possibleCollisions = possibleCollisions.HNext;
      }
      ocvColorImage.Draw(new System.Drawing.Rectangle(
        possibleCollisions.BoundingRectangle.Left + possibleCollisions.BoundingRectangle.Width / 2 - VisualAlertSide / 2,
        ocvColorImage.Height / 2 - VisualAlertSide / 2,
        VisualAlertSide, VisualAlertSide), new Rgb(255, 0, 0), 0);

      while (possibleCollisions.HPrev != null)
        possibleCollisions = possibleCollisions.HPrev;
    }

    private void CalculateBumperDistanceFromTop(int deltaSlidingWindowHeight)
    {
      if (deltaSlidingWindowHeight == 0)
        return;

      int dft = _bumperDistanceFromTop - (int)(deltaSlidingWindowHeight / 2);
      if (dft < 0)
        dft = 0;
      BumperDistanceFromTop = dft;
    }

    System.Drawing.Rectangle _slidingWindowCompact = System.Drawing.Rectangle.Empty;
    System.Drawing.Rectangle _slidingWindowFrame = System.Drawing.Rectangle.Empty;
    private void CalculateSlidingWindow()
    {
      if (_ocvRaw == null)
        return;

      int sourceWidth = _ocvRaw.Width;
      int sourceHeight = _ocvRaw.Height;

      int compactWidth = (int)(sourceWidth * Settings.Default.CompactScale);
      int compactHeight = (int)(sourceHeight * Settings.Default.CompactScale);

      int compactBumperHeight = (int)(_bumperHeight * Settings.Default.CompactScale);
      int compactBumperDistanceFromTop = (int)(_bumperDistanceFromTop * Settings.Default.CompactScale);
      int frameBumperDistanceFromTop = _bumperDistanceFromTop;

      if (VerticalFlip)
        frameBumperDistanceFromTop = (sourceHeight - _bumperHeight) - frameBumperDistanceFromTop;

      _slidingWindowCompact = new System.Drawing.Rectangle(
        0,
        compactBumperDistanceFromTop,
        compactWidth,
        compactBumperHeight);

      _slidingWindowFrame = new System.Drawing.Rectangle(
          -FrameWindowBorder,
          frameBumperDistanceFromTop - FrameWindowBorder,
          sourceWidth + 2 * FrameWindowBorder,
          _bumperHeight + 2 * FrameWindowBorder
        );
    }

    private void SetBumper(IImage ocvBumper, System.Windows.Controls.Image iTargetUI)
    {
      if (ocvBumper == null)
        return;

      IntPtr ptrCB = ocvBumper.Bitmap.GetHbitmap();
      BitmapSource bsCB = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ptrCB,
        IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
      DeleteObject(ptrCB);
      iTargetUI.Source = bsCB;
    }

    void WorkerNetwork_DoWork(object sender, DoWorkEventArgs e)
    {
      if (!_network.IsConnected)
      {
        if (IsConnected)
          IsConnected = false;
        return;
      }

      if (!IsConnected)
        IsConnected = true;

      try
      {
        //1- Sends the local state
        _network.SendState();

        //2- Send currently selected image
        IImage imageToSend = null;
        switch (BumperSelected)
        {
          case BumperType.Black:
            imageToSend = _ocvBlackBumper;
            break;
          case BumperType.Color:
            imageToSend = _ocvColorBumper;
            break;
          case BumperType.Gray:
            imageToSend = _ocvGrayBumper;
            break;
          case BumperType.Depth:
            imageToSend = _ocvDepthBumper;
            break;
          case BumperType.DepthMaskOnColor:
            imageToSend = _ocvDepthMaskOnColorBumper;
            break;
          case BumperType.HighDepth:
            imageToSend = _ocvHighDepthBumper;
            break;
          case BumperType.HighDepthColumns:
            imageToSend = _ocvHighDepthColumnsBumper;
            break;
          case BumperType.AvgHighDepthColumns:
            imageToSend = _ocvAvgHighDepthColumnsBumper;
            break;
          case BumperType.GrayDepth:
            imageToSend = _ocvGrayDepthBumper;
            break;
          case BumperType.ClosestObjectRange:
            imageToSend = _ocvClosestObjectRangeBumper;
            break;
          case BumperType.MaskOnColor:
            imageToSend = _ocvMaskOnColorBumper;
            break;
          case BumperType.MaskOnGray:
            imageToSend = _ocvMaskOnGrayBumper;
            break;
          case BumperType.Iconic:
            imageToSend = _ocvIconicBumper;
            break;
        }
        _network.SendImage(imageToSend, _useCompression);

        //3- Receives the log file from the remote device - the input text
        List<String> remoteLogs = _network.ReadLog();
        foreach (String remoteLog in remoteLogs)
          logger.Info(String.Format("{0};{1};{2};{3};{4}", DateTime.Now.Ticks, _bumperSelected, _bumperHeight, _bumperDistanceFromTop, remoteLog));

        //4- Receives the phone's state
        _network.ReadPhoneState();
      }
      catch (Exception exception)
      {
        Console.WriteLine(exception.Message);
      }
    }

    public void ProcessPhoneState(BumperType rPhoneBumperType, bool rPhoneVerticalFlip, int rPhoneBumperHeight, int rPhoneBumperDistanceFromTop, int rBrightness)
    {
      if (_phoneBumperType != rPhoneBumperType)
        BumperSelected = rPhoneBumperType;
      _phoneBumperType = rPhoneBumperType;

      if (_phoneVerticalFlip != rPhoneVerticalFlip)
        VerticalFlip = rPhoneVerticalFlip;
      _phoneVerticalFlip = rPhoneVerticalFlip;

      if (_phoneBumperHeight != rPhoneBumperHeight)
        BumperHeight = rPhoneBumperHeight;
      _phoneBumperHeight = rPhoneBumperHeight;

      if (_phoneBumperDistanceFromTop != rPhoneBumperDistanceFromTop)
        BumperDistanceFromTop = rPhoneBumperDistanceFromTop;
      _phoneBumperDistanceFromTop = rPhoneBumperDistanceFromTop;

      if (_phoneBrightness != rBrightness)
        Brightness = rBrightness;
      _phoneBrightness = rBrightness;
    }

    [DllImport("gdi32")]
    private static extern int DeleteObject(IntPtr o);

    public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(String propertyName)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    private void iColorBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.Color;
    }

    private void iGrayBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.Gray;
    }

    private void iDepthBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.Depth;
    }

    private void iDepthMaskOnColorBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.DepthMaskOnColor;
    }

    private void iHighDepthBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.HighDepth;
    }

    private void iHighDepthColumnsBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.HighDepthColumns;
    }

    private void iAvgHighDepthColumnsBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.AvgHighDepthColumns;
    }

    private void iGrayDepthBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.GrayDepth;
    }

    private void iClosestObjectRangeBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.ClosestObjectRange;
    }

    private void iMaskOnColorBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.MaskOnColor;
    }

    private void iMaskOnGrayBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.MaskOnGray;
    }

    private void iIconicBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.Iconic;
    }

    private void iBlackBumper_MouseDown(object sender, MouseButtonEventArgs e)
    {
      BumperSelected = BumperType.Black;
    }

  }

}
