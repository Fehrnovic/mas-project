using System;

namespace MultiAgent.searchClient
{
    public class Position
    {
        public int Row;
        public int Column;

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

        public override bool Equals(object obj)
        {
            if (obj is Position position)
            {
                return Row == position.Row && Column == position.Column;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public override string ToString()
        {
            return $"Row: {Row}, Column: {Column}";
        }
    }
}
