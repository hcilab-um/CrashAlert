using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenNI;

namespace KinectWPF
{
  /// <summary>
  /// Represents a Natural User Interface (image and depth) sensor handler.
  /// </summary>
  public class NuiSensor : INuiSource
  {
    #region Constants

    /// <summary>
    /// Default configuration file path.
    /// </summary>
    private const string CONFIGURATION = @"SamplesConfig.xml";

    /// <summary>
    /// Horizontal bitmap dpi.
    /// </summary>
    private readonly int DPI_X = 96;

    /// <summary>
    /// Vertical bitmap dpi.
    /// </summary>
    private readonly int DPI_Y = 96;

    #endregion

    #region Members

    /// <summary>
    /// Thread responsible for image and depth camera updates.
    /// </summary>
    private Thread _cameraThread;

    /// <summary>
    /// Indicates whether the thread is running.
    /// </summary>
    private bool _isRunning = true;

    /// <summary>
    /// Image camera source.
    /// </summary>
    private WriteableBitmap _imageBitmap;

    /// <summary>
    /// Histogram-based depth camera source.
    /// </summary>
    private WriteableBitmap _depthBitmap;

    /// <summary>
    /// The real depth camera image
    /// </summary>
    private WriteableBitmap _rawDepthBitmap;

    /// <summary>
    /// Raw image metadata.
    /// </summary>
    private ImageMetaData _imgMD = new ImageMetaData();

    /// <summary>
    /// Depth image metadata.
    /// </summary>
    private DepthMetaData _depthMD = new DepthMetaData();

    /// <summary>
    /// ScriptNode object -- JDHR: Added when migrating to the newer version of the OpenNI.net
    /// </summary>
    private ScriptNode scriptNode;

    #endregion

    #region Properties

    #region Bitmap properties

    /// <summary>
    /// Returns the image camera's bitmap source.
    /// </summary>
    public ImageSource RawImageSource
    {
      get
      {
        if (_imageBitmap != null)
        {
          _imageBitmap.Lock();
          _imageBitmap.WritePixels(new Int32Rect(0, 0, _imgMD.XRes, _imgMD.YRes), _imgMD.ImageMapPtr, (int)_imgMD.DataSize, _imageBitmap.BackBufferStride);
          _imageBitmap.Unlock();
        }

        return _imageBitmap;
      }
    }

    /// <summary>
    /// Returns the depth camera's bitmap source.
    /// </summary>
    public ImageSource DepthImageSource
    {
      get
      {
        if (_depthBitmap != null)
        {
          UpdateHistogram(_depthMD);

          _depthBitmap.Lock();

          unsafe
          {
            ushort* pDepth = (ushort*)DepthGenerator.DepthMapPtr;
            for (int y = 0; y < _depthMD.YRes; ++y)
            {
              byte* pDest = (byte*)_depthBitmap.BackBuffer.ToPointer() + y * _depthBitmap.BackBufferStride;
              for (int x = 0; x < _depthMD.XRes; ++x, ++pDepth, pDest += 3)
              {
                byte pixel = (byte)Histogram[*pDepth];
                pDest[0] = 0;
                pDest[1] = pixel;
                pDest[2] = pixel;
              }
            }
          }

          _depthBitmap.AddDirtyRect(new Int32Rect(0, 0, _depthMD.XRes, _depthMD.YRes));
          _depthBitmap.Unlock();
        }

        return _depthBitmap;
      }
    }

    private int _visualAlertDistance = 1024;
    public int VisualAlertDistance
    {
      get { return _visualAlertDistance; }
      set { _visualAlertDistance = value; }
    }

    /// <summary>
    /// Returns the depth camera's bitmap source to a certain depth
    /// </summary>
    public ImageSource RangeDepthImageSource
    {
      get
      {
        if (_rawDepthBitmap != null)
        {
          _rawDepthBitmap.Lock();

          unsafe
          {
            int maxDepth = DepthGenerator.DeviceMaxDepth;
            ushort* pDepth = (ushort*)DepthGenerator.DepthMapPtr;
            for (int y = 0; y < _depthMD.YRes; ++y)
            {
              byte* pDest = (byte*)_rawDepthBitmap.BackBuffer.ToPointer() + y * _rawDepthBitmap.BackBufferStride;
              for (int x = 0; x < _depthMD.XRes; ++x, ++pDepth, ++pDest)
              {
                ushort pixel = (ushort)(maxDepth - (ushort)(*pDepth));
                if (pixel <= (maxDepth - _visualAlertDistance) || (pixel == maxDepth))
                  pixel = 0;
                else
                  pixel = (ushort)(maxDepth - pixel);
                pDest[0] = (byte)(pixel / (_visualAlertDistance / 256));
              }
            }
          }

          _rawDepthBitmap.AddDirtyRect(new Int32Rect(0, 0, _depthMD.XRes, _depthMD.YRes));
          _rawDepthBitmap.Unlock();
        }

        return _rawDepthBitmap;
      }
    }

    #endregion

    #region OpenNI properties

    /// <summary>
    /// OpenNI main Context.
    /// </summary>
    public Context Context { get; private set; }

    /// <summary>
    /// OpenNI image generator.
    /// </summary>
    public ImageGenerator ImageGenerator { get; private set; }

    /// <summary>
    /// OpenNI depth generator.
    /// </summary>
    public DepthGenerator DepthGenerator { get; private set; }

    /// <summary>
    /// OpenNI histogram.
    /// </summary>
    public int[] Histogram { get; private set; }

    #endregion

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of NuiSensor with the default configuration file.
    /// </summary>
    public NuiSensor()
      : this(CONFIGURATION)
    {
    }

    /// <summary>
    /// Creates a new instance of NuiSensor with the specified configuration file.
    /// </summary>
    /// <param name="configuration">Configuration file path.</param>
    public NuiSensor(string configuration)
    {
      InitializeCamera(configuration);
      InitializeBitmaps();
      InitializeThread();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Initializes the image and depth camera.
    /// </summary>
    /// <param name="configuration">Configuration file path.</param>
    private void InitializeCamera(string configuration)
    {
      try
      {
        Context = Context.CreateFromXmlFile(configuration, out scriptNode);
      }
      catch
      {
        throw new Exception("Configuration file not found.");
      }

      ImageGenerator = Context.FindExistingNode(NodeType.Image) as ImageGenerator;
      DepthGenerator = Context.FindExistingNode(NodeType.Depth) as DepthGenerator;
      Histogram = new int[DepthGenerator.DeviceMaxDepth];
    }

    /// <summary>
    /// Initializes the image and depth bitmap sources.
    /// </summary>
    private void InitializeBitmaps()
    {
      MapOutputMode mapMode = DepthGenerator.MapOutputMode;

      int width = (int)mapMode.XRes;
      int height = (int)mapMode.YRes;

      _imageBitmap = new WriteableBitmap(width, height, DPI_X, DPI_Y, PixelFormats.Rgb24, null);
      _depthBitmap = new WriteableBitmap(width, height, DPI_X, DPI_Y, PixelFormats.Rgb24, null);
      _rawDepthBitmap = new WriteableBitmap(width, height, DPI_X, DPI_Y, PixelFormats.Gray8, null);
    }

    /// <summary>
    /// Initializes the background camera thread.
    /// </summary>
    private void InitializeThread()
    {
      _isRunning = true;

      _cameraThread = new Thread(CameraThread);
      _cameraThread.IsBackground = true;
      _cameraThread.Start();
    }

    /// <summary>
    /// Updates image and depth values.
    /// </summary>
    private unsafe void CameraThread()
    {
      while (_isRunning)
      {
        Context.WaitAndUpdateAll();

        ImageGenerator.GetMetaData(_imgMD);
        DepthGenerator.GetMetaData(_depthMD);
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Re-creates the depth histogram.
    /// </summary>
    /// <param name="depthMD"></param>
    public unsafe void UpdateHistogram(DepthMetaData depthMD)
    {
      // Reset.
      for (int i = 0; i < Histogram.Length; ++i)
        Histogram[i] = 0;

      ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

      int points = 0;
      for (int y = 0; y < depthMD.YRes; ++y)
      {
        for (int x = 0; x < depthMD.XRes; ++x, ++pDepth)
        {
          ushort depthVal = *pDepth;
          if (depthVal != 0)
          {
            Histogram[depthVal]++;
            points++;
          }
        }
      }

      for (int i = 1; i < Histogram.Length; i++)
      {
        Histogram[i] += Histogram[i - 1];
      }

      if (points > 0)
      {
        for (int i = 1; i < Histogram.Length; i++)
        {
          Histogram[i] = (int)(256 * (1.0f - (Histogram[i] / (float)points)));
        }
      }
    }

    /// <summary>
    /// Releases any resources.
    /// </summary>
    public void Dispose()
    {
      _imageBitmap = null;
      _depthBitmap = null;
      _rawDepthBitmap = null;
      _isRunning = false;
      _cameraThread.Join();
      Context.Dispose();
      _cameraThread = null;
      Context = null;
    }

    #endregion
  }
}
