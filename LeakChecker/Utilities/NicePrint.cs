namespace LeakChecker.Utilities;
public static class NicePrint
{
    private static readonly object ConsoleLock = new();

    public static void PrintSuccess(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.WriteLine("[SUCCESS] " + message);
            Console.ResetColor();
        }
    }

    public static void PrintWarning(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.WriteLine("[WARNING] " + message);
            Console.ResetColor();
        }
    }

    public static void PrintError(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("[ERROR] " + message);
            Console.ResetColor();
        }
    }
    
    public static void PrintInfo(string message)
    {
        lock (ConsoleLock)
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Error.WriteLine("[INFO] " + message);
            Console.ResetColor();
        }
    }
}
