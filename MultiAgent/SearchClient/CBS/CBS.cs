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
                Solution = new Dictionary<IAgent, List<IStep>>(),
            };

            var agentToBoxGoalDictionary = new Dictionary<Agent, List<Box>>(Level.Agents.Count);
            var agentToBoxDictionary = new Dictionary<Agent, List<Box>>(Level.Agents.Count);

            foreach (var agent in Level.Agents)
            {
                agentToBoxGoalDictionary.Add(agent, new List<Box>());
                agentToBoxDictionary.Add(agent, new List<Box>());
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
                agentToBoxDictionary[closestAgent].Add(closestBox);
            }

            foreach (var agent in Level.Agents)
            {
                var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number);
                var boxesMatchingAgent = Level.Boxes.Where(b => b.Color == agent.Color).ToList();
                var state = new SAState(agent, agentGoal, agentToBoxDictionary[agent], agentToBoxGoalDictionary[agent],
                    root.Constraints);
                root.Solution[agent] = GraphSearch.Search(state, new BestFirstFrontier()).ToList();
            }

            OPEN.Add(root.Cost, new Queue<Node>(new[] { root }));
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
                        actions.Add(P.Solution[agent].Select(s => ((SAStep)s).Action).ToList());
                    }

                    return actions;
                }

                // CONFLICT!

                // Update Conflict Matrix
                var agent1 = conflict.ConflictedAgents[0];
                var agent2 = conflict.ConflictedAgents[1];

                Node.CM[agent1.ReferenceAgent.Number, agent2.ReferenceAgent.Number] += 1;
                Node.CM[agent2.ReferenceAgent.Number, agent1.ReferenceAgent.Number] += 1;

                if (Node.ShouldMerge(agent1, agent2))
                {
                    MetaAgent metaAgent = new MetaAgent();
                    switch (agent1, agent2)
                    {
                        case (Agent a1, Agent a2):
                            P.Solution.Remove(a1);
                            P.Solution.Remove(a2);

                            metaAgent.Agents.Add(a1);
                            metaAgent.Agents.Add(a2);

                            break;

                        case (Agent a1, MetaAgent ma2):
                            P.Solution.Remove(a1);
                            P.Solution.Remove(ma2);

                            metaAgent.Agents.AddRange(ma2.Agents);
                            metaAgent.Agents.Add(a1);

                            break;

                        case (MetaAgent ma1, Agent a2):
                            P.Solution.Remove(ma1);
                            P.Solution.Remove(a2);

                            metaAgent.Agents.AddRange(ma1.Agents);
                            metaAgent.Agents.Add(a2);

                            break;

                        case (MetaAgent ma1, MetaAgent ma2):
                            P.Solution.Remove(ma1);
                            P.Solution.Remove(ma2);

                            metaAgent.Agents.AddRange(ma1.Agents);
                            metaAgent.Agents.AddRange(ma2.Agents);

                            break;
                    }

                    // TODO: MAKE THIS BETTER, TO ONLY REMOVE INTERNAL CONFLICTS
                    P.Constraints = P.Constraints.Where(c => !metaAgent.Agents.Contains(c.Agent)).ToHashSet();

                    List<Agent> agents = metaAgent.Agents;
                    List<Agent> agentGoals = Level.AgentGoals
                        .Where(ag => metaAgent.Agents.Exists(a => a.Number == ag.Number)).ToList();
                    List<Box> boxes = metaAgent.Agents.SelectMany(a => agentToBoxDictionary[a]).ToList();
                    List<Box> boxGoals = metaAgent.Agents.SelectMany(a => agentToBoxGoalDictionary[a]).ToList();

                    var state = new MAState(agents, agentGoals, boxes, boxGoals, P.Constraints);
                    P.Solution.Add(metaAgent, GraphSearch.Search(state, new BestFirstFrontier())?.ToList());

                    if (P.Solution[metaAgent] != null)
                    {
                        var cost = P.Cost;

                        if (!OPEN.ContainsKey(cost))
                        {
                            OPEN.Add(cost, new Queue<Node>());
                        }

                        OPEN[cost].Enqueue(P);
                        exploredNodes.Add(P);
                    }

                    continue;
                }

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
                                Agent = conflictedAgent.ReferenceAgent,
                                Position = positionConflict.Position,
                                Time = positionConflict.Time,
                            };
                            break;

                        case FollowConflict followConflict:
                            constraint = new Constraint
                            {
                                Agent = conflictedAgent.ReferenceAgent,
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

                    var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == conflictedAgent.ReferenceAgent.Number);
                    var boxesMatchingAgent = Level.Boxes.Where(b => b.Color == conflictedAgent.ReferenceAgent.Color).ToList();

                    var state = new SAState(conflictedAgent.ReferenceAgent, agentGoal, agentToBoxDictionary[conflictedAgent.ReferenceAgent],
                        agentToBoxGoalDictionary[conflictedAgent.ReferenceAgent], A.Constraints);
                    A.Solution[conflictedAgent] = GraphSearch.Search(state, new BestFirstFrontier())?.ToList();

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
