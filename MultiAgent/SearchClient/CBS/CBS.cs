using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MultiAgent.SearchClient.Search;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public static class CBS
    {
        public static int Counter = 0;

        public static List<List<Action>> Run()
        {
            var timer = new Stopwatch();
            timer.Start();

            var OPEN = new Dictionary<int, Queue<Node>>();
            var exploredNodes = new HashSet<Node>();

            var root = new Node
            {
                Constraints = new HashSet<Constraint>(),
                Solution = new Dictionary<Agent, List<Step>>(),
            };

            var agentToBoxGoalDictionary = new Dictionary<Agent, List<Box>>(Level.Agents.Count);
            foreach (var agent in Level.Agents)
            {
                agentToBoxGoalDictionary.Add(agent, new List<Box>());
            }

            var usedBoxes = new List<Box>();
            foreach (var boxGoal in Level.BoxGoals)
            {
                // Find the closest box to the goal sharing the same letter that hasn't been delegated yet
                Box closestBox = Level.Boxes
                    .Where(b => b.Letter == boxGoal.Letter && !usedBoxes.Contains(b))
                    .Aggregate(
                        (currentlyClosestBox, box) =>
                            currentlyClosestBox == null ||
                            (Level.DistanceBetweenPositions[
                                 (currentlyClosestBox.GetInitialLocation(), boxGoal.GetInitialLocation())] >
                             Level.DistanceBetweenPositions[(box.GetInitialLocation(), boxGoal.GetInitialLocation())])
                                ? box
                                : currentlyClosestBox);

                usedBoxes.Add(closestBox);

                // Find the agent closest to the box just found
                Agent closestAgent = Level.Agents
                    .Where(a => closestBox.Color.Equals(a.Color))
                    .Aggregate((currentlyClosestAgent, agent)
                        => currentlyClosestAgent == null ||
                           (Level.DistanceBetweenPositions[
                                (currentlyClosestAgent.GetInitialLocation(), closestBox.GetInitialLocation())]
                            > Level.DistanceBetweenPositions[
                                (agent.GetInitialLocation(), closestBox.GetInitialLocation())])
                            ? agent
                            : currentlyClosestAgent);

                agentToBoxGoalDictionary[closestAgent].Add(boxGoal);
            }

            foreach (var agent in Level.Agents)
            {
                var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number);
                var boxesMatchingAgent = Level.Boxes.Where(b => b.Color == agent.Color).ToList();
                var state = new State(agent, agentGoal, boxesMatchingAgent, agentToBoxGoalDictionary[agent],
                    root.Constraints);
                root.Solution[agent] =
                    GraphSearch.Search(state, new BestFirstFrontier(new Heuristic(state)));
            }

            OPEN.Add(root.Cost, new Queue<Node>(new[] {root}));
            exploredNodes.Add(root);

            while (OPEN.Any())
            {
                if (++Counter % 100 == 0)
                {
                    Console.Error.WriteLine(
                        $"OPEN has size : {OPEN.Values.Count}. Time spent: {timer.ElapsedMilliseconds / 1000.0} s");
                }

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
                        actions.Add(P.Solution[agent].Select(s => s.Action).ToList());
                    }

                    return actions;
                }

                // CONFLICT!
                foreach (var conflictedAgent in conflict.ConflictedAgents)
                {
                    var A = new Node();
                    A.Constraints = new HashSet<Constraint>(P.Constraints);

                    Constraint constraint;
                    switch (conflict)
                    {
                        case PositionConflict positionConflict:
                            constraint = new Constraint
                            {
                                Agent = conflictedAgent,
                                Position = positionConflict.Position,
                                Time = positionConflict.Time,
                            };
                            break;

                        case FollowConflict followConflict:
                            constraint = new Constraint
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
                    var boxesMatchingAgent = Level.Boxes.Where(b => b.Color == conflictedAgent.Color).ToList();

                    var state = new State(conflictedAgent, agentGoal, boxesMatchingAgent,
                        agentToBoxGoalDictionary[conflictedAgent], A.Constraints);
                    A.Solution[conflictedAgent] =
                        GraphSearch.Search(state, new BestFirstFrontier(new Heuristic(state)));

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
