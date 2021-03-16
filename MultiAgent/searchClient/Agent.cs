using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.searchClient
{
    public class Agent
    {
        public int Number;
        public Position Position;
        public Color Color;

        public Agent(int number,  Color color)
        {
            Number = number;
            Color = color;
        }
    }
}
