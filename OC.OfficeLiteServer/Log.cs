namespace OC.OfficeLiteServer;

public static class Log
{
    private static readonly object Lock = new();
    
    private static void WriteLine(string message)
    {
        lock (Lock)
        {
            if (Console.CursorVisible) return;
            Console.WriteLine(message);
        }
    }
    
    private static void WriteLine(string message, ConsoleColor color)
    {
        lock (Lock)
        {
            if (Console.CursorVisible) return;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    public static void Info(string message) 
        => WriteLine(message);
    public static void Warning(string message) 
        => WriteLine(message, ConsoleColor.Yellow);
    public static void Error(string message) 
        => WriteLine(message, ConsoleColor.Red);
}