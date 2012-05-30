package ca.umanitoba.cs.hci.phonebumper;

import java.util.Random;

import android.content.Context;
import android.content.Intent;
import android.os.AsyncTask;

public class BombPlanter extends AsyncTask<Context, Integer, Boolean>{

	public static final String BOMB_PLANTED = "BombPlanter.BOMB_PLANTED";
	
	private Context context = null;
	
	private ExploitingBomb[] bombs = null;
	private int bombIndex = -1;
	private long explosionTime = -1;
	private Random rndGenerator = null;
	
	public BombPlanter(ExploitingBomb[] bombs)
	{
		this.bombs = bombs;
		this.rndGenerator = new Random(System.currentTimeMillis());
	}
	
	@Override
	protected void onPostExecute(Boolean isGameRunning)
	{
		if(!isGameRunning)
			return;
		
		bombs[bombIndex].activateBomb(bombIndex, explosionTime);
		context.sendBroadcast(new Intent(BombPlanter.BOMB_PLANTED));
	}

	@Override
	protected Boolean doInBackground(Context... params) {
		this.context = params[0];
		
		try {
			Thread.sleep(PhoneBumperActivity.TIME_BETWEEN_BOMB_PLANTINGS);
			if(!PhoneBumperActivity.isStarted())
				return false;
		} catch (InterruptedException e) {}
		
		bombIndex = Math.abs(rndGenerator.nextInt()) % bombs.length;
		while (true) {
			if (!bombs[bombIndex].isActive())
				break;
			bombIndex = Math.abs(rndGenerator.nextInt()) % bombs.length;
		}
		//explosionTime = Math.abs(rndGenerator.nextLong()) % PhoneBumperActivity.MAX_BOMB_EXPLOSION_TIME;
		//if(explosionTime < PhoneBumperActivity.MIN_BOMB_EXPLOSION_TIME)
		//explosionTime = PhoneBumperActivity.MIN_BOMB_EXPLOSION_TIME;
		explosionTime = PhoneBumperActivity.MAX_BOMB_EXPLOSION_TIME;

		return true;
	}
	
}
