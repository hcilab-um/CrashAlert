package ca.umanitoba.cs.hci.phonebumper;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.ArrayList;
import java.util.List;
import java.util.Stack;
import java.util.UUID;

import org.apache.http.util.EncodingUtils;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothSocket;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.os.AsyncTask;

public class NetworkManager extends AsyncTask<String, Integer, Bitmap> {

	public static final String BUMPER_UPDATE = "NetworkManager.BUMPER_UPDATE";
	public static final String NETWORK_EXCEPTION = "NetworkManager.NETWORK_EXCEPTION";

	protected Context context;

	public static Bitmap responseContent;

	private static Stack<String> outgoingMessages = new Stack<String>();
	private static UUID btServiceUUID = null;
	private static String btRemoteServerAddress = null;
	private static BluetoothDevice btRemoteServer = null;
	private static BluetoothAdapter mAdapter;
	private static BluetoothSocket btSocket = null;
	private static InputStream btInputStream = null;
	private static OutputStream btOutputStream = null;
	private static DataInputStream btDataInputStream = null;
	private static DataOutputStream btDataOutputStream = null;

	public NetworkManager(Context iContext, String serverAddress, String serviceUUID) {
		super();
		context = iContext;
		btRemoteServerAddress = serverAddress;
		btServiceUUID = UUID.fromString(serviceUUID);
		mAdapter = BluetoothAdapter.getDefaultAdapter();
	}
	
	private static List<Long> framerate = new ArrayList<Long>();

	@Override
	protected Bitmap doInBackground(String... params) {
		// This is the object containing the result image value
		Bitmap newBumperImage = null;
		long initial = System.currentTimeMillis();

		try {

			if (btSocket == null) {
				btRemoteServer = mAdapter.getRemoteDevice(btRemoteServerAddress);
				btSocket = btRemoteServer.createRfcommSocketToServiceRecord(btServiceUUID);
				btSocket.connect();
				manageConnectedSocket(btSocket);
			}

			if (btInputStream == null || btOutputStream == null)
				throw new RuntimeException("btInputStream == null || btOutputStream == null");

			// 1- Downloads BumperType
			int ibtServer = btDataInputStream.readInt();
			BumperType btServer = BumperType.values()[ibtServer];
			// 2- Downloads InvertSource
			int iisServer = btDataInputStream.readInt();
			InvertSource isServer = InvertSource.values()[iisServer];
			// 3- Downloads changes in the sliding window (height and position-from-top)
			int swHeightServer = btDataInputStream.readInt();
			int swDistanceFromTopServer = btDataInputStream.readInt();
			// 4- Downloads the brightness
			int brightness = btDataInputStream.readInt();
			SettingsActivity.processServerState(btServer, isServer, swHeightServer, swDistanceFromTopServer, brightness);

			// 5- downloads the new image width and height
			int imageWidth = btDataInputStream.readInt();
			int imageHeight = btDataInputStream.readInt();
			// 6- Downloads useCompression but ignores it
			btDataInputStream.readBoolean();
			// 7- Downloads the buffer size
			int bufferSize = btDataInputStream.readInt(); // the image is sent compressed in PNG
			byte[] imageBuffer = new byte[bufferSize];
			// 8- Downloads the buffer
			btDataInputStream.readFully(imageBuffer, 0, bufferSize);
			newBumperImage = BitmapFactory.decodeByteArray(imageBuffer, 0, bufferSize);
			if (newBumperImage.getWidth() != imageWidth || newBumperImage.getHeight() != imageHeight)
				throw new RuntimeException("newBumperImage.getWidth() != imageWidth || newBumperImage.getHeight() != imageHeight");

			// 9- first it sends all the outgoing messages -- stringlenght;string -- when it's the last string stringlenght == -1
			synchronized (outgoingMessages) {
				btDataOutputStream.writeInt(outgoingMessages.size());
				String message = null;
				while ((message = getOutgoing()) != null && message.length() > 0) {
					byte[] messageBytes = EncodingUtils.getAsciiBytes(message);
					btDataOutputStream.writeInt(messageBytes.length);
					btDataOutputStream.write(messageBytes);
				}
				btDataOutputStream.flush();
			}

			// 10- Sends BumperType
			btDataOutputStream.writeInt(SettingsActivity.getBumperType().ordinal());
			// 11- Sends InvertImage
			btDataOutputStream.writeInt(SettingsActivity.getInvertSource().ordinal());
			// 12- Sends changes in the sliding window (height and position-from-top)
			btDataOutputStream.writeInt(SettingsActivity.getBumperHeight());
			btDataOutputStream.writeInt(SettingsActivity.getBumperDistanceFromTop());
			// 13- Sends the brightness
			btDataOutputStream.writeInt(SettingsActivity.getBrightness());
		} catch (Exception exception) {
			newBumperImage = null;
			finishUp();
			exception.printStackTrace();
		}
		
		long elapsed = System.currentTimeMillis() - initial;
		framerate.add(elapsed);
		return newBumperImage;
	}

	@Override
	protected void onPostExecute(Bitmap result) {
		responseContent = result;
		Intent intent = new Intent(NetworkManager.BUMPER_UPDATE);
		intent.putExtra(NetworkManager.NETWORK_EXCEPTION, responseContent == null);
		context.sendBroadcast(intent);
	}

	public static void setOutgoing(String message) {
		synchronized (outgoingMessages) {
			outgoingMessages.push(message);
		}
	}

	private String getOutgoing() {
		String message = "";
		if (!outgoingMessages.isEmpty())
			message = outgoingMessages.pop();
		return message;
	}

	private void manageConnectedSocket(BluetoothSocket socket) throws IOException {
		btSocket = socket;

		btInputStream = btSocket.getInputStream();
		btOutputStream = btSocket.getOutputStream();
		btDataInputStream = new DataInputStream(btInputStream);
		btDataOutputStream = new DataOutputStream(btOutputStream);
	}

	public static void finishUp() {
		try {
			if (btDataInputStream != null)
				btDataInputStream.close();
			if (btInputStream != null)
				btInputStream.close();
			if (btDataOutputStream != null)
				btDataOutputStream.close();
			if (btOutputStream != null)
				btOutputStream.close();
			if (btSocket != null)
				btSocket.close();
		} catch (Exception e) {
		} finally {			
			btDataInputStream = null;
			btInputStream = null;
			btDataOutputStream = null;
			btOutputStream = null;
			btSocket = null;
		}
	}

	public static boolean isConnected() {
		if (btSocket == null)
			return false;
		return true;
	}

}
