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
            var OPEN = new List<Node>();

            var root = new Node
            {
                Solution = new Dictionary<Agent, (int, Action, Position)[]>()
            };

            var agentToBoxGoalDictionary = new Dictionary<Agent, List<Box>>();

            var usedBoxes = new List<Box>();

            foreach (var boxGoal in Level.BoxGoals)
            {
                // Find the closest box to the goal sharing the same letter that hasn't been delegated yet
                Box closestBox = Level.Boxes
                    .Where(b => b.Letter == boxGoal.Letter && !usedBoxes.Contains(b))
                    .Aggregate(
                        (currentlyClosestBox, box) =>
                            currentlyClosestBox == null ||
                            (Level.DistanceBetweenPositions[(currentlyClosestBox.Position, boxGoal.Position)] >
                             Level.DistanceBetweenPositions[(box.Position, boxGoal.Position)])
                                ? box
                                : currentlyClosestBox);

                usedBoxes.Add(closestBox);

                // Find the agent closest to the box just found
                Agent closestAgent = Level.Agents
                    .Where(a => closestBox.Color.Equals(a.Color))
                    .Aggregate((currentlyClosestAgent, agent)
                        => currentlyClosestAgent == null ||
                           (Level.DistanceBetweenPositions[(currentlyClosestAgent.Position, closestBox.Position)]
                            > Level.DistanceBetweenPositions[(agent.Position, closestBox.Position)])
                            ? agent
                            : currentlyClosestAgent);

                if (!agentToBoxGoalDictionary.ContainsKey(closestAgent))
                {
                    agentToBoxGoalDictionary.Add(closestAgent, new List<Box>());
                }

                agentToBoxGoalDictionary[closestAgent].Add(boxGoal);
            }

            foreach (var agent in Level.Agents)
            {
                var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number);
                root.Solution[agent] = GraphSearch.Search(new State(agent, agentGoal), new BFSFrontier());
            }

            OPEN.Add(root);

            while (OPEN.Any())
            {
                var P = OPEN.OrderBy(n => n.Cost).ToList().First();
                OPEN.Remove(P);

                var conflict = P.HasConflict();

                if (conflict == null)
                {
                    var actions = new List<List<Action>>();
                    foreach (var agent in Level.Agents)
                    {
                        actions.Add(P.Solution[agent].Select(e => e.Item2).ToList());
                    }

                    return actions;
                }

                // CONFLICT!

                foreach (var agent in conflict.ConflictedAgents)
                {
                    var A = new Node();

                    var constraint = agent.Number == 0
                        ? new Constraint(agent, new Position(conflict.PositionFrom),
                            new Position(conflict.PositionTo), conflict.Depth)
                        : new Constraint(agent, new Position(conflict.PositionTo),
                            new Position(conflict.PositionFrom), conflict.Depth);

                    P.Constraints.ForEach((c) =>
                    {
                        var copiedAgent = new Agent(c.Agent.Number, c.Agent.Color, new Position(c.PositionFrom));
                        var copiedConstraint = new Constraint(copiedAgent, new Position(c.PositionFrom),
                            new Position(c.PositionTo), c.Depth);
                        A.Constraints.Add(copiedConstraint);
                    });

                    if (!A.Constraints.Any(c => agent.Number == c.Agent.Number
                                                && constraint.PositionFrom.Equals(c.PositionFrom)
                                                && constraint.PositionTo.Equals(c.PositionTo)
                                                && constraint.Depth == c.Depth))
                    {
                        A.Constraints.Add(constraint);
                    }

                    A.Solution = Extensions.CloneDictionary(P.Solution);

                    // Change to supply proper state
                    var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number);

                    A.UpdateSolution(new State(agent, agentGoal));

                    if (A.Solution[agent] != null)
                    {
                        OPEN.Add(A);
                    }
                }
            }

            return null;
        }
    }

    static class Extensions
    {
        public static IList<T> CloneList<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T) item.Clone()).ToList();
        }

        public static Dictionary<TKey, TValue> CloneDictionary<TKey, TValue>
            (Dictionary<TKey, TValue> original) where TValue : ICloneable
        {
            Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(original.Count,
                original.Comparer);
            foreach (KeyValuePair<TKey, TValue> entry in original)
            {
                ret.Add(entry.Key, (TValue) entry.Value.Clone());
            }

            return ret;
        }
    }
}
