using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

public class NetworkManager : MonoBehaviour {

	private string _ip;
	private int _port;
	private bool _isConnected = false;
	private UdpClient _client;
   private IPEndPoint _remote;
	private Thread _receiveThread;
   private static int _waitingForReceive = 0x00;
   public byte[] output = new byte[65000];
   private int outputOffset = 0;
   private bool _loggedIn = false;

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

	}

	// Update is called once per frame
	
   public void Update () {
      if (isLoggedIn())
	if (_waitingForReceive == 0x00)
	  {
	     AddPlayerStatus();
	     Send();
	  }
   }

   public void login()
     {
	_loggedIn = false;
	outputOffset = Packet.Player.login(ref output, 0, "Tommy Flinck", 1920, 1080, 320, 200, 100);
	Send();
     }

   private static void ReceiveCallback( IAsyncResult ar ) 
     {
	try 
	  {
	     State state = (State) ar.AsyncState;
	     Byte[] receiveBytes = state._client.EndReceive(ar, ref state._remote);
	     Console.WriteLine("Got {0} bytes", receiveBytes.Length);
	     _waitingForReceive = 0x00;
	     int offset = 0;
	     while(offset < receiveBytes.Length)
	       {
		  switch(receiveBytes[offset++])
		    {
		     case 0x75:
		       state._manager.eventLogin(
						 Packet.LoginResponse.getSecret(ref receiveBytes, offset),
						 Packet.LoginResponse.getId(ref receiveBytes, offset));
		       offset += Packet.LoginResponse.nextOffset(ref receiveBytes, offset);
		       break;
		     case 0x70:
		       state._manager.eventPlayer(
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
	Int32 PLAYER_X = 1920;
	Int32 PLAYER_Y = 1080;
	Int16 PLAYER_DX = 1000;
	Int16 PLAYER_DY = 1000;
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
     }

   public bool isLoggedIn()
     {
	return _loggedIn;
     }
}
