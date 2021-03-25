using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.searchClient
{
    public class Box
    {
        public char Letter;
        public Position Position;
        public Color Color;

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
        
        public override bool Equals(object? obj)
        {
            if (obj is Box box)
            {
                return Letter == box.Letter && Color == box.Color == Position.Equals(box.Position);
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            return Letter.GetHashCode() + Color.GetHashCode() + Position.GetHashCode();
        }
    }
}
