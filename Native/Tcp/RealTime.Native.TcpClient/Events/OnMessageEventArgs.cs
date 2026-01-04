namespace RealTime.Native.TcpClient.Events;

/// <summary>
/// Serverdan yangi ma'lumot kelganda ishlatiladigan argumentlar
/// </summary>
public class OnMessageEventArgs(
    ReadOnlyMemory<byte> data
) : EventArgs
{
    // Xom baytlar (kerak bo'lib qolishi mumkin)
    public ReadOnlyMemory<byte> RawData { get; } = data;

    // Kelgan vaqti
    public DateTimeOffset ReceivedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ma'lumotni foydalanuvchi xohlagan tipga o'girib beruvchi yordamchi metod
    /// </summary>
    public string AsString() => System.Text.Encoding.UTF8.GetString(RawData.Span);
}
