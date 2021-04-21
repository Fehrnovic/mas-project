using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public class Node
    {
        public HashSet<IConstraint> Constraints = new();
        public Dictionary<Agent, List<(Position position, Action action)>> Solution;
        public int Cost => CalculateCost();

        private int CalculateCost()
        {
            return Solution.Values.Max(l => l.Count) + Constraints.Count;
            // return Solution.Values.Aggregate(0, (current, solution) => current + solution.Count);
            var sum = 0;
            foreach (var solution in Solution.Values)
            {
                sum += solution.Count - 1;
            }

            return sum;
        }

        public IConflict GetConflict()
        {
            // Make sure all solutions are the same length
            var maxSolutionLength = Solution.Values.Max(a => a.Count);
            var clonedSolution = CloneSolution();
            foreach (var solution in clonedSolution.Values)
            {
                if (solution.Count >= maxSolutionLength)
                {
                    continue;
                }

                var lastElement = solution[solution.Count - 1];
                var maxIterations = maxSolutionLength - solution.Count;
                for (var i = 0; i < maxIterations; i++)
                {
                    solution.Add((lastElement.position, null));
                }
            }

            // Check conflicts
            for (var time = 1; time < maxSolutionLength; time++)
            {
                for (var agent1Index = 0; agent1Index < Solution.Keys.Count; agent1Index++)
                {
                    for (var agent2Index = agent1Index + 1; agent2Index < Solution.Keys.Count; agent2Index++)
                    {
                        var (agent1, agent1Solution) = clonedSolution.ElementAt(agent1Index);
                        var (agent2, agent2Solution) = clonedSolution.ElementAt(agent2Index);

                        // Check for PositionConflict
                        if (agent1Solution[time].position == agent2Solution[time].position)
                        {
                            return new PositionConflict
                            {
                                Agent1 = agent1,
                                Agent2 = agent2,
                                Position = agent1Solution[time].position,
                                Time = time,
                            };
                        }
                        
                        // Check if Agent 1 follows Agent 2
                        if (agent1Solution[time].position == agent2Solution[time - 1].position)
                        {
                            return new FollowConflict
                            {
                                Leader = agent2,
                                Follower = agent1,
                                FollowerPosition = agent1Solution[time].position,
                                FollowerTime = time,
                            };
                        }

                        // Check if Agent 2 follows Agent 1
                        if (agent2Solution[time].position == agent1Solution[time - 1].position)
                        {
                            return new FollowConflict
                            {
                                Leader = agent1,
                                Follower = agent2,
                                FollowerPosition = agent2Solution[time].position,
                                FollowerTime = time,
                            };
                        }
                    }
                }
            }

            return null;
        }

        public Dictionary<Agent, List<(Position position, Action action)>> CloneSolution()
        {
            return Solution.ToDictionary(
                x => x.Key,
                x => x.Value.Select(tuple => tuple).ToList()
            );
        }
    }
}
