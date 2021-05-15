using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.Search;

namespace MultiAgent.SearchClient.CBS
{
    public class Node
    {
        public HashSet<IConstraint> Constraints = new();
        public Dictionary<Agent, List<SAStep>> Solution;
        public int Cost => CalculateCost();

        private int CalculateCost()
        {
            return Solution.Values.Sum(l => l.Sum(s => s.ActionCount));
        }

        public bool InvokeLowLevelSearch(Agent agent, SAState state)
        {
            var agentSolution = GraphSearch.Search(state, new BestFirstFrontier())?.ToList();
            if (agentSolution == null)
            {
                return false;
            }

            Solution[agent] = agentSolution;

            return true;
        }

        public Dictionary<Agent, List<SAStep>> ExtractMoves()
        {
            // TODO: Convert all MASteps to SASteps
            // shouldMerge == false no MASteps are created. But needed for merging

            var agentMoves = new Dictionary<Agent, List<SAStep>>(Level.Agents.Count);
            foreach (var (agent, steps) in Solution)
            {
                agentMoves.Add(agent, steps.ToList());
            }

            return agentMoves;
        }

        public List<IConflict> GetAllConflicts(Dictionary<Agent, bool> finishedAgents)
        {
            var conflicts = new List<IConflict>();

            var maxSolutionLength = Solution.Values.Max(a => a.Count);
            var clonedSolution = CloneSolution();

            // For all finished agents, make their solution the same length as the longest.
            foreach (var (agent, value) in clonedSolution.Where(k => finishedAgents[k.Key]))
            {
                if (value.Count >= maxSolutionLength)
                {
                    continue;
                }

                var lastElement = value.Last();
                var maxIterations = maxSolutionLength - value.Count;
                for (var i = 0; i < maxIterations; i++)
                {
                    value.Add(new SAStep(lastElement.Positions, null));
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

                        // Check that the solutions has the time. Otherwise continue (future events should not count as conflicts)
                        if (time >= agent1Solution.Count || time >= agent2Solution.Count)
                        {
                            break;
                        }

                        // Check for PositionConflict
                        var conflictingPositions = agent1Solution[time].Positions
                            .Intersect(agent2Solution[time].Positions)
                            .ToList();

                        if (conflictingPositions.Any())
                        {
                            conflicts.Add(new PositionConflict
                            {
                                Agent1 = agent1,
                                Agent2 = agent2,
                                Position = conflictingPositions.First(),
                                Time = time,
                            });
                        }

                        // Check if Agent 1 follows Agent 2
                        conflictingPositions = agent1Solution[time].Positions
                            .Intersect(agent2Solution[time - 1].Positions)
                            .ToList();
                        if (conflictingPositions.Any())
                        {
                            conflicts.Add(new FollowConflict
                            {
                                Leader = agent2,
                                Follower = agent1,
                                FollowerPosition = conflictingPositions.First(),
                                FollowerTime = time,
                            });
                        }

                        // Check if Agent 2 follows Agent 1
                        conflictingPositions = agent2Solution[time].Positions
                            .Intersect(agent1Solution[time - 1].Positions)
                            .ToList();
                        if (conflictingPositions.Any())
                        {
                            conflicts.Add(new FollowConflict
                            {
                                Leader = agent1,
                                Follower = agent2,
                                FollowerPosition = conflictingPositions.First(),
                                FollowerTime = time,
                            });
                        }
                    }
                }
            }

            return conflicts;
        }

        public Dictionary<Agent, List<SAStep>> CloneSolution()
        {
            return Solution.ToDictionary(
                x => x.Key,
                x => x.Value.Select(tuple => tuple).ToList()
            );
        }
    }
}
