package ca.umanitoba.cs.hci.phonebumper;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.os.Bundle;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.TextView;

public class PhoneBumperActivity extends Activity {

	private static final int MENU_SETTINGS = 0;
	private static final int MENU_CONNECT = 1;

	private boolean isConnected = false;
	
	private Bumper ivBumper = null;
	private Button bStart = null;
	private TextView tvBombCount = null;
	private TextView tvExplosionCount = null;

	private int bombCount = -1;
	private int explosionCount = -1;
	private long initMilliseconds = 0;
	private static boolean isStarted = false;

	public static final long TIME_BETWEEN_BOMB_PLANTINGS = 500;
	public static final long MAX_BOMB_EXPLOSION_TIME = 2500;
	public static final long MIN_BOMB_EXPLOSION_TIME = 2500;
	private BombPlanter bombPlanter = null;
	private ExploitingBomb[] bombs = null;

	public static boolean isStarted() {
		return isStarted;
	}

	@Override
	public void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		requestWindowFeature(Window.FEATURE_NO_TITLE);
		getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
		setContentView(R.layout.main);

		ivBumper = (Bumper) findViewById(R.id.iBumper);
		bStart = (Button) findViewById(R.id.bStart);
		tvBombCount = (TextView) findViewById(R.id.tvBombCount);
		tvExplosionCount = (TextView) findViewById(R.id.tvExplosionCount);

		bombs = new ExploitingBomb[16];
		bombs[0] = (ExploitingBomb) findViewById(R.id.bomb1);
		bombs[1] = (ExploitingBomb) findViewById(R.id.bomb2);
		bombs[2] = (ExploitingBomb) findViewById(R.id.bomb3);
		bombs[3] = (ExploitingBomb) findViewById(R.id.bomb4);
		bombs[4] = (ExploitingBomb) findViewById(R.id.bomb5);
		bombs[5] = (ExploitingBomb) findViewById(R.id.bomb6);
		bombs[6] = (ExploitingBomb) findViewById(R.id.bomb7);
		bombs[7] = (ExploitingBomb) findViewById(R.id.bomb8);
		bombs[8] = (ExploitingBomb) findViewById(R.id.bomb9);
		bombs[9] = (ExploitingBomb) findViewById(R.id.bomb10);
		bombs[10] = (ExploitingBomb) findViewById(R.id.bomb11);
		bombs[11] = (ExploitingBomb) findViewById(R.id.bomb12);
		bombs[12] = (ExploitingBomb) findViewById(R.id.bomb13);
		bombs[13] = (ExploitingBomb) findViewById(R.id.bomb14);
		bombs[14] = (ExploitingBomb) findViewById(R.id.bomb15);
		bombs[15] = (ExploitingBomb) findViewById(R.id.bomb16);

		registerReceiver(bombReceiver, new IntentFilter(ExploitingBomb.BOMB_EXPLOSION));
		registerReceiver(bombReceiver, new IntentFilter(ExploitingBomb.BOMB_DIFUSED));
		registerReceiver(bombPlantedReceiver, new IntentFilter(BombPlanter.BOMB_PLANTED));
		registerReceiver(bumperTypeChangedReceiver, new IntentFilter(SelectBumperImageActivity.INTENT_BUMPER_TYPE_CHANGED));
		registerReceiver(networkReceiver, new IntentFilter(NetworkManager.BUMPER_UPDATE));
	}

	@Override
	public void onDestroy() {
		super.onDestroy();
		unregisterReceiver(networkReceiver);
		unregisterReceiver(bumperTypeChangedReceiver);
		unregisterReceiver(bombPlantedReceiver);
		unregisterReceiver(bombReceiver);
		NetworkManager.finishUp();
	}

	private MenuItem menuConnect = null;
	
	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		menu.add(Menu.NONE, MENU_SETTINGS, Menu.NONE, R.string.menu_settings);
		menuConnect = menu.add(Menu.NONE, MENU_CONNECT, Menu.NONE, R.string.menu_connect);
		return true;
	}
	
	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		if (item.getItemId() == MENU_SETTINGS) {
			Intent intent = new Intent(this, SettingsActivity.class);
			intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
			startActivity(intent);
			return true;
		} else if (item.getItemId() == MENU_CONNECT) {
			if (!NetworkManager.isConnected()) {
				isConnected = true;
				NetworkManager downloader = new NetworkManager(this, getString(R.string.remote_connection_data), getString(R.string.remote_service_uuid));
				downloader.execute("");
				item.setTitle(R.string.menu_disconnect);
			} else {
				isConnected = false;
				NetworkManager.finishUp();
				item.setTitle(R.string.menu_connect);
			}
			return true;
		}
		return false;
	}

	public void bStartClicked(View view) {
		if (!isStarted) {
			bombCount = 0;
			tvBombCount.setText(bombCount + "");
			explosionCount = 0;
			tvExplosionCount.setText(explosionCount + "");

			isStarted = true;
			initMilliseconds = System.currentTimeMillis();
			bStart.setText(R.string.start_button_stop);

			for (ExploitingBomb bomb : bombs)
				bomb.cancelBomb();

			bombPlanter = new BombPlanter(bombs);
			bombPlanter.execute(this);
		} else {
			isStarted = false;
			bStart.setText(R.string.start_button_start);

			for (ExploitingBomb bomb : bombs)
				bomb.cancelBomb();

			long elapsedTime = System.currentTimeMillis() - initMilliseconds;
			NetworkManager.setOutgoing(elapsedTime + ";" + bombCount + ";" + explosionCount);
		}
	}

	private BroadcastReceiver bombPlantedReceiver = new BroadcastReceiver() {
		@Override
		public void onReceive(Context context, Intent intent) {
			if (!isStarted)
				return;

			bombPlanter = new BombPlanter(bombs);
			bombPlanter.execute(context);
		}
	};

	/**
	 * Updates the value of the bumper so that it gets sent over to the server
	 */
	private BroadcastReceiver bumperTypeChangedReceiver = new BroadcastReceiver() {
		@Override
		public void onReceive(final Context context, final Intent intent) {
			String iAction = intent.getAction();
			if (iAction.equals(SelectBumperImageActivity.INTENT_BUMPER_TYPE_CHANGED)) {
				int bumperTypeOrdinal = intent.getIntExtra(BumperType.class.toString(), BumperType.None.ordinal());
				BumperType bumperType = BumperType.values()[bumperTypeOrdinal];
				ivBumper.setImageBitmap(SettingsActivity.getDrawable(context, bumperType));
			}
		}
	};

	private final BroadcastReceiver networkReceiver = new BroadcastReceiver() {
		@Override
		public void onReceive(Context context, Intent intent) {
			if (NetworkManager.responseContent != null)
				ivBumper.setImageBitmap(NetworkManager.responseContent);
			
			boolean netException = intent.getBooleanExtra(NetworkManager.NETWORK_EXCEPTION, false);
			isConnected = !netException;
			
			if (isConnected) {
				NetworkManager downloader = new NetworkManager(context, getString(R.string.remote_connection_data), getString(R.string.remote_service_uuid));
				downloader.execute("");
			}
			else
			{
				menuConnect.setTitle(R.string.menu_connect);
			}
		}
	};

	private final BroadcastReceiver bombReceiver = new BroadcastReceiver() {
		@Override
		public void onReceive(Context context, Intent intent) {
			if (!isStarted)
				return;

			if (ExploitingBomb.BOMB_DIFUSED == intent.getAction()) {
				bombCount++;
				tvBombCount.setText(bombCount + "");
			} else if (ExploitingBomb.BOMB_EXPLOSION == intent.getAction()) {
				int bombIndex = intent.getIntExtra(ExploitingBomb.BOMB_INDEX, -1);
				ExploitingBomb eventBomb = bombs[bombIndex];
				eventBomb.resetBomb(PhoneBumperActivity.TIME_BETWEEN_BOMB_PLANTINGS);

				explosionCount++;
				tvExplosionCount.setText(explosionCount + "");
			}
		}
	};

}