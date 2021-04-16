using System;

namespace MultiAgent.searchClient
{
    public class Box
    {
        public readonly char Letter;
        private Position _position;
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
            _position = position;
        }

        public Position GetInitialLocation()
        {
            return _position;
        }

        public override bool Equals(object obj)
        {
            if (obj is Box box)
            {
                return Letter == box.Letter && Color == box.Color == _position.Equals(box._position);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Letter, Color, _position);
        }
    }
}
