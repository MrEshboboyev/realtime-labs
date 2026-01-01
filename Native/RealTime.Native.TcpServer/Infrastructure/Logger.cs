namespace RealTime.Native.TcpServer.Infrastructure;

public enum LogLevel 
{ 
    Info = 10,
    Warning = 20,
    Error = 30,
    Critical = 40
}

public class Logger
{
    public void Log(LogLevel level, string message, Exception? ex = null)
    {
        var color = level switch
        {
            LogLevel.Info => ConsoleColor.Gray,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpper()}] {message}";
        Console.WriteLine(logEntry);

        if (ex != null)
            Console.WriteLine($"   Exception: {ex.Message}");

        Console.ResetColor();
    }
}
