using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient.Search
{
    public interface IFrontier
    {
        void Add(State state);
        State Pop();
        bool IsEmpty();
        int Size();
        bool Contains(State state);
        string GetName();
    }

    public class BFSFrontier : IFrontier
    {
        public readonly Queue<State> Queue = new();
        public readonly HashSet<State> Set = new();

        public void Add(State state)
        {
            Queue.Enqueue(state);
            Set.Add(state);
        }

        public State Pop()
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

        public bool Contains(State state)
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
        public readonly Dictionary<int, Queue<State>> Map = new();
        public readonly HashSet<State> Set = new();

        private readonly Heuristic Heuristic;

        public BestFirstFrontier(Heuristic heuristic)
        {
            Heuristic = heuristic;
        }

        public void Add(State state)
        {
            int score = Heuristic.CalculateHeuristic(state);

            if (!Map.ContainsKey(score))
            {
                Map.Add(score, new Queue<State>());
            }

            Map[score].Enqueue(state);
            Set.Add(state);
        }

        public State Pop()
        {
            int minScore = Map.Keys.Min();

            State state = Map[minScore].Dequeue();

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

        public bool Contains(State state)
        {
            return Set.Contains(state);
        }

        public string GetName()
        {
            return "best-first search";
        }
    }
}
