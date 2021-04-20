using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public class Node
    {
        public HashSet<Constraint> Constraints = new();
        public Dictionary<Agent, List<(Position position, Action action)>> Solution;
        public int Cost => CalculateCost();

        private int CalculateCost()
        {
            return Solution.Values.Aggregate(0, (current, solution) => current + solution.Count);
        }
    }
}
