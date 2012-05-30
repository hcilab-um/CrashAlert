package ca.umanitoba.cs.hci.phonebumper;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import android.util.AttributeSet;
import android.view.View;

public class Bumper extends View {

	private Drawable bumperImage;

	public Bumper(Context context) {
		super(context);
		bumperImage = context.getResources().getDrawable(R.drawable.bumper_color);
		setBackgroundDrawable(bumperImage);
	}

	public Bumper(Context context, AttributeSet attrs) {
		super(context, attrs);
		bumperImage = context.getResources().getDrawable(R.drawable.bumper_color);
		setBackgroundDrawable(bumperImage);
	}

	public Bumper(Context context, AttributeSet attrs, int defStyle) {
		super(context, attrs, defStyle);
		bumperImage = context.getResources().getDrawable(R.drawable.bumper_color);
		setBackgroundDrawable(bumperImage);
	}

	@Override
	protected void onMeasure(int widthMeasureSpec, int heightMeasureSpec) {
		int width = MeasureSpec.getSize(widthMeasureSpec);
		int height = width * bumperImage.getIntrinsicHeight() / bumperImage.getIntrinsicWidth();
		setMeasuredDimension(width, height);
	}
	
	public void setImageBitmap(Bitmap bm)
	{
		bumperImage = new BitmapDrawable(bm);
		setBackgroundDrawable(bumperImage);
		invalidate();
		refreshDrawableState();
	}
	
	public void setImageBitmap(Drawable drawable)
	{
		bumperImage = drawable;
		setBackgroundDrawable(bumperImage);
		invalidate();
		refreshDrawableState();
	}
	
}
