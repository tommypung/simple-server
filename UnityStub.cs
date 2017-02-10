using System;

namespace UnityEngine 
{
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
