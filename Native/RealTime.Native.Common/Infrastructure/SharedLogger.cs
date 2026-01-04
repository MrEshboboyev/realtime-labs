using System.Runtime.CompilerServices;
using System.IO;

namespace RealTime.Native.Common.Infrastructure;

/// <summary>
/// Provides thread-safe logging functionality with colored console output
/// </summary>
public class SharedLogger(string owner)
{
    private readonly string _owner = owner; // "SERVER" or "CLIENT"

    /// <summary>
    /// Logs a message with the specified log level
    /// </summary>
    /// <param name="level">The log level</param>
    /// <param name="message">The message to log</param>
    /// <param name="ex">Optional exception to include in the log</param>
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

        lock (Console.Out) // Ensures thread-safe console writing in multi-threading environment
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

    /// <summary>
    /// Logs a message with the source location information
    /// </summary>
    /// <param name="level">The log level</param>
    /// <param name="message">The message to log</param>
    /// <param name="memberName">The name of the calling member</param>
    /// <param name="filePath">The file path of the calling member</param>
    /// <param name="lineNumber">The line number of the calling member</param>
    public void LogWithLocation(LogLevel level, string message, 
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var enhancedMessage = $"{message} | Location: {Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
        Log(level, enhancedMessage);
    }
}
