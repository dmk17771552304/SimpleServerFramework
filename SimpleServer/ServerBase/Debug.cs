using System;

public static class Debug
{
    static Debug()
    {
      
    }

    public static void Log(object message)
    {
        Console.WriteLine(message);
    }

    public static void LogError(object message)
    {
        Console.WriteLine(message);
    }
}
