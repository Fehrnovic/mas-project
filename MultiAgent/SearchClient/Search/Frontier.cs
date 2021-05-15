using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient.Search
{
    public interface IFrontier
    {
        void Add(SAState state);
        SAState Pop();
        bool IsEmpty();
        int Size();
        bool Contains(SAState state);
        string GetName();
    }

    public class BFSFrontier : IFrontier
    {
        public readonly Queue<SAState> Queue = new();
        public readonly HashSet<SAState> Set = new();

        public void Add(SAState state)
        {
            Queue.Enqueue(state);
            Set.Add(state);
        }

        public SAState Pop()
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

        public bool Contains(SAState state)
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
        public readonly Dictionary<int, Queue<SAState>> Map = new();
        public readonly HashSet<SAState> Set = new();

        public void Add(SAState state)
        {
            var score = Heuristic.CalculateHeuristicSA(state);

            if (!Map.ContainsKey(score))
            {
                Map.Add(score, new Queue<SAState>());
            }

            Map[score].Enqueue(state);
            Set.Add(state);
        }

        public SAState Pop()
        {
            int minScore = Map.Keys.Min();

            SAState state = Map[minScore].Dequeue();

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

        public bool Contains(SAState state)
        {
            return Set.Contains(state);
        }

        public string GetName()
        {
            return "best-first search";
        }
    }
}
