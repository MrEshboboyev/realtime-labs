using RealTime.Native.Common.Infrastructure;
using RealTime.Native.TcpClient.Abstractions;

namespace RealTime.Native.TcpClient.Core;

internal class ReconnectionManager
{
    private readonly ITcpClient _client;
    private readonly ClientOptions _options;
    private readonly SharedLogger _logger;
    private int _retryCount = 0;
    private bool _isReconnecting = false;

    public ReconnectionManager(ITcpClient client, ClientOptions options, SharedLogger logger)
    {
        _client = client;
        _options = options;
        _logger = logger;

        // Mijoz uzilganda avtomatik ishga tushishi uchun eventga bog'laymiz
        _client.OnDisconnected += async (s, e) => await HandleDisconnectAsync();
    }

    private async Task HandleDisconnectAsync()
    {
        if (!_options.AutoReconnect || _isReconnecting) return;

        _isReconnecting = true;
        _retryCount = 0;

        _logger.Log(LogLevel.Warning, "Ulanish uzildi. Avtomatik qayta ulanish boshlanmoqda...");

        while (_retryCount < _options.MaxRetryAttempts && !_client.IsConnected)
        {
            _retryCount++;

            // Kutish vaqti: Exponential Backoff (2s, 4s, 8s...)
            int delaySeconds = (int)Math.Pow(_options.ReconnectDelay.TotalSeconds, _retryCount);
            _logger.Log(LogLevel.Info, $"Qayta ulanish urinishi {_retryCount}/{_options.MaxRetryAttempts}. Kutish: {delaySeconds}s");

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            try
            {
                await _client.ConnectAsync();
                if (_client.IsConnected)
                {
                    _logger.Log(LogLevel.Info, "Qayta ulanish muvaffaqiyatli yakunlandi.");
                    _isReconnecting = false;
                    _retryCount = 0;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Urinish {_retryCount} muvaffaqiyatsiz tugadi: {ex.Message}");
            }
        }

        _isReconnecting = false;
        _logger.Log(LogLevel.Critical, "Maksimal urinishlar soni tugadi. Avtomatik ulanish to'xtatildi.");
    }
}
