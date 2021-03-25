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
        public Color Color;
        public Position Position;

        public Agent(int number, Color color)
        {
            Number = number;
            Color = color;
        }

        public Agent(int number, Position position)
        {
            Number = number;
            Position = position;
        }

        public Agent(int number, Color color, Position position)
        {
            Number = number;
            Color = color;
            Position = position;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Agent agent)
            {
                return Number == agent.Number && Color == agent.Color == Position.Equals(agent.Position);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Number.GetHashCode() + Color.GetHashCode() + Position.GetHashCode();
        }
    }
}
