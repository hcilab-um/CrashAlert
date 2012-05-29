package ca.umanitoba.cs.hci.phonebumper;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.drawable.Drawable;
import android.util.AttributeSet;
import android.view.MotionEvent;
import android.view.View;

public class SlidingWindowManager extends View
{

	@SuppressWarnings("unused")
	private final int SOURCE_WIDTH = 640;
	private final int SOURCE_HEIGHT = 480;
	private float scaleHeight = 1;

	private Drawable backgroundImage;
	//private Bitmap bumperImage;

	private Paint solidWhitePaint = null;
	private Paint semiTransparentPaint = null;

	public SlidingWindowManager(Context context)
	{
		super(context);
		setUpManager(context);
	}

	public SlidingWindowManager(Context context, AttributeSet attrs)
	{
		super(context, attrs);
		setUpManager(context);
	}

	public SlidingWindowManager(Context context, AttributeSet attrs, int defStyle)
	{
		super(context, attrs, defStyle);
		setUpManager(context);
	}

	private void setUpManager(Context context)
	{
		backgroundImage = context.getResources().getDrawable(R.drawable.sliding_window_management);
		setBackgroundDrawable(backgroundImage);

		solidWhitePaint = new Paint();
		solidWhitePaint.setColor(Color.WHITE);

		semiTransparentPaint = new Paint();
		semiTransparentPaint.setARGB(180, 0xCC, 0xCC, 0xCC);
	}

	@Override
	protected void onMeasure(int widthMeasureSpec, int heightMeasureSpec)
	{
		int width = MeasureSpec.getSize(widthMeasureSpec);
		int height = width * backgroundImage.getIntrinsicHeight() / backgroundImage.getIntrinsicWidth();
		setMeasuredDimension(width, height);

		scaleHeight = (float) height / (float) SOURCE_HEIGHT;
	}

	private boolean isTrackingTouch = false;
	private float startingX = -1, startingY = -1;
	private int pointerId = -1, pointerIndex = -1;
	private float deltaX = -1, deltaY = -1;
	private int tmpBumperHeight = -1, tmpBumperDistanceFromTop = -1;

	@Override
	public boolean onTouchEvent(MotionEvent event)
	{
		if (event.getActionMasked() == MotionEvent.ACTION_DOWN)
		{
			if (pointerId != -1 && pointerIndex != -1 && event.getPointerCount() > 1)
				return true;

			isTrackingTouch = true;
			tmpBumperHeight = SettingsActivity.getBumperHeight();
			tmpBumperDistanceFromTop = SettingsActivity.getBumperDistanceFromTop();
			pointerIndex = event.getActionIndex();
			pointerId = event.getPointerId(pointerIndex);
			startingX = event.getX(pointerIndex);
			startingY = event.getY(pointerIndex);
			deltaX = 0;
			deltaY = 0;
		}
		else if (event.getActionMasked() == MotionEvent.ACTION_MOVE)
		{
			if (event.getActionIndex() != pointerIndex)
				return true;

			// Handles changes in bumperHeight and bumperDistanceFromTop
			deltaX = event.getX(pointerIndex) - startingX;
			deltaY = event.getY(pointerIndex) - startingY;

			startingX = event.getX(pointerIndex);
			startingY = event.getY(pointerIndex);

			tmpBumperHeight = tmpBumperHeight + (int) deltaX;
			if (tmpBumperHeight < 10)
				tmpBumperHeight = 10;
			if (tmpBumperHeight > SOURCE_HEIGHT)
				tmpBumperHeight = SOURCE_HEIGHT;

			float scaledDeltaY = deltaY / scaleHeight;
			tmpBumperDistanceFromTop = tmpBumperDistanceFromTop + (int) scaledDeltaY;
			if (tmpBumperDistanceFromTop < 0)
				tmpBumperDistanceFromTop = 0;
			if ((tmpBumperDistanceFromTop + tmpBumperHeight) > SOURCE_HEIGHT)
				tmpBumperDistanceFromTop = SOURCE_HEIGHT - tmpBumperHeight;

			invalidate();
		}
		else if (event.getActionMasked() == MotionEvent.ACTION_UP)
		{
			if (event.getActionIndex() != pointerIndex)
				return true;

			isTrackingTouch = false;
			SettingsActivity.setBumperDistanceFromTop(tmpBumperDistanceFromTop);
			SettingsActivity.setBumperHeight(tmpBumperHeight);
			pointerIndex = -1;
			pointerId = -1;
			startingX = -1;
			startingY = -1;
			deltaX = -1;
			deltaY = -1;
			invalidate();
		}
		return true;
	}

	@Override
	protected void onDraw(Canvas canvas)
	{
		if (deltaX != -1)
			canvas.drawText(((int) deltaX) + "", 10, 10, solidWhitePaint);
		if (deltaY != -1)
			canvas.drawText(((int) deltaY) + "", 50, 10, solidWhitePaint);

		float left = 0;
		float top = SettingsActivity.getBumperDistanceFromTop() * scaleHeight;
		float right = getWidth();
		float bottom = (SettingsActivity.getBumperHeight() + SettingsActivity.getBumperDistanceFromTop()) * scaleHeight;
		if (isTrackingTouch)
		{
			top = tmpBumperDistanceFromTop * scaleHeight;
			bottom = (tmpBumperHeight + tmpBumperDistanceFromTop) * scaleHeight;
		}
		canvas.drawRect(left, top, right, bottom, semiTransparentPaint);
		
//		if(!isTrackingTouch && bumperImage != null)
//		{
//			float bumperScale = getWidth() / (float)bumperImage.getWidth();
//			Matrix backUp = canvas.getMatrix();
//			Matrix scalingMatrix = new Matrix(backUp);
//			//scalingMatrix.setTranslate(left, -top);
//			scalingMatrix.setScale(bumperScale, bumperScale);
//			canvas.setMatrix(scalingMatrix);
//			canvas.drawBitmap(bumperImage, 0, 0, null);
//			canvas.setMatrix(backUp);
//		}
	}
	
	public void setBumperImage(Bitmap bm)
	{
//		bumperImage = bm;
//		invalidate();
	}

}
