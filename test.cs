using System.Runtime.CompilerServices;
using System;

public class Packet
{
   public static Int32 getInt32(ref byte[] byteArr, int offset, int atBit)
     {
	int startIndex = offset + (atBit >> 3);
	if ((atBit % 8) == 0)
	  return (Int32) (
			  ((byteArr[startIndex]     << 24) & (UInt32) 0xFF000000) |
			  ((byteArr[startIndex + 1] << 16) & (UInt32) 0x00FF0000) |
			  ((byteArr[startIndex + 2] << 8)  & (UInt32) 0x0000FF00) |
			  ((byteArr[startIndex + 3]     )  & (UInt32) 0x000000FF));

	return -1;	
     }

   [MethodImpl(MethodImplOptions.AggressiveInlining)] 
   public static byte getByte(ref byte[] byteArr, int offset, int atBit)
       {
	  return byteArr[offset + (atBit >> 3)];
       }

   public static Int16 getInt16(ref byte[] byteArr, int offset, int atBit)
     {
	int startIndex = offset + (atBit >> 3);
	if ((atBit % 8) == 0)
	  return (Int16) (((byteArr[startIndex] << 8) & (UInt16) 0xFF00) | (byteArr[startIndex + 1] & (UInt16) 0x00FF));

	return -1;
     }
  
   public static class PlayerResponse
     {
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int32 getX(ref byte[] byteArr, int offset)        { return Packet.getInt32(ref byteArr, offset, 16); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int32 getY(ref byte[] byteArr, int offset)        { return Packet.getInt32(ref byteArr, offset, 48); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int16 getDX(ref byte[] byteArr, int offset)       { return Packet.getInt16(ref byteArr, offset, 80); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int16 getDY(ref byte[] byteArr, int offset)       { return Packet.getInt16(ref byteArr, offset, 96); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte  getHP(ref byte[] byteArr, int offset)       { return Packet.getByte(ref byteArr, offset, 112); }
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int16 nextOffset(ref byte[] byteArr, int offset)  { return 15; }
     }
}

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
     }
}
