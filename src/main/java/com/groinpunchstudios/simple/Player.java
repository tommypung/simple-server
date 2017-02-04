package com.groinpunchstudios.simple;

import java.io.UnsupportedEncodingException;
import java.nio.ByteBuffer;
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
				return new String(name, "UTF-8") + " @ " + x + "x" + y + " id:" + id + " secret:" + secret;
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
	}
}
