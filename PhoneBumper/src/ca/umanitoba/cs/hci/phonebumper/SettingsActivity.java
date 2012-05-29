package ca.umanitoba.cs.hci.phonebumper;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.graphics.drawable.Drawable;
import android.os.Bundle;
import android.view.View;
import android.widget.SeekBar;
import android.widget.SeekBar.OnSeekBarChangeListener;
import android.widget.TextView;

public class SettingsActivity extends Activity implements OnSeekBarChangeListener {

	// These are the local settings
	private static BumperType bumperType = BumperType.Color;
	private static InvertSource invertSource = InvertSource.Inverted;
	private static int bumperHeight = 150;
	private static int bumperDistanceFromTop = 165;
	private static int brightness = 1;

	// These are the desktop settings
	private static BumperType sBumperType = BumperType.None;
	private static InvertSource sInvertSource = InvertSource.None;
	private static int sBumperHeight = -1;
	private static int sBumperDistanceFromTop = -1;
	private static int sBrightness = -1;

	// These are the non-shared settings
	private static ScreenPosition screenPosition = ScreenPosition.Top;

	// These are references to the UI components
	private TextView tvBumperType = null;
	private TextView tvScreenPosition = null;
	private TextView tvInvertImage = null;
	private SlidingWindowManager swManager = null;
	private TextView tvBumperDistanceFromTop = null;
	private TextView tvBumperHeight = null;
	private SeekBar sbBrightness = null;

	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.settings);

		// Registers receiver for change in the bumperType from the selector
		registerReceiver(bumperTypeChangedReceiver, new IntentFilter(SelectBumperImageActivity.INTENT_BUMPER_TYPE_CHANGED));
		// Registers receiver for alerts on changes in the state variables so that the UI is updated accordingly
		registerReceiver(networkReceiver, new IntentFilter(NetworkManager.BUMPER_UPDATE));

		// Find the UI components
		tvBumperType = (TextView) findViewById(R.id.tvBumperType);
		tvScreenPosition = (TextView) findViewById(R.id.tvScreenPosition);
		tvInvertImage = (TextView) findViewById(R.id.tvInvertImage);
		swManager = (SlidingWindowManager) findViewById(R.id.slmManager);
		tvBumperDistanceFromTop = (TextView) findViewById(R.id.tvBumperDistanceFromTop);
		tvBumperHeight = (TextView) findViewById(R.id.tvBumperHeight);
		sbBrightness = (SeekBar) findViewById(R.id.sbBrightness);

		// Attach listener
		sbBrightness.setOnSeekBarChangeListener(this);
		
		// Fills the current values
		tvBumperType.setCompoundDrawablesWithIntrinsicBounds(null, null, getDrawable(this, bumperType), null);
		tvScreenPosition.setText(screenPosition.toString());
		tvInvertImage.setText(invertSource.toString());
		sbBrightness.setProgress(brightness);
	}

	@Override
	public void onDestroy() {
		super.onDestroy();
		unregisterReceiver(bumperTypeChangedReceiver);
		unregisterReceiver(networkReceiver);
	}

	public void tvBumperTypeClicked(View view) {
		Intent intent = new Intent(this, SelectBumperImageActivity.class);
		intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
		startActivity(intent);
	}

	public void tvScreenPositionClicked(View view) {
		int currentIndex = screenPosition.ordinal();
		++currentIndex;
		screenPosition = ScreenPosition.values()[currentIndex % ScreenPosition.values().length];

		tvScreenPosition.setText(screenPosition.toString());
	}

	public void tvInvertImageClicked(View view) {
		if (invertSource == InvertSource.Inverted)
			invertSource = InvertSource.NotInverted;
		else
			invertSource = InvertSource.Inverted;

		tvInvertImage.setText(invertSource.toString());
	}

	private boolean isChangingBrightness = false;

	public void onProgressChanged(SeekBar seekBar, int progress, boolean fromUser) {
	}

	public void onStartTrackingTouch(SeekBar seekBar) {
		isChangingBrightness = true;
	}

	public void onStopTrackingTouch(SeekBar seekBar) {
		brightness = seekBar.getProgress();
		isChangingBrightness = false;
	}

	/**
	 * Updates the value of the bumper so that it gets sent over to the server
	 */
	private BroadcastReceiver bumperTypeChangedReceiver = new BroadcastReceiver() {
		@Override
		public void onReceive(final Context context, final Intent intent) {
			String iAction = intent.getAction();
			if (iAction.equals(SelectBumperImageActivity.INTENT_BUMPER_TYPE_CHANGED)) {
				int bumperTypeOrdinal = intent.getIntExtra(BumperType.class.toString(), BumperType.None.ordinal());
				bumperType = BumperType.values()[bumperTypeOrdinal];
				tvBumperType.setCompoundDrawablesWithIntrinsicBounds(null, null, getDrawable(context, bumperType), null);
			}
		}
	};

	/**
	 * This one happens in the UI thread -- thus updating the text on the screen
	 */
	private final BroadcastReceiver networkReceiver = new BroadcastReceiver() {
		@Override
		public void onReceive(Context context, Intent intent) {
			tvBumperType.setCompoundDrawablesWithIntrinsicBounds(null, null, getDrawable(context, bumperType), null);
			tvScreenPosition.setText(screenPosition.toString());
			tvInvertImage.setText(invertSource.toString());
			tvBumperDistanceFromTop.setText(bumperDistanceFromTop + "");
			tvBumperHeight.setText(bumperHeight + "");

			if (!isChangingBrightness)
				sbBrightness.setProgress(brightness);
			if (NetworkManager.responseContent != null)
				swManager.setBumperImage(NetworkManager.responseContent);
		}
	};

	public static Drawable getDrawable(Context context, BumperType btDrawable) {
		Drawable rValue = null;
		switch (btDrawable) {
		case Depth:
			rValue = context.getResources().getDrawable(R.drawable.bumper_depth);
			break;
		case AvgHighDepthColumns:
			rValue = context.getResources().getDrawable(R.drawable.bumper_avg_high_depth_columns);
			break;
		case MaskOnColor:
			rValue = context.getResources().getDrawable(R.drawable.bumper_mask_on_color);
			break;
		case DepthMaskOnColor:
			rValue = context.getResources().getDrawable(R.drawable.bumper_depth_mask_on_color);
			break;
		case Color:
			rValue = context.getResources().getDrawable(R.drawable.bumper_color);
			break;
		case Black:
		default:
			rValue = context.getResources().getDrawable(R.drawable.bumper_black);
			break;
		}
		return rValue;
	}

	public static BumperType getBumperType() {
		return bumperType;
	}

	public static void setBumperType(BumperType bumperType) {
		SettingsActivity.bumperType = bumperType;
	}

	public static InvertSource getInvertSource() {
		return invertSource;
	}

	public static void setInvertSource(InvertSource invertSource) {
		SettingsActivity.invertSource = invertSource;
	}

	public static int getBumperHeight() {
		return bumperHeight;
	}

	public static void setBumperHeight(int slidingWindowHeight) {
		SettingsActivity.bumperHeight = slidingWindowHeight;
	}

	public static int getBumperDistanceFromTop() {
		return bumperDistanceFromTop;
	}

	public static void setBumperDistanceFromTop(int slidingWindowDistanceFromTop) {
		SettingsActivity.bumperDistanceFromTop = slidingWindowDistanceFromTop;
	}

	public static int getBrightness() {
		return brightness;
	}

	public static void setBrightness(int brightness) {
		SettingsActivity.brightness = brightness;
	}

	/**
	 * The server always has prevalence over the local settings --> thus, if new values come down from the server, they override the local values.
	 * 
	 * @param btServer
	 * @param isServer
	 * @param bumperHeightServer
	 * @param bumperDistanceFromTopServer
	 */
	public static void processServerState(BumperType btServer, InvertSource isServer, int bumperHeightServer, int bumperDistanceFromTopServer,
			int brightnessServer) {
		if (sBumperType != btServer)
			bumperType = btServer;
		sBumperType = btServer;

		if (sInvertSource != isServer)
			invertSource = isServer;
		sInvertSource = isServer;

		if (sBumperHeight != bumperHeightServer)
			bumperHeight = bumperHeightServer;
		sBumperHeight = bumperHeightServer;

		if (sBumperDistanceFromTop != bumperDistanceFromTopServer)
			bumperDistanceFromTop = bumperDistanceFromTopServer;
		sBumperDistanceFromTop = bumperDistanceFromTopServer;

		if (sBrightness != brightnessServer)
			brightness = brightnessServer;
		sBrightness = brightnessServer;
	}

}
