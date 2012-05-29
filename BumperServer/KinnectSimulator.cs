using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectWPF;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Media;
using System.Windows;
using System.Runtime.InteropServices;

namespace BumperServer
{

  public class KinnectSimulator : INuiSource
  {

    private WriteableBitmap _rawImage = null;
    private WriteableBitmap _depthImage = null;

    public KinnectSimulator()
    {
    }

    public ImageSource RawImageSource
    {
      get
      {
        if (_rawImage == null)
        {
          Bitmap noiseImage = GenerateNoise(640, 480);
          IntPtr ptrCB = noiseImage.GetHbitmap();
          BitmapSource bsCB = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ptrCB,
            IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
          _rawImage = new WriteableBitmap(bsCB);
          DeleteObject(ptrCB);
        }
        else
        {
          Random r = new Random();
          _rawImage.Lock();
          unsafe
          {
            ushort* pDepth = (ushort*)_rawImage.BackBuffer;
            for (int y = 0; y < _rawImage.Height; ++y)
            {
              byte* pDest = (byte*)_rawImage.BackBuffer.ToPointer() + y * _rawImage.BackBufferStride;
              for (int x = 0; x < _rawImage.Width; ++x, ++pDepth, pDest += 3)
              {
                byte pixel = (byte)r.Next(0, 256);
                pDest[0] = pixel;
                pDest[1] = pixel;
                pDest[2] = pixel;
              }
            }
          }
          _rawImage.Unlock();
        }
        return _rawImage;
      }
    }

    public ImageSource DepthImageSource
    {
      get
      {
        if (_depthImage == null)
        {
          Bitmap noiseImage = GenerateNoise(640, 480);
          IntPtr ptrCB = noiseImage.GetHbitmap();
          BitmapSource bsCB = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ptrCB,
            IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
          _depthImage = new WriteableBitmap(bsCB);
          DeleteObject(ptrCB);
        }
        else
        {
          Random r = new Random();
          _depthImage.Lock();
          unsafe
          {
            ushort* pDepth = (ushort*)_depthImage.BackBuffer;
            for (int y = 0; y < _depthImage.Height; ++y)
            {
              byte* pDest = (byte*)_depthImage.BackBuffer.ToPointer() + y * _depthImage.BackBufferStride;
              for (int x = 0; x < _depthImage.Width; ++x, ++pDepth, pDest += 3)
              {
                byte pixel = (byte)r.Next(0, 256);
                pDest[0] = pixel;
                pDest[1] = 0;
                pDest[2] = 0;
              }
            }
          }
          _depthImage.Unlock();
        }
        return _depthImage;
      }
    }

    public ImageSource RawDepthImageSource 
    {
      get
      {
        return DepthImageSource;
      }
    }

    public void Dispose()
    {
    }

    public Bitmap GenerateNoise(int width, int height)
    {
      Bitmap finalBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
      Random r = new Random();

      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          int num = r.Next(0, 256);
          finalBmp.SetPixel(x, y, System.Drawing.Color.FromArgb(255, num, num, num));
        }
      }
      return finalBmp;
    }

    [DllImport("gdi32")]
    private static extern int DeleteObject(IntPtr o);

    public int VisualAlertDistance
    {
      get;
      set;
    }
  }

}
