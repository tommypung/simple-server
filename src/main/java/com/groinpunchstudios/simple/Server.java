package com.groinpunchstudios.simple;

import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.net.InetSocketAddress;
import java.net.SocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.DatagramChannel;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.Map;
import java.util.logging.Level;
import java.util.logging.Logger;

public class Server extends Thread
{
	private static final Logger LOG = Logger.getLogger(Server.class.getName());
	private static final Collection<Server> servers = new LinkedList<>();

	public static final byte RESPONSE_PLAYER = 'p';
	public static final byte RESPONSE_YOU = 'u';

	private DatagramChannel channel;
	private ByteBuffer buff = ByteBuffer.allocate(65507);
	private ByteBuffer writeBuff = ByteBuffer.allocate(65507);
	private byte[] byteArray = new byte[100];
	private boolean running = true;
	Map<Integer, Player> players = new HashMap<>();

	private int playerId = 0;
	public int port;

	public Server(int port) throws IOException
	{
		this.port = port;
		LOG.info("Starting SimpleServer on UDP:" + port);
		channel = DatagramChannel.open();
		channel.socket().bind(new InetSocketAddress(port));

		synchronized(servers) {
			servers.add(this);
		}
	}

	@Override
	public void run()
	{
		while(running)
		{
			receiveMessage();
		}
	}

	private void receiveMessage()
	{
		try {
			writeBuff.clear();
			buff.clear();
			SocketAddress addr = channel.receive(buff);
			buff.flip();
			Player player = getPlayer(addr, buff, writeBuff);

			LOG.info("Incoming packet from " + addr  + " - " + player + ": " + buff.position());
			buff.flip();
			buff.clear();

			writeBuff.put("Hej på dig, din fjert".getBytes("UTF-8"));

			sendPlayers(player, writeBuff);
			writeBuff.flip();
			channel.send(writeBuff, addr);
			writeBuff.clear();
		} catch (IOException e) {
			LOG.log(Level.SEVERE, "Exception trying to receive UDP-package", e);
		}
	}

	private void sendPlayers(Player player, ByteBuffer write)
	{
		for(Player p : players.values())
			if (p.id != player.id) {
				write.put(RESPONSE_PLAYER);
				p.serialize(write);
			}
	}

	private Player getPlayer(SocketAddress addr, ByteBuffer read, ByteBuffer write) throws UnsupportedEncodingException
	{
		int secret = read.getInt();
		LOG.info("got secret: " + secret);
		if (secret == 0) {
			LOG.info("New player incoming - no secret provided - from " + addr);
			Player player = new Player();
			while(true) {
				player.generateNewSecret();
				if (getPlayer(player.secret) != null)
					continue;
				break;
			}

			int i=0;
			byte ch;
			do {
				byteArray[i++] = ch = read.get();
			} while(ch != 0);

			player.name = Arrays.copyOfRange(byteArray, 0, i);

			write.put(RESPONSE_YOU);
			write.putInt(player.secret);
			player.deserialize(read);

			addPlayer(player);
			write.putShort(player.id);

			return player;
		}

		Player player = getPlayer(secret);
		player.deserialize(write);
		return player;
	}

	private void addPlayer(Player player)
	{
		player.id = (short) playerId++;
		players.put(player.secret, player);
		LOG.info("Added new player: " + player);
	}

	private Player getPlayer(int secret)
	{
		return players.get(secret);
	}

	public static Collection<Server> getAllServers()
	{
		return servers;
	}
}
