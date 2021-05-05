using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using MultiAgent.SearchClient.Search;

namespace MultiAgent.SearchClient.CBS
{
    public class Node
    {
        public HashSet<Constraint> Constraints = new();
        public Dictionary<IAgent, List<IStep>> Solution;
        public int Cost => CalculateCost();
        public static int[,] CM = new int[Level.Agents.Count, Level.Agents.Count];
        public static readonly int B = 10;

        private int CalculateCost()
        {
            int bonus = Solution.Keys.Sum(agent => agent is MetaAgent ? agent.Agents.Count : 0);

            return Solution.Values.Max(l => l.Count) + Constraints.Count - bonus;
            // return Solution.Values.Aggregate(0, (current, solution) => current + solution.Count);
            //var sum = 0;
            //foreach (var solution in Solution.Values)
            //{
            //    sum += solution.Count - 1;
            //}

            //return sum;
        }

        public static bool ShouldMerge(IAgent agent1, IAgent agent2)
        {
            return false;
            return CM[agent1.ReferenceAgent.Number, agent2.ReferenceAgent.Number] > B;
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
                    solution.Add(new SAStep(lastElement.Positions, null));
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

                        var conflictingPositions = agent1Solution[time].Positions
                            .Intersect(agent2Solution[time].Positions)
                            .ToList();

                        if (conflictingPositions.Any())
                        {
                            return new PositionConflict
                            {
                                Agent1 = agent1,
                                Agent2 = agent2,
                                Position = conflictingPositions.First(),
                                Time = time,
                            };
                        }

                        // Check if Agent 1 follows Agent 2
                        conflictingPositions = agent1Solution[time].Positions
                            .Intersect(agent2Solution[time - 1].Positions)
                            .ToList();
                        if (conflictingPositions.Any())
                        {
                            return new FollowConflict
                            {
                                Leader = agent2,
                                Follower = agent1,
                                FollowerPosition = conflictingPositions.First(),
                                FollowerTime = time,
                            };
                        }

                        // Check if Agent 2 follows Agent 1
                        conflictingPositions = agent2Solution[time].Positions
                            .Intersect(agent1Solution[time - 1].Positions)
                            .ToList();
                        if (conflictingPositions.Any())
                        {
                            return new FollowConflict
                            {
                                Leader = agent1,
                                Follower = agent2,
                                FollowerPosition = conflictingPositions.First(),
                                FollowerTime = time,
                            };
                        }
                    }
                }
            }

            return null;
        }

        public Dictionary<IAgent, List<IStep>> CloneSolution()
        {
            return Solution.ToDictionary(
                x => x.Key,
                x => x.Value.Select(tuple => tuple).ToList()
            );
        }
    }
}
