package com.groinpunchstudios.simple;

import java.nio.ByteBuffer;

public class Player
{
	short id;
	byte[] name;
	int x;
	int y;
	short dx;
	short dy;
	byte hp;

	public void serialize(ByteBuffer buffer)
	{
		buffer.putShort(id);
		buffer.put((byte) name.length);
		buffer.put(name, 0, name.length & 0xff);
		buffer.putInt(x);
		buffer.putInt(y);
		buffer.putShort(dx);
		buffer.putShort(dy);
		buffer.put(hp);
	}
}
