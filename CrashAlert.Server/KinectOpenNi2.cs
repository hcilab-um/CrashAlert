using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenNIWrapper;

namespace CrashAlert.Server
{
  public class KinectOpenNi2 : INuiSource
  {

    private Object colorMutex = new Object();
    private System.Windows.Media.Imaging.WriteableBitmap colorBitmap;
    private VideoFrameRef colorFrame;

    private Object depthMutex = new Object();
    private System.Windows.Media.Imaging.WriteableBitmap depthBitmap;
    private VideoFrameRef depthFrame;

    private Device kinectDevice;
    private VideoStream depthSensor;
    private VideoStream colorSensor;

    public KinectOpenNi2()
    {
      HandleError(OpenNI.Initialize());

      DeviceInfo[] devices = OpenNI.EnumerateDevices();
      if (devices.Length == 0)
        HandleError(OpenNI.Status.NO_DEVICE);

      kinectDevice = devices[0].OpenDevice();

      colorSensor = kinectDevice.CreateVideoStream(Device.SensorType.COLOR);
      VideoMode[] videoModes = colorSensor.SensorInfo.getSupportedVideoModes();
      colorSensor.VideoMode = videoModes[1];
      colorSensor.Start();
      colorSensor.onNewFrame += new VideoStream.VideoStreamNewFrame(colorSensor_onNewFrame);

      depthSensor = kinectDevice.CreateVideoStream(Device.SensorType.DEPTH);
      videoModes = depthSensor.SensorInfo.getSupportedVideoModes();
      depthSensor.VideoMode = videoModes[0];
      depthSensor.Start();
      depthSensor.onNewFrame += new VideoStream.VideoStreamNewFrame(depthSensor_onNewFrame);
    }

    void colorSensor_onNewFrame(VideoStream vStream)
    {
      if (!vStream.isValid || !vStream.isFrameAvailable())
        return;

      VideoFrameRef frame = vStream.readFrame();
      if (!frame.isValid)
        return;

      lock (colorMutex)
      {
        colorFrame = frame;
      }
    }

    public System.Windows.Media.ImageSource RawImageSource
    {
      get
      {
        lock (colorMutex)
        {
          if (colorFrame == null || !colorFrame.isValid)
            return null;

          var width = colorFrame.FrameSize.Width;
          var height = colorFrame.FrameSize.Height;
          if (colorBitmap == null)
            colorBitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);

          colorBitmap.Lock();
          colorBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), colorFrame.Data, colorFrame.DataSize, colorFrame.DataStrideBytes);
          colorBitmap.Unlock();

          colorFrame.Dispose();
          return colorBitmap;
        }
      }
    }

    void depthSensor_onNewFrame(VideoStream vStream)
    {
      if (!vStream.isValid || !vStream.isFrameAvailable())
        return;

      VideoFrameRef frame = vStream.readFrame();
      if (!frame.isValid) 
        return;

      lock (depthMutex)
      {
        depthFrame = frame;
      }
    }

    public System.Windows.Media.ImageSource DepthImageSource
    {
      get
      {
        lock (depthMutex)
        {
          if (depthFrame == null || !depthFrame.isValid)
            return null;

          var width = depthFrame.FrameSize.Width;
          var height = depthFrame.FrameSize.Height;
          if (depthBitmap == null)
            depthBitmap = new System.Windows.Media.Imaging.WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);

          depthBitmap.Lock();
          VideoFrameRef.copyBitmapOptions options24 = VideoFrameRef.copyBitmapOptions.RawDepth24BitRGB;
          Bitmap bitmap24 = depthFrame.toBitmap(options24);
          System.Drawing.Imaging.BitmapData data24 = bitmap24.LockBits(new Rectangle(0, 0, bitmap24.Width, bitmap24.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
          try
          {
            depthBitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), data24.Scan0, width * height * 3, data24.Stride);
          }
          finally
          {
            bitmap24.UnlockBits(data24);
          }
          bitmap24.Dispose();
          depthBitmap.Unlock();
          

          depthFrame.Dispose();
          return depthBitmap;
        }
      }
    }

    private bool HandleError(OpenNI.Status status)
    {
      if (status == OpenNI.Status.OK)
        return true;
      System.Windows.Forms.MessageBox.Show("Error: " + status.ToString() + " - " + OpenNI.LastError, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Asterisk);
      return false;
    }

    public int VisualAlertDistance { get; set; }

    public void Dispose()
    {
      OpenNI.Shutdown();
    }
  }
}
