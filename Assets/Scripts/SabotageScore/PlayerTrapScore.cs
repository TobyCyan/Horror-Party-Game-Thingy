using System;
using Unity.Netcode;

public struct PlayerTrapScore : INetworkSerializable, IEquatable<PlayerTrapScore>
{
    public ulong clientId;
    public int trapScore;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref trapScore);
    }

    public bool Equals(PlayerTrapScore other)
    {
        return clientId == other.clientId && trapScore == other.trapScore;
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerTrapScore other && Equals(other);
    }

    public override int GetHashCode()
    {
        return clientId.GetHashCode() ^ trapScore.GetHashCode();
    }
}
