using System.Runtime.CompilerServices;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static NetworkManager;

public class HelloWorld
{
   static public void PrintArray(ref byte[] byteArr)
     {
	foreach(var item in byteArr)
	  Console.Write("{0:x2} ", item);
	Console.WriteLine("");
     }

   static public void Main ()
     {
	byte[] byteArr = new byte[] {
	     0x32, 0xb9, 0x3a, 0xf1, // SECRET
	     0x00, 0x00, 0x00, 0x96, // X
	     0x00, 0x00, 0x00, 0xfa, // Y
	     0x00, 0xc8, // DX
	     0x04, 0x03, // DY
	     0x64 // HP
	};

	Console.Write("Packet: ");
	PrintArray(ref byteArr);

	Console.WriteLine("secret: {0}\nx: {1}\ny: {2}\ndx: {3}\ndy: {4}\nhp: {5}",
			  Packet.getInt32(ref byteArr, 0, 0 * 8),
			  Packet.getInt32(ref byteArr, 0, 4 * 8),
			  Packet.getInt32(ref byteArr, 0, 8 * 8),
			  Packet.getInt16(ref byteArr, 0, 12 * 8),
			  Packet.getInt16(ref byteArr, 0, 14 * 8),
			  Packet.getByte( ref byteArr, 0, 16 * 8));

	
	/** Here starts a normal "receive"-packet parseing
	 */
	byte[] pR = new byte[] {
	     0x70, // COMMAND
	       0x00, 0x01, // ID
	       0x00, 0x00, 0x00, 0x78, // X
	       0x00, 0x00, 0x00, 0xc8, // Y
	       0x00, 0x00, // DX
	       0x00, 0x00, // DY
	       0x64, // HP

	       0x70, // COMMAND
	       0x00, 0x00, // ID
	       0x00, 0x00, 0x00, 0x96, // X
	       0x00, 0x00, 0x00, 0xfa, // Y
	       0x00, 0xc8, // DX
	       0x04, 0x03, // DY
	       0x64 // HP
	  };

	for(int offset = 0; offset < pR.Length;)
	  {
	     switch(pR[offset++])
	       {
		case 0x70:
		  Console.WriteLine("Player: {0}x{1} -> {2}x{3} with {4}% hp",
				    Packet.PlayerResponse.getX(ref pR, offset),
				    Packet.PlayerResponse.getY(ref pR, offset),
				    Packet.PlayerResponse.getDX(ref pR, offset),
				    Packet.PlayerResponse.getDY(ref pR, offset),
				    Packet.PlayerResponse.getHP(ref pR, offset)
				   );
		  offset += Packet.PlayerResponse.nextOffset(ref pR, offset);
		  break;
		default:
		  Console.WriteLine("Unknown command found({0})", (char) pR[offset]);
		  return;
	       }
	  }
	
	byte[] br = new byte[] 
	  {
	     0xAB, 0xF3, 0x3F, 0x40, 0xC0 // 1010 1011 - 1111 11111 - 1111 1111 - 0100 0000
	  };
	byte[] brO = new byte[] {
	   0xFF, 0xFF, 0xFF, 0xFF, 0xFF
	};
	Console.WriteLine("brO = " + string.Join(" ", brO.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray()));
	Packet.setInt32(ref brO, 0, 2, unchecked((Int32) 0xAFCCFD03));
	Console.WriteLine("br  = " + string.Join(" ", br.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray()));
	Console.WriteLine("brO = " + string.Join(" ", brO.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray()));
	Console.WriteLine("2 % 8 = " + 2%8);
	Console.WriteLine("Read= " + Convert.ToString(Packet.getInt32(ref brO, 0, 2), 2));

	Packet.setInt32(ref brO, 0, 2, unchecked((Int32) 0x00001120));
	/* Console.WriteLine("br  = " + string.Join(" ", br.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray())); */
	Console.WriteLine("brO = " + string.Join("", brO.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray()));
	Console.WriteLine("Read= --" + Convert.ToString(
							Packet.getInt32(ref brO, 0, 2),
							2).PadLeft(32, '0'));

	if (Packet.getInt32(ref brO, 0, 2) != 0x00001120)
	  Console.WriteLine("Ints do not match");

	/* if (true) return; */
	
	for(int i=0;i<=18;i++)
	  checkByte(i);

	for(int i=0;i<=15;i++)
	  checkInt16(i);

	checkInt32(7);
	
	if (true) return;
	Console.WriteLine("----- Running Johnnys NetworkManager.cs -----");
	NetworkManager nm = new NetworkManager();
	nm.playerPrefab = new UnityEngine.GameObject();
	nm.playerManager = new UnityEngine.GameObject();
	nm.Connect("127.0.0.1", 9999);
	nm.login();
	while(true)
	  {
	     nm.Update();
	  }
     }

   public static void checkByte(int atBit)
     {
	Console.WriteLine("Testing all possible values of Byte - quick (atBit={0})", atBit);
	byte[] brO = new byte[] {
	   0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
	};
	for(byte i=0;i<0xFF;i++)
	  {
	     Packet.setByte(ref brO, 0, atBit, i);
	     byte res = Packet.getByte(ref brO, 0, atBit);
	     if ((UInt16) res != (UInt16) i)
	       {
		  Console.WriteLine("res = " + (atBit % 8));
		  Console.WriteLine("Mismatching i  =0x{0:X2}", i);
		  Console.WriteLine("Mismatching res=0x{0:X2}", res);
		  Console.WriteLine("brO = " + string.Join("", brO.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray()));
		  Console.WriteLine("Read= " + "".PadLeft(atBit, '-') +
				    Convert.ToString(
						     res,
						     2).PadLeft(8, '0')
				   );
		  System.Environment.Exit(1);
	       }
	  }
	Console.WriteLine("Done");
     }

   public static void checkInt16(int atBit)
     {
	Console.WriteLine("Testing all possible values of Int16 - quick (atBit={0})", atBit);
	byte[] brO = new byte[] {
	   0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
	};
	for(UInt16 i=0;i<0xFFFF;i++)
	  {
	     if ((i & 0x3FFF) == 0)
	       Console.WriteLine("i = " + i + " {0}%", ((double)i / (double)0xFFFF) * 100);

	     Packet.setInt16(ref brO, 0, atBit, unchecked((Int16) i));
	     UInt16 res = (UInt16) Packet.getInt16(ref brO, 0, atBit);
	     if ((UInt16) res != (UInt16) i)
	       {
		  Console.WriteLine("res = " + (atBit % 8));
		  Console.WriteLine("Mismatching i  =0x{0:X8}", i);
		  Console.WriteLine("Mismatching res=0x{0:X8}", res);
		  Console.WriteLine("brO = " + string.Join("", brO.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray()));
		  Console.WriteLine("Read= " + "".PadLeft(atBit, '-') +
				    Convert.ToString(
						     res,
						     2).PadLeft(16, '0')
				   );
		  System.Environment.Exit(1);
	       }
	  }
	Console.WriteLine("Done");
     }

   public static void checkInt32(int atBit)
     {
	Console.WriteLine("Testing all possible values of Int32 - this might take a while (atBit={0})", atBit);
	byte[] brO = new byte[] {
	   0xFF, 0xFF, 0xFF, 0xFF, 0xFF
	};
	for(UInt32 i=0;i<0xFFFFFFFF;i++)
	  {
	     if ((i & 0x0FFFFFFF) == 0)
	       Console.WriteLine("i = " + i + " {0}%", ((double)i / (double)0xFFFFFFFF) * 100);
	     
	     Packet.setInt32(ref brO, 0, atBit, unchecked((Int32) i));
	     UInt32 res = (UInt32) Packet.getInt32(ref brO, 0, atBit);
	     if ((UInt32) res != (UInt32) i)
	       {
		  Console.WriteLine("Mismatching i  =0x{0:X8}", i);
		  Console.WriteLine("Mismatching res=0x{0:X8}", res);
		  Console.WriteLine("brO = " + string.Join("", brO.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray()));
		  Console.WriteLine("Read= " + "".PadLeft(atBit, '-') +
				    Convert.ToString(
						     res,
						     2).PadLeft(32, '0')
				   );
		  return;
	       }
	  }
	Console.WriteLine("Done");
     }
}

