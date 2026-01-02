namespace RealTime.Native.Common.Infrastructure;

public class SharedLogger(string owner)
{
    private readonly string _owner = owner; // "SERVER" yoki "CLIENT"

    public void Log(LogLevel level, string message, Exception? ex = null)
    {
        var color = level switch
        {
            LogLevel.Info => ConsoleColor.Cyan,
            LogLevel.Success => ConsoleColor.Green,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };

        lock (Console.Out) // Ko'p tarmoqli (multithreading) muhitda yozuvlar aralashib ketmasligi uchun
        {
            Console.ForegroundColor = color;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] [{_owner}] [{level.ToString().ToUpper()}] ");
            Console.ResetColor();
            Console.WriteLine(message);

            if (ex != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"   --> Exception: {ex.Message}");
                if (ex.StackTrace != null) Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }
        }
    }
}
