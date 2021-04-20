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
}
