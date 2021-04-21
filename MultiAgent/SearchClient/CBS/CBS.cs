using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.Search;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public static class CBS
    {
        public static List<List<Action>> Run()
        {
            var OPEN = new Dictionary<int, Queue<Node>>();
            var exploredNodes = new HashSet<Node>();

            var root = new Node
            {
                Constraints = new HashSet<IConstraint>(),
                Solution = new Dictionary<Agent, List<(Position, Action)>>(),
            };

            // var agentToBoxGoalDictionary = new Dictionary<Agent, List<Box>>();
            //
            // var usedBoxes = new List<Box>();
            //
            // foreach (var boxGoal in Level.BoxGoals)
            // {
            //     // Find the closest box to the goal sharing the same letter that hasn't been delegated yet
            //     Box closestBox = Level.Boxes
            //         .Where(b => b.Letter == boxGoal.Letter && !usedBoxes.Contains(b))
            //         .Aggregate(
            //             (currentlyClosestBox, box) =>
            //                 currentlyClosestBox == null ||
            //                 (Level.DistanceBetweenPositions[(currentlyClosestBox.Position, boxGoal.Position)] >
            //                  Level.DistanceBetweenPositions[(box.Position, boxGoal.Position)])
            //                     ? box
            //                     : currentlyClosestBox);
            //
            //     usedBoxes.Add(closestBox);
            //
            //     // Find the agent closest to the box just found
            //     Agent closestAgent = Level.Agents
            //         .Where(a => closestBox.Color.Equals(a.Color))
            //         .Aggregate((currentlyClosestAgent, agent)
            //             => currentlyClosestAgent == null ||
            //                (Level.DistanceBetweenPositions[(currentlyClosestAgent.Position, closestBox.Position)]
            //                 > Level.DistanceBetweenPositions[(agent.Position, closestBox.Position)])
            //                 ? agent
            //                 : currentlyClosestAgent);
            //
            //     if (!agentToBoxGoalDictionary.ContainsKey(closestAgent))
            //     {
            //         agentToBoxGoalDictionary.Add(closestAgent, new List<Box>());
            //     }
            //
            //     agentToBoxGoalDictionary[closestAgent].Add(boxGoal);
            // }

            foreach (var agent in Level.Agents)
            {
                var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number);
                root.Solution[agent] =
                    GraphSearch.Search(new State(agent, agentGoal, new List<Box>(), root.Constraints),
                        new BFSFrontier());
            }

            OPEN.Add(root.Cost, new Queue<Node>(new[] {root}));
            exploredNodes.Add(root);

            while (OPEN.Any())
            {
                var minCost = OPEN.Keys.Min();

                var P = OPEN[minCost].Dequeue();

                if (!OPEN[minCost].Any())
                {
                    OPEN.Remove(minCost);
                }

                var conflict = P.GetConflict();
                if (conflict == null)
                {
                    var actions = new List<List<Action>>();
                    foreach (var agent in Level.Agents.OrderBy(a => a.Number))
                    {
                        actions.Add(P.Solution[agent].Select(e => e.action).ToList());
                    }

                    return actions;
                }

                // CONFLICT!
                foreach (var conflictedAgent in conflict.ConflictedAgents)
                {
                    var A = new Node();
                    A.Constraints = new HashSet<IConstraint>(P.Constraints);

                    IConstraint constraint;
                    switch (conflict)
                    {
                        case PositionConflict positionConflict:
                            constraint = new AgentConstraint
                            {
                                Agent = conflictedAgent,
                                Position = positionConflict.Position,
                                Time = positionConflict.Time,
                            };
                            break;

                        case FollowConflict followConflict:
                            constraint = new AgentConstraint
                            {
                                Agent = conflictedAgent,
                                Position = followConflict.FollowerPosition,
                                Time = conflictedAgent == followConflict.Follower
                                    ? followConflict.FollowerTime
                                    : followConflict.FollowerTime - 1,
                            };

                            if (constraint.Time == 0)
                            {
                                continue;
                            }

                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(conflict));
                    }

                    A.Constraints.Add(constraint);
                    A.Solution = P.CloneSolution();

                    var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == conflictedAgent.Number);
                    var state = new State(conflictedAgent, agentGoal, new List<Box>(), A.Constraints);
                    A.Solution[conflictedAgent] = GraphSearch.Search(state, new BFSFrontier());

                    if (A.Solution[conflictedAgent] != null)
                    {
                        var cost = A.Cost;

                        if (!OPEN.ContainsKey(cost))
                        {
                            OPEN.Add(cost, new Queue<Node>());
                        }

                        OPEN[cost].Enqueue(A);
                        exploredNodes.Add(A);
                    }
                }
            }

            return null;
        }
    }
}
