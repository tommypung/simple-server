using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

public class NetworkManager : MonoBehaviour {

	/*
	 * Only used in Unity to automatically connect when GameObject is created
	 */

	public string predefinedIP = null;
	public int predefinedPort = 0;
	public GameObject playerPrefab;
	public GameObject playerManager;

	private string _ip;
	private int _port;
	private bool _isConnected = false;
	private UdpClient _client;
	private IPEndPoint _remote;
	private Thread _receiveThread;
	private static int _waitingForReceive = 0x00;
	private byte[] output = new byte[65000];
	private int outputOffset = 0;
	private bool _loggedIn = false;
   private static object eventLock = new object();
private static List<Byte[]> events = new List<Byte[]>();

	public class State
	{
		public UdpClient _client;
		public IPEndPoint _remote;
		public NetworkManager _manager;
		public State(ref UdpClient client, ref IPEndPoint remote, NetworkManager manager)
		{
			this._client = client;
			this._remote = remote;
			this._manager = manager;
		}
	}

	// Use this for initialization
	void Start () {
		if (predefinedIP != null && predefinedPort != 0)
			Connect (predefinedIP, predefinedPort);
	}

	// Update is called once per frame

	public void Update () {
		if (isLoggedIn())
		if (_waitingForReceive == 0x00)
		{
			AddPlayerStatus();
			Send();
		}
	   lock(eventLock) 
	     {
		events.ForEach(el => parseResponse(el));
		events.Clear();
	     }
	}

	public void login()
	{
		_loggedIn = false;
		outputOffset = Packet.Player.login(ref output, 0, "Rico", 1920, 1080, 320, 200, 100);
		Send();
	}

   private void parseResponse(Byte[] receiveBytes)
     {
	int offset = 0;
	while(offset < receiveBytes.Length)
	  {
	     switch(receiveBytes[offset++])
	       {
		case 0x75:
		  eventLogin(
			     Packet.LoginResponse.getSecret(ref receiveBytes, offset),
			     Packet.LoginResponse.getId(ref receiveBytes, offset));
		  offset += Packet.LoginResponse.nextOffset(ref receiveBytes, offset);
		  break;
		case 0x70:
		  eventPlayer(
			      Packet.PlayerResponse.getId(ref receiveBytes, offset),
			      Packet.PlayerResponse.getX(ref receiveBytes, offset),
			      Packet.PlayerResponse.getY(ref receiveBytes, offset),
			      Packet.PlayerResponse.getDX(ref receiveBytes, offset),
			      Packet.PlayerResponse.getDY(ref receiveBytes, offset),
			      Packet.PlayerResponse.getHP(ref receiveBytes, offset)
			     );
		  offset += Packet.PlayerResponse.nextOffset(ref receiveBytes, offset);
		  break;
		default:
		  Console.WriteLine("Unknown command found: 0x{0:x2}", receiveBytes[offset]);
		  return;
	       }
	  }
     }
   
   private static void ReceiveCallback( IAsyncResult ar ) 
	{
		try 
		{
			State state = (State) ar.AsyncState;
			Byte[] receiveBytes = state._client.EndReceive(ar, ref state._remote);
			Console.WriteLine("Got {0} bytes", receiveBytes.Length);
			_waitingForReceive = 0x00;
		   lock(eventLock) {
			events.Add(receiveBytes);
		     }
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
		}
	}

	private void Send()
	{
		try {
			if (_waitingForReceive != 0)
				return; // just skipping instead of queueing, this is wrong

			Debug.Log ("Sending " + outputOffset + " bytes to UDP stream");
			_client.Send(output, outputOffset);
			outputOffset = 4; // don't overwrite the login credentials

			_waitingForReceive = Environment.TickCount;
			_client.BeginReceive(new AsyncCallback(ReceiveCallback), new State(ref _client, ref _remote, this));
		}  catch (Exception e) {
			Debug.LogError (e);
		}
	}

	private void AddPlayerStatus()
	{
		Int32 PLAYER_X = (int)playerPrefab.transform.position.x;
		Int32 PLAYER_Y = (int)playerPrefab.transform.position.y;
		Int16 PLAYER_DX = (Int16)playerPrefab.GetComponent<Rigidbody2D>().velocity.x;
		Int16 PLAYER_DY = (Int16)playerPrefab.GetComponent<Rigidbody2D>().velocity.y;
		byte PLAYER_HP = 100;

		outputOffset += Packet.Player.update(ref output, outputOffset, PLAYER_X, PLAYER_Y, PLAYER_DX, PLAYER_DY, PLAYER_HP);
	}

	//Establish UDP connection with desired ip/port
	public void Connect (string ip, int port) {
		Debug.Log ("Trying to connect with IP: " + ip + " and port: " + port);
		if (_isConnected) {
			SwitchConnection (ip, port);
			return;
		}

		try {
			_client = new UdpClient ();
			_remote = new IPEndPoint(IPAddress.Parse(ip), port);
			_client.Connect(_remote);
			_isConnected = true;
			_ip = ip;
			_port = port;
		} catch(Exception e) {
			Debug.LogError (e);
		}

	}

	// Allow [ip:port] format
	public void Connect (string ip) {
		string[] split = ip.Split (':');
		Connect (split[0], int.Parse(split[1]));
	}

	// Simply drops current connection
	public void Disconnect () {
		try {
			if (_receiveThread != null) 
				_receiveThread.Abort(); 
			if(_client != null)
				_client.Close ();
			_isConnected = false;
		} catch(Exception e) {
			Debug.LogError (e);
		}
	}

	// Basically jumps from one server to another
	private void SwitchConnection (string ip, int port) {
		Disconnect ();
		Connect (ip, port);
	}

	void OnDisable() 
	{ 
		Disconnect ();
	}

	public void eventLogin(Int32 secret, Int16 id)
	{
		_loggedIn = true;
		Packet.setInt32(ref output, 0, 0, secret); // store away the secret as the first Int32 in the packet, always
		Console.WriteLine("Logged in, got secret {0} and id {1}", secret, id);
	}

	public void eventPlayer(Int16 id, Int32 x, Int32 y, Int16 dx, Int16 dy, byte hp)
	{
		Console.WriteLine("Player update {0} @ {1}x{2} - {3}x{4} with {5}%hp", id, x, y, dx, dy, hp);
		playerManager.GetComponent<PlayerManager> ().UpdatePlayer (id, x, y, dx, dy, hp);
	}

	public bool isLoggedIn()
	{
		return _loggedIn;
	}

	public string GetIP() {
		return _ip;
	}

	public int GetPort() {
		return _port;
	}

	/* ==============================================
	 * ============== PACKET CLASS ==================
	 * ============================================== */

	public class Packet
	{
		public static int secret = 0x00000000;

		public static void setInt32(ref byte[] byteArr, int offset, int atBit, Int32 val)
		{
		   int startIndex = offset + (atBit >> 3);
		   int res = atBit % 8;
		   if (res == 0)
		     {
			byteArr[startIndex++] =   (byte) ((val>>24) & 0xFF);
			byteArr[startIndex++] = (byte) ((val>>16) & 0xFF);
			byteArr[startIndex++] = (byte) ((val>>8)  & 0xFF);
			byteArr[startIndex++] = (byte) (val       & 0xFF);
		     }
		   else
		     {
			byteArr[startIndex] &= (byte) (0xFF<<(8-res)); // clear old data
			byteArr[startIndex++] |= (byte) ((val >> (24 + res)) & (0xFF>>res));
			byteArr[startIndex++] = (byte) ((val >> (16 + res)) & (0xFF));
			byteArr[startIndex++] = (byte) ((val >> (8 + res)) & (0xFF));
			byteArr[startIndex++] = (byte) ((val >> (res)) & (0xFF));
			byteArr[startIndex] &= (byte) (0xFF>>res); // clear old data
			byteArr[startIndex++] |= (byte) ((val << (8-res)) & (0xFF<<(8-res)));
		     }
		}

		public static void setInt16(ref byte[] byteArr, int offset, int atBit, Int16 val)
		{
		   int startIndex = offset + (atBit >> 3);
		   int res = atBit % 8;
		   if (res == 0)
		     {
			byteArr[startIndex++] = (byte) ((val>>8)  & 0xFF);
			byteArr[startIndex++] = (byte) (val       & 0xFF);
		     }
		   else
		     {
			byteArr[startIndex] &= (byte) (0xFF<<(8-res)); // clear old data
			byteArr[startIndex++] |= (byte) ((val >> (8 + res)) & (0xFF>>res));
			byteArr[startIndex++] = (byte) ((val >> (res)) & (0xFF));
			byteArr[startIndex] &= (byte) (0xFF>>res); // clear old data
			byteArr[startIndex++] |= (byte) ((val << (8-res)) & (0xFF<<(8-res)));
		     }
		}

		public static int setString(ref byte[] byteArr, int offset, int atBit, string val)
		{
			int startIndex = offset + (atBit >> 3);
			byte[] toBytes = Encoding.UTF8.GetBytes(val);
			for(int i=0;i<toBytes.Length;i++)
				byteArr[offset + startIndex + i] = toBytes[i];
			offset += toBytes.Length;
			byteArr[offset + startIndex] = 0x00;
			return offset + 1;
		}

		public static void setByte(ref byte[] byteArr, int offset, int atBit, byte val)
		{
		   int startIndex = offset + (atBit >> 3);
		   int res = atBit % 8;
		   if (res == 0)
		     byteArr[startIndex++] = (byte) (val       & 0xFF);
		   else
		     {
			byteArr[startIndex] &= (byte) (0xFF<<(8-res)); // clear old data
			byteArr[startIndex++] |= (byte) ((val >> res) & (0xFF>>res));

			byteArr[startIndex] &= (byte) (0xFF>>res); // clear old data
			byteArr[startIndex] |= (byte) ((val << (8-res)) & (0xFF<<(8-res)));
		     }
		}

		public static Int32 getInt32(ref byte[] byteArr, int offset, int atBit)
		{
		   int startIndex = offset + (atBit >> 3);
		   int res = atBit % 8;
		   if (res == 0)
		     return (Int32) (
				     ((byteArr[startIndex]     << 24) & (UInt32) 0xFF000000) |
				     ((byteArr[startIndex + 1] << 16) & (UInt32) 0x00FF0000) |
				     ((byteArr[startIndex + 2] << 8)  & (UInt32) 0x0000FF00) |
				     ((byteArr[startIndex + 3]     )  & (UInt32) 0x000000FF));
		   else
		     {
			/* Int32 _first = (Int32) ((byteArr[startIndex] & (0xFF >> res)) << (24 + res)); */
			/* Int32 _second = (Int32) ((byteArr[startIndex + 1]) << (16 + res)); */
			/* Int32 _third = (Int32) ((byteArr[startIndex + 2]) << (8 + res)); */
			/* Int32 _fourth = (Int32) ((byteArr[startIndex + 3]) << (res)); */
			/* Int32 _fifth = (Int32) ((byteArr[startIndex + 4]) >> (8 - res)); */
			return (Int32) (
					((byteArr[startIndex] & (0xFF >> res)) << (24 + res)) |
					((byteArr[startIndex + 1]) << (16 + res)) |
					((byteArr[startIndex + 2]) << (8 + res)) |
					((byteArr[startIndex + 3]) << (res)) |
					((byteArr[startIndex + 4]) >> (8 - res))
				       );
		     }
		}

		 
		public static byte getByte(ref byte[] byteArr, int offset, int atBit)
		{
		   int startIndex = offset + (atBit >> 3);
		   int res = atBit % 8;
		   if (res == 0)
		     return byteArr[startIndex];
		   else
		     return (byte) (((byteArr[startIndex] << res) & (0xFF << res)) |
				    (byteArr[startIndex + 1] >> (8 - res)));
		}

		public static Int16 getInt16(ref byte[] byteArr, int offset, int atBit)
		{
		   int startIndex = offset + (atBit >> 3);
		   int res = atBit % 8;
		   if (res == 0)
		     return (Int16) (((byteArr[startIndex] << 8) & (UInt16) 0xFF00) | (byteArr[startIndex + 1] & (UInt16) 0x00FF));
		   else
		     {
			// atBit=5,  res=5
			// 8 + res = 13
			// 8 - res = 3
			// 
			// 
		        // 1234 5678 9ABC DEFG HIJK LMNO
			// 1234 5678                     & 0xFF >> 5 (res)
			// 0000 0FFF
			// ---------
			// 0000 0678                     << 8 + res
			// 6780 0000 0000 0000
			//
			// 9ABC DEFG                     << res
			// 0009 ABCD EFG0 0000
			// 
			// HIJK LMNO                     >> 3 (8 - res)
			// 0000 0000 000H IJKL
			
			Int16 _first = (Int16) ((byteArr[startIndex] & (0xFF >> res)) << (8 + res));
			Int16 _second = (Int16) ((byteArr[startIndex + 1]) << (res));
			Int16 _third = (Int16) ((byteArr[startIndex + 2]) >> (8 - res));

			/* Console.WriteLine("from _first byte  = " + Convert.ToString(_first, 2).PadLeft(16, '0')); */
			/* Console.WriteLine("from _second byte = " + Convert.ToString(_second, 2).PadLeft(16, '0')); */
			/* Console.WriteLine("from _third byte  = " + Convert.ToString(_third, 2).PadLeft(16, '0')); */

			return (Int16) (_first | _second | _third);
		     }
		}

		public static class LoginResponse
		{
			 public static Int32 getSecret(ref byte[] byteArr, int offset)   { return Packet.getInt32(ref byteArr, offset, 0); }
			 public static Int16 getId(ref byte[] byteArr, int offset)       { return Packet.getInt16(ref byteArr, offset, 32); }
			 public static Int16 nextOffset(ref byte[] byteArr, int offset)  { return 6; }
		}

		public static class PlayerResponse
		{
			public static Int16 getId(ref byte[] byteArr, int offset)       { return Packet.getInt16(ref byteArr, offset, 0); }
			 public static Int32 getX(ref byte[] byteArr, int offset)        { return Packet.getInt32(ref byteArr, offset, 16); }
			 public static Int32 getY(ref byte[] byteArr, int offset)        { return Packet.getInt32(ref byteArr, offset, 48); }
			 public static Int16 getDX(ref byte[] byteArr, int offset)       { return Packet.getInt16(ref byteArr, offset, 80); }
			 public static Int16 getDY(ref byte[] byteArr, int offset)       { return Packet.getInt16(ref byteArr, offset, 96); }
			 public static byte  getHP(ref byte[] byteArr, int offset)       { return Packet.getByte(ref byteArr, offset, 112); }
			 public static Int16 nextOffset(ref byte[] byteArr, int offset)  { return 15; }
		}

		public static class Player
		{
			public static int login(ref byte[] byteArr, int offset, string name, Int32 x, Int32 y, Int16 dx, Int16 dy, byte hp)
			{
				Packet.setInt32(ref byteArr, offset, 0, 0x00000000);
				offset += Packet.setString(ref byteArr, offset, 32, name);
				Packet.setInt32(ref byteArr, offset, 32,   x);
				Packet.setInt32(ref byteArr, offset, 64,   y);
				Packet.setInt16(ref byteArr, offset, 96,   dx);
				Packet.setInt16(ref byteArr, offset, 112,  dy);
				Packet.setByte( ref byteArr, offset, 128,  hp);
				return offset + 17;
			}

			public static int update(ref byte[] byteArr, int offset, Int32 x, Int32 y, Int16 dx, Int16 dy, byte hp)
			{
				Packet.setByte(ref byteArr, offset,  0,    0x75);
				Packet.setInt32(ref byteArr, offset, 8,   x);
				Packet.setInt32(ref byteArr, offset, 40,   y);
				Packet.setInt16(ref byteArr, offset, 72,   dx);
				Packet.setInt16(ref byteArr, offset, 88,  dy);
				Packet.setByte( ref byteArr, offset, 104,  hp);
				return offset + 14;
			}
		}
	}
}