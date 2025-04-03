using UnityChess;
using System;

[Serializable]
public struct NetworkSquare : IEquatable<NetworkSquare>
{
    public int File;
    public int Rank;

    public NetworkSquare(Square square)
    {
        File = square.File;
        Rank = square.Rank;
    }

    public Square ToSquare()
    {
        return new Square(File, Rank);
    }

    public bool Equals(NetworkSquare other)
    {
        return File == other.File && Rank == other.Rank;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkSquare other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(File, Rank);
    }
}