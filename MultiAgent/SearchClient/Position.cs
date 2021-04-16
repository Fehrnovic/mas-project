using System;

namespace MultiAgent.searchClient
{
    public struct Position : IEquatable<Position>
    {
        public readonly int Row;
        public readonly int Column;

        public Position(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public Position(Position position)
        {
            Row = position.Row;
            Column = position.Column;
        }

        public override string ToString()
        {
            return $"Row: {Row}, Column: {Column}";
        }

        public bool Equals(Position other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is Position position && Equals(position);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public static bool operator ==(Position left, Position right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }
    }
}
