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
