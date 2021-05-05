using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient
{
    public interface IAgent
    {
        Agent ReferenceAgent { get; }
        List<Agent> Agents { get; }
    }

    public class Agent : IAgent
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

        public Agent ReferenceAgent => this;
        public List<Agent> Agents => new List<Agent>() {this};
    }

    public class MetaAgent : IAgent
    {
        public List<Agent> Agents { get; } = new();
        public Agent ReferenceAgent => Agents[0];

        public override bool Equals(object? obj)
        {
            if (obj is MetaAgent ma)
            {
                return ma.Agents.Count == Agents.Count && !ma.Agents.Except(Agents).Any();
            }

            return false;
        }
    }
}
