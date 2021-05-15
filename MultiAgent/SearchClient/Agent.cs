using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient
{
    public class Agent
    {
        public readonly int Number;
        public readonly Color Color;
        private Position _initialPosition;

        public Agent(int number, Color color, Position initialPosition)
        {
            Number = number;
            Color = color;
            _initialPosition = initialPosition;
        }

        public Position GetInitialLocation()
        {
            return _initialPosition;
        }

        public void SetInitialLocation(Position position)
        {
            _initialPosition = position;
        }

        // DO NOT OVERWRITE EQUAL / HASHCODE!
        // TWO AGENTS SHOULD ALWAYS REFERENCE THE SAME AGENT

        public override string ToString()
        {
            return $"{Number}";
        }
    }
}
