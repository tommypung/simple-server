using System;

namespace UnityEngine 
{
   public class PlayerManager
     {
     }
   

   public class Rigidbody2D
     {
	
     }

   public class GameObject
     {
	public class Transform
	  {
	     public class Position
	       {
		  public int x, y;
	       }
	     public Position position;
	  }
	public class Velocity
	  {
	     public int x, y;
	     public Velocity velocity;
	     public void UpdatePlayer(Int16 id, Int32 x, Int32 y, Int16 dx, Int16 dy, byte hp)
	       {
	       }
	  }
	
	public Transform transform;
	public Velocity GetComponent<T>()
	  {
	     return new Velocity();
	  }
     }
			    
   public class Debug
     {
	public static void LogError(object message, object context)
	  {
	     Console.WriteLine(message);
	  }

	public static void LogError(object message)
	  {
	     Console.WriteLine(message);
	  }

	public static void Log(string str)
	  {
	     Console.WriteLine(str);
	  }
     }
   
   public abstract class MonoBehaviour 
     {
	
     }
}
