using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient.Search
{
    public interface IFrontier
    {
        void Add(SAState saState);
        SAState Pop();
        bool IsEmpty();
        int Size();
        bool Contains(SAState saState);
        string GetName();
    }

    public class BFSFrontier : IFrontier
    {
        public readonly Queue<SAState> Queue = new();
        public readonly HashSet<SAState> Set = new();

        public void Add(SAState saState)
        {
            Queue.Enqueue(saState);
            Set.Add(saState);
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

        public bool Contains(SAState saState)
        {
            return Set.Contains(saState);
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

        private readonly Heuristic Heuristic;

        public BestFirstFrontier(Heuristic heuristic)
        {
            Heuristic = heuristic;
        }

        public void Add(SAState saState)
        {
            int score = Heuristic.CalculateHeuristic(saState);

            if (!Map.ContainsKey(score))
            {
                Map.Add(score, new Queue<SAState>());
            }

            Map[score].Enqueue(saState);
            Set.Add(saState);
        }

        public SAState Pop()
        {
            int minScore = Map.Keys.Min();

            SAState saState = Map[minScore].Dequeue();

            if (!Map[minScore].Any())
            {
                Map.Remove(minScore);
            }

            Set.Remove(saState);

            return saState;
        }

        public bool IsEmpty()
        {
            return !Set.Any();
        }

        public int Size()
        {
            return Set.Count;
        }

        public bool Contains(SAState saState)
        {
            return Set.Contains(saState);
        }

        public string GetName()
        {
            return "best-first search";
        }
    }
}
