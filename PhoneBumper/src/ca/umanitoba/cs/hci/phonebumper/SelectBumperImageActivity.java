package ca.umanitoba.cs.hci.phonebumper;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.graphics.Color;
import android.os.Bundle;
import android.view.View;
import android.widget.ImageView;
import android.widget.TextView;

public class SelectBumperImageActivity extends Activity
{

	public final static String INTENT_BUMPER_TYPE_CHANGED = "SelectBumperImageActivity.INTENT_BUMPER_TYPE_CHANGED";

	private final int TEXT_NORMAL_COLOR = Color.WHITE;
	private final int TEXT_SELECTED_COLOR = Color.RED;

	private TextView tvBlack = null;
	private TextView tvColor = null;
	private TextView tvDepth = null;
	private TextView tvAvgHighDepthColumns = null;
	private TextView tvDepthMaskOnColor = null;
	private TextView tvMaskOnColor = null;

	@Override
	public void onCreate(Bundle savedInstanceState)
	{
		super.onCreate(savedInstanceState);
		setContentView(R.layout.bumper_images);

		// Registers receiver for alerts on changes in the state variables so that the UI is updated accordingly
		registerReceiver(networkReceiver, new IntentFilter(NetworkManager.BUMPER_UPDATE));

		// Gets the UI components
		tvBlack = (TextView) findViewById(R.id.tvBlack);
		tvColor = (TextView) findViewById(R.id.tvColor);
		tvDepth = (TextView) findViewById(R.id.tvDepth);
		tvAvgHighDepthColumns = (TextView) findViewById(R.id.tvAvgHighDepthColumns);
		tvDepthMaskOnColor = (TextView)findViewById(R.id.tvDepthMaskOnColor);
		tvMaskOnColor = (TextView) findViewById(R.id.tvMaskOnColor);

		// Highlights the selected bumper
		setSelectedBumper(SettingsActivity.getBumperType(), false);
	}

	@Override
	public void onDestroy()
	{
		super.onDestroy();
		unregisterReceiver(networkReceiver);
	}

	public void ivBumperChanged(View view)
	{
		ImageView selectedImage = (ImageView) view;
		String imageContentDescription = (String) selectedImage.getContentDescription();

		if (imageContentDescription.equals(tvBlack.getText()))
			setSelectedBumper(BumperType.Black, true);
		else if (imageContentDescription.equals(tvColor.getText()))
			setSelectedBumper(BumperType.Color, true);
		else if (imageContentDescription.equals(tvDepth.getText()))
			setSelectedBumper(BumperType.Depth, true);
		else if (imageContentDescription.equals(tvAvgHighDepthColumns.getText()))
			setSelectedBumper(BumperType.AvgHighDepthColumns, true);
		else if(imageContentDescription.equals(tvDepthMaskOnColor.getText()))
			setSelectedBumper(BumperType.DepthMaskOnColor, true);
		else if (imageContentDescription.equals(tvMaskOnColor.getText()))
			setSelectedBumper(BumperType.MaskOnColor, true);
	}

	private void setSelectedBumper(BumperType bumperType, boolean sendIntent)
	{
		// Gets the UI components
		tvBlack.setTextColor(TEXT_NORMAL_COLOR);
		tvColor.setTextColor(TEXT_NORMAL_COLOR);
		tvDepth.setTextColor(TEXT_NORMAL_COLOR);
		tvAvgHighDepthColumns.setTextColor(TEXT_NORMAL_COLOR);
		tvDepthMaskOnColor.setTextColor(TEXT_NORMAL_COLOR);
		tvMaskOnColor.setTextColor(TEXT_NORMAL_COLOR);

		// Highlights the selected bumper
		TextView tvSelected = getSelectedTextView(bumperType);
		tvSelected.setTextColor(TEXT_SELECTED_COLOR);

		if (sendIntent)
		{
			// Fires the BumperTyeChanged intent
			Intent bumberTypeChanged = new Intent(INTENT_BUMPER_TYPE_CHANGED);
			bumberTypeChanged.putExtra(BumperType.class.toString(), bumperType.ordinal());
			sendBroadcast(bumberTypeChanged);
		}
	}

	private TextView getSelectedTextView(BumperType bumperType)
	{
		TextView rValue = null;
		switch (bumperType)
		{
			case Depth:
				rValue = tvDepth;
				break;
			case DepthMaskOnColor:
				rValue = tvDepthMaskOnColor;
				break;
			case AvgHighDepthColumns:
				rValue = tvAvgHighDepthColumns;
				break;
			case MaskOnColor:
				rValue = tvMaskOnColor;
				break;
			case Color:
				rValue = tvColor;
				break;
			case Black:
			default:
				rValue = tvBlack;
				break;
		}
		return rValue;
	}

	private final BroadcastReceiver networkReceiver = new BroadcastReceiver()
	{
		@Override
		public void onReceive(Context context, Intent intent)
		{
			// Highlights the selected bumper
			setSelectedBumper(SettingsActivity.getBumperType(), false);
		}
	};

}
