using System;

namespace MultiAgent.searchClient
{
    public class Position
    {
        public int Row, Col;

        public Position(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public Position(Position position)
        {
            Row = position.Row;
            Col = position.Col;
        }

        public override bool Equals(object obj)
        {
            if (obj is Position position)
            {
                return this.Row == position.Row && this.Col == position.Col;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Col);
        }

        public override string ToString()
        {
            return $"Row: {Row}, Col: {Col}";
        }
    }
}
