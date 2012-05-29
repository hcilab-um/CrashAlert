using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using System.IO;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing.Imaging;
using BumperServer.Properties;
using System.Threading;
using System.ComponentModel;

namespace BumperServer
{

  public class NetworkManager
  {

    private const int RECONNECTION_ATTEMPTS = 3;

    private BluetoothClient client = null;
    private BluetoothListener serverSocket = null;
    private BinaryWriter bWriter = null;
    private BinaryReader bReader = null;

    private ImageCodecInfo jpgEncoder = null;
    private System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
    private EncoderParameters myEncoderParameters = null;

    private MainWindow mwObject = null;

    public Boolean IsConnected
    {
      get
      {
        if (client == null)
          return false;
        return client.Connected;
      }
    }

    public NetworkManager(Guid applicationGuid, MainWindow mwObject)
    {
      serverSocket = new BluetoothListener(applicationGuid);
      serverSocket.Start();

      jpgEncoder = GetEncoder(ImageFormat.Jpeg);
      myEncoderParameters = new EncoderParameters(1);
      EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, Settings.Default.CompressionLevel);
      myEncoderParameters.Param[0] = myEncoderParameter;

      serverWorker = new BackgroundWorker();
      serverWorker.DoWork += new DoWorkEventHandler(ServerWorker_DoWork);
      serverWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ServerWorker_RunWorkerCompleted);

      this.mwObject = mwObject;
    }

    public void Init()
    {
      isRunning = true;
      serverWorker.RunWorkerAsync();
    }

    void ServerWorker_DoWork(object sender, DoWorkEventArgs e)
    {
      try
      {
        client = serverSocket.AcceptBluetoothClient();
        Stream peerStream = client.GetStream();
        bWriter = new BinaryWriter(peerStream, Encoding.ASCII);
        bReader = new BinaryReader(peerStream, Encoding.ASCII);
        e.Result = true;
      }
      catch (Exception exception)
      {
        e.Result = false;
        Console.WriteLine(exception.Message);
      }
    }

    void ServerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      if (isRunning)
        serverWorker.RunWorkerAsync();
    }

    public void SendState()
    {
      //// 1- Sends BumperType
      WriteInt((int)mwObject.BumperSelected);
      //// 2- Sends InvertSource
      if (mwObject.VerticalFlip)
        WriteInt(1);
      else
        WriteInt(0);
      //// 3- Sends changes in the sliding window (height and position-from-top)
      WriteInt(mwObject.BumperHeight);
      WriteInt(mwObject.BumperDistanceFromTop);
      //// 4- Sends changes in brightness
      WriteInt(mwObject.Brightness);
    }

    public void SendImage(IImage imageToSend, bool useCompression)
    {
      if (imageToSend == null)
        return;

      //// 5- Sends the new image width and height
      WriteInt(imageToSend.Size.Width);
      WriteInt(imageToSend.Size.Height);

      //// 6- Sends useCompression
      bWriter.Write(useCompression);

      MemoryStream memStream = new MemoryStream();
      if (useCompression)
        imageToSend.Bitmap.Save(memStream, jpgEncoder, myEncoderParameters);
      else
        imageToSend.Bitmap.Save(memStream, ImageFormat.Bmp);

      //// 7- Sends the buffer size
      byte[] imageBuffer = memStream.GetBuffer();
      int bufferLength = (int)memStream.Position;
      WriteInt(bufferLength);

      //// 8- Sends the buffer
      bWriter.Write(imageBuffer, 0, bufferLength);
      bWriter.Flush();
    }

    public List<String> ReadLog()
    {
      //// 9- Downloads the count of incoming messages, then each one of them -- stringlenght;string
      List<String> remoteLogs = new List<String>();
      int nroOfLogs = ReadInt();
      for (int count = 0; count < nroOfLogs; count++)
      {
        int logLenght = ReadInt();
        byte[] logchars = bReader.ReadBytes(logLenght);
        System.Text.ASCIIEncoding ascii = new ASCIIEncoding();
        remoteLogs.Add(ascii.GetString(logchars, 0, logLenght));
      }
      return remoteLogs;
    }

    public void ReadPhoneState()
    {
      //// 10- Downloads BumperType
      int iBumperType = ReadInt();
      BumperType remoteBumperType = (BumperType)iBumperType;

      //// 11- Downloads InvertImage
      int iVerticalFlip = ReadInt();
      bool remoteVerticalFlip = true;
      if (iVerticalFlip == 0)
        remoteVerticalFlip = false;

      //// 12- Downloads changes in the sliding window (height and position-from-top)
      int remoteBumperHeight = ReadInt();
      int remoteBumperDistanceFromTop = ReadInt();

      //// 13- Downloads changes in brightness
      int brightness = ReadInt();

      mwObject.ProcessPhoneState(remoteBumperType, remoteVerticalFlip, remoteBumperHeight, remoteBumperDistanceFromTop, brightness);
    }

    /// <summary>
    /// BitEndian/LittleEndian incompatibility for Win -vs- Android
    /// </summary>
    /// <param name="intValue"></param>
    private void WriteInt(int intValue)
    {
      byte[] intValueBytes = BitConverter.GetBytes(intValue);
      bWriter.Write(intValueBytes[3]);
      bWriter.Write(intValueBytes[2]);
      bWriter.Write(intValueBytes[1]);
      bWriter.Write(intValueBytes[0]);
      bWriter.Flush();
    }

    private int ReadInt()
    {
      byte[] intValueButes = new byte[4];
      intValueButes[3] = bReader.ReadByte();
      intValueButes[2] = bReader.ReadByte();
      intValueButes[1] = bReader.ReadByte();
      intValueButes[0] = bReader.ReadByte();
      return BitConverter.ToInt32(intValueButes, 0);
    }

    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
      ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
      foreach (ImageCodecInfo codec in codecs)
      {
        if (codec.FormatID == format.Guid)
          return codec;
      }
      return null;
    }

    private bool isRunning = false;
    private BackgroundWorker serverWorker = null;
    internal void Dispose()
    {
      try
      {
        isRunning = false;
        serverSocket.Stop();
      }
      catch (Exception exception)
      { }
    }
  }

}
