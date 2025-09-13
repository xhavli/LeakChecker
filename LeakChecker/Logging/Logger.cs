namespace LeakChecker.Logging;
public static class Logger
{
    private static readonly object ConsoleLock = new();

    public static void LogSuccess(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.WriteLine("[SUCCESS] " + message);
            Console.ResetColor();
        }
    }

    public static void LogWarning(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine("[WARNING] " + message);
            Console.ResetColor();
        }
    }

    public static void LogError(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("[ERROR] " + message);
            Console.ResetColor();
        }
    }
    
    public static void LogInfo(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Error.WriteLine("[INFO] " + message);
            Console.ResetColor();
        }
    }
}
