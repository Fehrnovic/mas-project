using System;

namespace MultiAgent.searchClient
{
    public class Agent
    {
        public readonly int Number;
        public readonly Color Color;
        private Position _position;

        public Agent(int number, Color color)
        {
            Number = number;
            Color = color;
        }

        public Agent(int number, Position position)
        {
            Number = number;
            _position = position;
        }

        public Agent(int number, Color color, Position position)
        {
            Number = number;
            Color = color;
            _position = position;
        }

        public Position GetInitialLocation()
        {
            return _position;
        }

        public override bool Equals(object obj)
        {
            if (obj is Agent agent)
            {
                return Number == agent.Number && Color == agent.Color == _position.Equals(agent._position);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, Color, _position);
        }
    }
}
