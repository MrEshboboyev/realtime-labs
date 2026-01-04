namespace RealTime.Native.Udp.Models;

[Serializable]
public record UdpPacket(
    Guid PacketId,      // ACK (tasdiqlash) uchun
    long SequenceNumber, // Tartibni saqlash uchun
    byte[] Payload,     // Asosiy ma'lumot
    bool RequiresAck = false // Tasdiq kerakmi yoki yo'q?
);
