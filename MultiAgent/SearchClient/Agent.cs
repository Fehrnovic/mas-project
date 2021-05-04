using System.Collections.Generic;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient
{
    public interface IAgent
    {
        Agent ReferenceAgent { get; }
    }

    public class Agent : IAgent
    {
        public readonly int Number;
        public readonly Color Color;
        private readonly Position _initialPosition;

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

        // DO NOT OVERWRITE EQUAL / HASHCODE!
        // TWO AGENTS SHOULD ALWAYS REFERENCE THE SAME AGENT

        public override string ToString()
        {
            return $"{Number}";
        }

        public Agent ReferenceAgent => this;
    }

    public class MetaAgent : IAgent
    {
        public List<Agent> Agents = new();
        public Agent ReferenceAgent => Agents[0];
    }
}
