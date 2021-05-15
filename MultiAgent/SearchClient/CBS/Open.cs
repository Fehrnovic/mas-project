using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiAgent.SearchClient.CBS;

namespace MultiAgent.searchClient.CBS
{
    public class Open
    {
        public Dictionary<int, Queue<Node>> OpenNodes = new();
        public int Size => OpenNodes.Values.Count;
        public bool IsEmpty => !OpenNodes.Any();

        public Node GetMinNode()
        {
            var minCost = OpenNodes.Keys.Min();

            var P = OpenNodes[minCost].Dequeue();

            if (!OpenNodes[minCost].Any())
            {
                OpenNodes.Remove(minCost);
            }

            return P;
        }

        public void AddNode(Node n)
        {
            var cost = n.Cost;
            if (!OpenNodes.ContainsKey(cost))
            {
                OpenNodes.Add(cost, new Queue<Node>());
            }

            OpenNodes[cost].Enqueue(n);
        }
    } 
}
