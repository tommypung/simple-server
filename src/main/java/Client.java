import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.DatagramChannel;
import java.util.Scanner;

import com.groinpunchstudios.simple.Player;
import com.groinpunchstudios.simple.Server;

public class Client {
	public static String host;
	public static int port;
	private static ByteBuffer readBuff = ByteBuffer.allocate(65507);
	private static ByteBuffer writeBuff = ByteBuffer.allocate(65507);
	private static Player player;

	public static void main(String[] args)
	{
		System.out.println("Testing client for SimpleServer");
		if (args.length < 2)
		{
			System.out.println("Call with two arguments HOST PORT, example: Client localhost 9999");
			System.exit(1);
		}

		host = args[0];
		port = Integer.parseInt(args[1]);
		System.out.println("Communicating with " + host + ":" + port);
		printHelp();
		Scanner s = new Scanner(System.in);
		while(s.hasNext()) {
			String cmd = s.next();
			switch(cmd)
			{
			case "set_hp":
				if (player == null) {
					System.out.println("No player set - call new_player");
				} else {
					player.hp = (byte) s.nextShort();
					updatePlayer();
				}
				break;
			case "move_player":
				if (player == null) {
					System.out.println("No player set - call new_player");
				} else {
					player.x = s.nextInt();
					player.y = s.nextInt();
					player.dx = s.nextShort();
					player.dy = s.nextShort();
					updatePlayer();
				}
				break;
			case "new_player":
				sendNewPlayer(s);
				break;
			case "help":
				printHelp();
				break;
			case "exit":
			case "quit":
				s.close();
				System.exit(0);
				break;
			default:
				System.out.println("Unknown command: " + cmd);	
			}
		}
		s.close();
	}

	private static void updatePlayer()
	{
		System.out.println("updatePlayer says: hp=" + (int) player.hp);
		writeBuff.clear();
		writeBuff.putInt(player.secret);
		writeBuff.putInt(player.x);
		writeBuff.putInt(player.y);
		writeBuff.putShort(player.dx);
		writeBuff.putShort(player.dy);
		writeBuff.put(player.hp);
		writeBuff.flip();
		try {
			send(writeBuff);
			parseResponse();
		} catch(IOException e) {
			e.printStackTrace();
		}
	}

	private static void sendNewPlayer(Scanner s)
	{
		try {
			String name = s.next();
			int x = s.nextInt();
			int y = s.nextInt();
			System.out.println("x = " + x + ", y = " + y);
			writeBuff.clear();
			writeBuff.putInt(0x00000000);
			writeBuff.put(name.getBytes("UTF-8"));
			writeBuff.put((byte) 0x00);
			writeBuff.putInt(x);
			writeBuff.putInt(y);
			writeBuff.putShort((short) 0x0000);
			writeBuff.putShort((short) 0x0000);
			writeBuff.put((byte) 100);
			writeBuff.flip();
			send(writeBuff);
			parseResponse();
		} catch(IOException e) {
		}
	}

	private static void parseResponse()
	{
		readBuff.flip();
		while(readBuff.hasRemaining())
		{
			byte cmd = readBuff.get();
			switch(cmd)
			{
			case Server.RESPONSE_YOU:
				player = new Player();
				player.secret = readBuff.getInt();
				player.id = readBuff.getShort();
				player.hp = 100;
				System.out.println("New secret received: " + player.secret + " with public id " + player.id + " - hp: " + player.hp);
				break;
			case Server.RESPONSE_PLAYER:
				Player p = new Player();
				p.id = readBuff.getShort();
				p.deserialize(readBuff);
				System.out.println(p.id + " - " + p.x + "x" + p.y + " -> " + p.dx + "x" + p.dy);
				break;
			}
		}
	}

	private static ByteBuffer send(ByteBuffer writeBuff) throws IOException
	{
		DatagramChannel channel = DatagramChannel.open();
		channel.send(writeBuff, new InetSocketAddress(host,port));
		readBuff.clear();
		channel.receive(readBuff);
		return readBuff;
	}

	private static void printHelp()
	{
		System.out.println("List of commands:");
		System.out.println("new_player [name] [x] [y]");
		System.out.println("move_player [x] [y] [dx] [dy]");
		System.out.println("set_hp [hp]");
		System.out.println("exit\nquit");
	}
}
