namespace RealTime.Native.TcpServer.Protocols.Framing;

public interface IFrameHandler
{
    // Ma'lumotni yuborishdan oldin unga uzunlik prefiksini qo'shadi
    byte[] Wrap(byte[] data);

    // Oqimdan kelayotgan baytlarni yig'ib, to'liq xabar bo'lganda qaytaradi
    IEnumerable<byte[]> Unwrap(byte[] receivedData, Guid connectionId);
}
