package com.groinpunchstudios.simple;

import java.io.IOException;
import java.net.InetSocketAddress;
import java.net.SocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.DatagramChannel;
import java.util.logging.Level;
import java.util.logging.Logger;

public class Server extends Thread
{
	private static final Logger LOG = Logger.getLogger(Server.class.getName());

	private DatagramChannel channel;
	private ByteBuffer buff = ByteBuffer.allocate(65507);
	public Server(int port) throws IOException
	{
		LOG.info("Starting SimpleServer on UDP:" + port);
		channel = DatagramChannel.open();
		channel.socket().bind(new InetSocketAddress(port));
	}

	@Override
	public void run()
	{
		try {
			SocketAddress addr = channel.receive(buff);
			LOG.info("Incoming packet from " + addr + ": " + new String(buff.array(), "UTF-8"));
			buff.flip();
			buff.clear();
			buff.put("Hej på dig, din fjert".getBytes("UTF-8"));
			Player player = new Player();
			player.name = "Tommy".getBytes("UTF-8");
			player.hp = 100;
			player.dx = 2000;
			player.dy = 2330;
			player.x = 1000;
			player.y = 1000;
			player.id = 1;
			player.serialize(buff);
			buff.flip();
			channel.send(buff, addr);
		} catch (IOException e) {
			LOG.log(Level.SEVERE, "Exception trying to receive UDP-package", e);
		}
	}
}
