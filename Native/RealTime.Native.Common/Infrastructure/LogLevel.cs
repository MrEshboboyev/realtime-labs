namespace RealTime.Native.Common.Infrastructure;

/// <summary>
/// Defines the levels of logging available
/// </summary>
public enum LogLevel 
{ 
    /// <summary>
    /// Informational messages
    /// </summary>
    Info,
    
    /// <summary>
    /// Successful operation messages
    /// </summary>
    Success,
    
    /// <summary>
    /// Warning messages
    /// </summary>
    Warning, 
    
    /// <summary>
    /// Error messages
    /// </summary>
    Error,
    
    /// <summary>
    /// Critical error messages
    /// </summary>
    Critical
}
