package com.groinpunchstudios.simple;

import java.io.UnsupportedEncodingException;
import java.nio.ByteBuffer;
import java.util.Arrays;
import java.util.Random;

public class Player
{
	private static final Random rand = new Random();

	public int secret;
	public short id;
	byte[] name;
	public int x;
	public int y;
	public short dx;
	public short dy;
	public byte hp;
	public long mLastUpdateTimestamp = 0;
	public long mPreviousUpdateTimestamp = 0;
	public long mLatencyTotal = 0;
	public long mLatencyCount = 0;

	public long mLastExtAttrChange = 0;

	private String mNameString;

	public int appearance;

	public boolean hasLastUpdateBetween(long from, long to)
	{
		return from <= mLastUpdateTimestamp && mLastUpdateTimestamp <= to;
	}

	public boolean hasNameChangedBetween(long from, long to)
	{
		return from <= mLastExtAttrChange && mLastExtAttrChange <= to;
	}

	public void serialize(ByteBuffer buffer)
	{
		buffer.putShort(id);
		buffer.putInt(x);
		buffer.putInt(y);
		buffer.putShort(dx);
		buffer.putShort(dy);
		buffer.put(hp);
	}

	public void generateNewSecret()
	{
		secret = rand.nextInt();
	}

	@Override
	public String toString()
	{
		try {
			if (name != null)
				return new String(name, "UTF-8") + " @ " + x + "x" + y + " id:" + id + " secret:" + secret + ", latency: ";
			else
				return "noname @ " + x + "x" + y + " id:" + id + " secret:" + secret;
		} catch (UnsupportedEncodingException e) {
			return null;
		}
	}

	public void deserialize(ByteBuffer buff)
	{
		x = buff.getInt();
		y = buff.getInt();
		dx = buff.getShort();
		dy = buff.getShort();
		hp = buff.get();

		addLatencyCalculations();
	}

	private void addLatencyCalculations()
	{
		long curr = System.currentTimeMillis();
		mPreviousUpdateTimestamp = mLastUpdateTimestamp;
		if (mLastUpdateTimestamp != 0)
		{
			mLatencyTotal += curr - mLastUpdateTimestamp;
			mLatencyCount++;
		}

		mLastUpdateTimestamp = curr;
	}

	public long getMsSinceLastUpdate()
	{
		return System.currentTimeMillis() - mLastUpdateTimestamp;
	}

	public long getNumUpdatesReceived()
	{
		return mLatencyCount;
	}

	public boolean isAlive()
	{
		return (System.currentTimeMillis() - mLastUpdateTimestamp) < 10000;
	}

	public double getLatency()
	{
		return ((System.currentTimeMillis() - mLastUpdateTimestamp) + mLatencyTotal) / Math.max(mLatencyCount + 1, 1);
	}

	public void serializeExtendedAttributes(ByteBuffer write)
	{
		write.putShort(id);
		write.put(name);
		write.put((byte) 0x00);
		write.putInt(appearance);
	}

	public void deserializeExtAttr(ByteBuffer read) throws UnsupportedEncodingException
	{
		byte[] name = new byte[100];
		int index = 0;
		byte ch;
		while( (ch = read.get()) != 0x00)
			if (index < 100)
				name[index++] = ch;

		setName(Arrays.copyOfRange(name, 0, index));
		this.appearance = read.getInt();

		this.mLastExtAttrChange = System.currentTimeMillis();
	}

	public byte[] getName()
	{
		return name;
	}

	public void setName(byte[] copyOfRange) throws UnsupportedEncodingException
	{
		this.mLastExtAttrChange = System.currentTimeMillis();
		this.name = copyOfRange;
		if (copyOfRange != null)
			this.mNameString = new String(this.name, "UTF-8");
	}

	public String getNameString()
	{
		return this.mNameString;
	}
}
