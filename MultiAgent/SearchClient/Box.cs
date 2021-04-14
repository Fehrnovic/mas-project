using System;

namespace MultiAgent.searchClient
{
    public class Box
    {
        public readonly char Letter;
        public Position Position;
        public readonly Color Color;

        public Box(char letter,  Color color)
        {
            Letter = letter;
            Color = color;
        }

        public Box(char letter, Color color, Position position)
        {
            Letter = letter;
            Color = color;
            Position = position;
        }

        public override bool Equals(object obj)
        {
            if (obj is Box box)
            {
                return Letter == box.Letter && Color == box.Color == Position.Equals(box.Position);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Letter, Color, Position);
        }
    }
}
