using RealTime.Native.Common.Infrastructure;
using RealTime.Native.TcpClient.Abstractions;

namespace RealTime.Native.TcpClient.Core;

/// <summary>
/// Manages automatic reconnection logic for the TCP client
/// </summary>
internal class ReconnectionManager
{
    private readonly ITcpClient _client;
    private readonly ClientOptions _options;
    private readonly SharedLogger _logger;
    private int _retryCount = 0;
    private bool _isReconnecting = false;
    private readonly Lock _lock = new();

    public ReconnectionManager(ITcpClient client, ClientOptions options, SharedLogger logger)
    {
        _client = client;
        _options = options;
        _logger = logger;

        // Subscribe to disconnect event to trigger reconnection
        _client.OnDisconnected += async (s, e) => await HandleDisconnectAsync();
    }

    private async Task HandleDisconnectAsync()
    {
        if (!_options.AutoReconnect || _isReconnecting) return;
        
        lock (_lock)
        {
            if (_isReconnecting) return; // Double-check to prevent race conditions
            _isReconnecting = true;
        }

        _logger.Log(LogLevel.Warning, "Connection lost. Starting automatic reconnection...");

        while (_retryCount < _options.MaxRetryAttempts && !_client.IsConnected)
        {
            _retryCount++;

            // Exponential backoff: 2s, 4s, 8s, 16s, etc.
            double delaySeconds = Math.Pow(2, _retryCount) * _options.ReconnectDelay.TotalSeconds;
            _logger.Log(LogLevel.Info, $"Reconnection attempt {_retryCount}/{_options.MaxRetryAttempts}. Waiting: {delaySeconds}s");

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                await _client.ConnectAsync();
                if (_client.IsConnected)
                {
                    _logger.Log(LogLevel.Info, "Reconnection successful.");
                    lock (_lock)
                    {
                        _isReconnecting = false;
                    }
                    _retryCount = 0;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Attempt {_retryCount} failed: {ex.Message}");
            }
        }

        lock (_lock)
        {
            _isReconnecting = false;
        }
        _logger.Log(LogLevel.Critical, "Maximum reconnection attempts reached. Automatic reconnection stopped.");
    }
    
    /// <summary>
    /// Resets the reconnection state
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _isReconnecting = false;
        }
        _retryCount = 0;
    }
}
