import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.net.InetSocketAddress;
import java.nio.ByteBuffer;
import java.nio.channels.DatagramChannel;
import java.nio.channels.SelectionKey;
import java.nio.channels.Selector;
import java.util.HashMap;
import java.util.Map;
import java.util.Scanner;

import com.googlecode.lanterna.TerminalPosition;
import com.googlecode.lanterna.TextCharacter;
import com.googlecode.lanterna.input.KeyStroke;
import com.googlecode.lanterna.input.KeyType;
import com.googlecode.lanterna.screen.Screen;
import com.googlecode.lanterna.screen.TerminalScreen;
import com.googlecode.lanterna.terminal.DefaultTerminalFactory;
import com.googlecode.lanterna.terminal.Terminal;
import com.groinpunchstudios.simple.Player;
import com.groinpunchstudios.simple.Server;

public class Client {
	public static String host;
	public static int port;
	private static ByteBuffer readBuff = ByteBuffer.allocate(65507);
	private static ByteBuffer writeBuff = ByteBuffer.allocate(65507);
	private static Player player;
	private static DatagramChannel channel;
	private static Selector sel;
	private static boolean guiMode = false;

	public static void main(String[] args) throws IOException
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
		channel = DatagramChannel.open();
		channel.configureBlocking(false);
		channel.socket().setSoTimeout(2000);
		sel = Selector.open();
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
			case "gui":
				try {
					enterGUIMode();
				} catch (IOException e) {
					e.printStackTrace();
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
		if (!guiMode)
			System.out.println("updatePlayer says: hp=" + (int) player.hp);

		writeBuff.clear();
		writeBuff.putInt(player.secret);
		writeBuff.put((byte) Server.COMMAND_UPDATE);
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
			writeBuff.putInt(0xFFFFFFFF);
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

	private static Map<Short, Player> players = new HashMap<>();

	private static void parseResponse() throws UnsupportedEncodingException
	{
		readBuff.flip();
		Player p;
		while(readBuff.hasRemaining())
		{
			byte cmd = readBuff.get();
			switch(cmd)
			{
			case Server.RESPONSE_YOU:
				if (player == null)
					player = new Player();
				player.secret = readBuff.getInt();
				player.id = readBuff.getShort();
				player.hp = 100;
				if (!guiMode)
					System.out.println("New secret received: " + player.secret + " with public id " + player.id + " - hp: " + player.hp);
				break;
			case Server.RESPONSE_PLAYER:
				p = getOrCreate(readBuff.getShort());
				p.deserialize(readBuff);
				if (!guiMode)
					System.out.println(p.id + " - " + p.x + "x" + p.y + " -> " + p.dx + "x" + p.dy);
				break;
			case Server.RESPONSE_PLAYER_EXT_ATTR:
				p = getOrCreate(readBuff.getShort());
				p.deserializeExtAttr(readBuff);
				if (!guiMode)
					System.out.println(p.id + " - " + p.getNameString());
				break;
			}
		}
	}

	private static Player getOrCreate(short id)
	{
		Player p = players.get(id);
		if (p == null) {
			p = new Player();
			p.id = id;
			players.put(id, p);
		}
		return p;
	}

	private static void printPlayer(Screen screen, int row, int col, Player player) throws IOException
	{
		printText(screen, row, col, "Player #" + player.id + ": " + player.x + "x" + player.y + ", " + player.dx + "x" + player.dy + ", latency: " + player.getLatency() + ", msSinceLastUpdate: " + player.getMsSinceLastUpdate() + ", alive: " + player.isAlive() + ", name: " + player.getNameString() + "           ");
	}

	private static int printText(Screen screen, int row, int col, String str) throws IOException
	{
		int len = str.length();
		for(int i=0;i<len;i++)
			screen.setCharacter(col + i, row, new TextCharacter(str.charAt(i)));
		return len;
	}

	private static void enterGUIMode() throws IOException
	{
		guiMode = true;
		Terminal terminal = new DefaultTerminalFactory().createTerminal();
		Screen screen = new TerminalScreen(terminal);
		screen.startScreen();

		while(true)
		{
			updatePlayer();
			screen.setCursorPosition(TerminalPosition.TOP_LEFT_CORNER);
			int y = 0;
			try {
				printPlayer(screen, y++, 0, player);
				for(Player p : players.values())
					printPlayer(screen, y++, 0, p);
			} catch(Exception e) {
				e.printStackTrace();
			}

			screen.refresh();

			KeyStroke stroke = screen.pollInput();
			if (stroke != null && stroke.getKeyType() == KeyType.Escape)
				break;
		}
		screen.stopScreen();
		printHelp();
	}

	private static ByteBuffer send(ByteBuffer writeBuff) throws IOException
	{
		channel.send(writeBuff, new InetSocketAddress(host,port));
		readBuff.clear();
		try {
			channel.register(sel, SelectionKey.OP_READ);
			int n = sel.select(1000);
			sel.selectedKeys().clear();
			if (n != 1)
			{
				System.out.println("No response received within 1000ms - n = " + n);
				readBuff.clear();
				return readBuff;
			}
		} finally {

		}

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
