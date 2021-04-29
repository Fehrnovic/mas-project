using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.searchClient.Search;

namespace MultiAgent.SearchClient.Search
{
    public interface IFrontier
    {
        void Add(IState state);
        IState Pop();
        bool IsEmpty();
        int Size();
        bool Contains(IState state);
        string GetName();
    }

    public class BFSFrontier : IFrontier
    {
        public readonly Queue<IState> Queue = new();
        public readonly HashSet<IState> Set = new();

        public void Add(IState state)
        {
            Queue.Enqueue(state);
            Set.Add(state);
        }

        public IState Pop()
        {
            var state = Queue.Dequeue();
            Set.Remove(state);

            return state;
        }

        public bool IsEmpty()
        {
            return !Queue.Any();
        }

        public int Size()
        {
            return Queue.Count;
        }

        public bool Contains(IState state)
        {
            return Set.Contains(state);
        }

        public string GetName()
        {
            return "breadth-first search";
        }
    }


    public class BestFirstFrontier : IFrontier
    {
        public readonly Dictionary<int, Queue<IState>> Map = new();
        public readonly HashSet<IState> Set = new();

        public void Add(IState state)
        {
            var score = state switch
            {
                SAState s => Heuristic.CalculateHeuristicSA(s),
                MAState s => Heuristic.CalculateHeuristicMA(s),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!Map.ContainsKey(score))
            {
                Map.Add(score, new Queue<IState>());
            }

            Map[score].Enqueue(state);
            Set.Add(state);
        }

        public IState Pop()
        {
            int minScore = Map.Keys.Min();

            IState state = Map[minScore].Dequeue();

            if (!Map[minScore].Any())
            {
                Map.Remove(minScore);
            }

            Set.Remove(state);

            return state;
        }

        public bool IsEmpty()
        {
            return !Set.Any();
        }

        public int Size()
        {
            return Set.Count;
        }

        public bool Contains(IState state)
        {
            return Set.Contains(state);
        }

        public string GetName()
        {
            return "best-first search";
        }
    }
}
