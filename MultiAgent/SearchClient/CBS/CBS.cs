using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                Constraints = new HashSet<IConstraint>(),
                Solution = new Dictionary<IAgent, List<IStep>>(),
            };

            foreach (var agent in Level.Agents)
            {
                var agentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number);
                var state = new SAState(agent, agentGoal, LevelDelegationHelper.LevelDelegation.AgentToBoxes[agent],
                    LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[agent],
                    root.Constraints);
                root.Solution[agent] =
                    GraphSearch.Search(state, new BestFirstFrontier())?.ToList();
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
                    return ExtractMoves(P);
                }

                // CONFLICT!

                // Update Conflict Matrix
                var agent1 = conflict.ConflictedAgents[0];
                var agent2 = conflict.ConflictedAgents[1];

                Node.CM[agent1.ReferenceAgent.Number, agent2.ReferenceAgent.Number] += 1;
                Node.CM[agent2.ReferenceAgent.Number, agent1.ReferenceAgent.Number] += 1;

                if (Node.ShouldMerge(agent1, agent2))
                {
                    var metaAgent = new MetaAgent();
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

                    Console.Error.WriteLine($"Merging agent: {agent1} and {agent2} and created metaagent: {metaAgent}");

                    // TODO: MAKE THIS BETTER, TO ONLY REMOVE INTERNAL CONFLICTS
                    P.Constraints = P.Constraints.Where(c => !metaAgent.Agents.Contains(c.Agent)).ToHashSet();

                    List<Agent> agents = metaAgent.Agents;
                    List<Agent> agentGoals = Level.AgentGoals
                        .Where(ag => metaAgent.Agents.Exists(a => a.Number == ag.Number)).ToList();
                    List<Box> boxes = metaAgent.Agents
                        .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxes[a]).ToList();
                    List<Box> boxGoals = metaAgent.Agents
                        .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[a]).ToList();

                    var state = new MAState(agents, agentGoals, boxes, boxGoals, P.Constraints);
                    P.Solution.Add(metaAgent, GraphSearch.Search(state, new BestFirstFrontier())?.ToList());

                    if (P.Solution[metaAgent] != null)
                    {
                        if (P.GetConflict() == null)
                        {
                            return ExtractMoves(P);
                        }

                        var cost = P.Cost;

                        if (!OPEN.ContainsKey(cost))
                        {
                            OPEN.Add(cost, new Queue<Node>());
                        }

                        OPEN[cost].Enqueue(P);
                        exploredNodes.Add(P);
                    }
                    else
                    {
                        // Find agents defined in level (except agents already in this meta agent)
                        // with the same color as the ones in the meta agent
                        var agentsAbleToMerge = Level.Agents.Except(metaAgent.Agents)
                            .Where(a => metaAgent.Agents.Exists(ma => ma.Color == a.Color)).ToList();

                        // While there exists agents able to merge and we still can't solve level
                        while (agentsAbleToMerge.Any())
                        {
                            var mergeAgent = agentsAbleToMerge.First();

                            P.Solution.Remove(mergeAgent);
                            P.Solution.Remove(metaAgent);
                            metaAgent.Agents.Add(mergeAgent);

                            agents = metaAgent.Agents;
                            agentGoals = Level.AgentGoals
                                .Where(ag => metaAgent.Agents.Exists(a => a.Number == ag.Number)).ToList();
                            boxes = metaAgent.Agents
                                .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxes[a]).ToList();
                            boxGoals = metaAgent.Agents
                                .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[a]).ToList();

                            state = new MAState(agents, agentGoals, boxes, boxGoals, P.Constraints);
                            P.Solution.Add(metaAgent, GraphSearch.Search(state, new BestFirstFrontier())?.ToList());

                            if (P.Solution[metaAgent] != null)
                            {
                                if (P.GetConflict() == null)
                                {
                                    return ExtractMoves(P);
                                }

                                var cost = P.Cost;

                                if (!OPEN.ContainsKey(cost))
                                {
                                    OPEN.Add(cost, new Queue<Node>());
                                }

                                OPEN[cost].Enqueue(P);
                                exploredNodes.Add(P);

                                break;
                            }

                            agentsAbleToMerge = Level.Agents.Except(metaAgent.Agents)
                                .Where(a => metaAgent.Agents.Exists(ma => ma.Color == a.Color)).ToList();
                        }
                    }

                    continue;
                }

                foreach (var conflictedAgent in conflict.ConflictedAgents)
                {
                    var A = new Node();
                    A.Constraints = new HashSet<IConstraint>(P.Constraints);

                    var constraints = new List<IConstraint>();
                    HashSet<Position> corridor;
                    switch (conflict)
                    {
                        case PositionConflict positionConflict:
                            corridor = CorridorHelper.CorridorOfPosition(positionConflict.Position);
                            if (corridor != null)
                            {
                                // Find other agent
                                var otherAgent = conflict.ConflictedAgents[0] == conflictedAgent
                                    ? conflict.ConflictedAgents[1]
                                    : conflict.ConflictedAgents[0];

                                // Find time other agent is still in the corridor
                                var timeCounter = positionConflict.Time;
                                while (timeCounter < P.Solution[otherAgent].Count
                                       && P.Solution[otherAgent][timeCounter].Positions
                                           .Exists(p => corridor.Contains(p)))
                                {
                                    timeCounter++;
                                }

                                // Add constraints for agent saying it cannot go into corridor while the other agent is still there
                                var maxTime = timeCounter;

                                // Find entry time for agent
                                timeCounter = positionConflict.Time;
                                while (timeCounter > 0
                                       && P.Solution[conflictedAgent][timeCounter].Positions
                                           .Exists(p => corridor.Contains(p)))
                                {
                                    timeCounter--;
                                }

                                // Add constraint saying the agent should not enter while the other agent is still in there
                                var minTime = timeCounter;

                                constraints.Add(new CorridorConstraint
                                {
                                    Agent = conflictedAgent.ReferenceAgent,
                                    CorridorPositions = corridor.ToList(),
                                    Time = (minTime, maxTime),
                                });
                            }
                            else
                            {
                                constraints.Add(new Constraint
                                {
                                    Agent = conflictedAgent.ReferenceAgent,
                                    Position = positionConflict.Position,
                                    Time = positionConflict.Time,
                                });
                            }

                            break;

                        case FollowConflict followConflict:
                            var constraint = new Constraint
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

                            constraints.Add(constraint);

                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(conflict));
                    }

                    foreach (var constraint in constraints)
                    {
                        A.Constraints.Add(constraint);
                    }

                    A.Solution = P.CloneSolution();

                    IState state;

                    if (conflictedAgent is MetaAgent ma)
                    {
                        var agents = ma.Agents;
                        var agentGoals = Level.AgentGoals
                            .Where(ag => ma.Agents.Exists(a => a.Number == ag.Number)).ToList();
                        var boxes = ma.Agents
                            .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxes[a]).ToList();
                        var boxGoals = ma.Agents
                            .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[a]).ToList();

                        state = new MAState(agents, agentGoals, boxes, boxGoals, A.Constraints);
                    }
                    else
                    {
                        var agentGoal =
                            Level.AgentGoals.FirstOrDefault(ag => ag.Number == conflictedAgent.ReferenceAgent.Number);

                        state = new SAState(conflictedAgent.ReferenceAgent, agentGoal,
                            LevelDelegationHelper.LevelDelegation.AgentToBoxes[conflictedAgent.ReferenceAgent],
                            LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[conflictedAgent.ReferenceAgent],
                            A.Constraints);
                    }

                    A.Solution[conflictedAgent] =
                        GraphSearch.Search(state, new BestFirstFrontier())?.ToList();

                    // Agent found a solution
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
                    // Agent did not find a solution
                    // else
                    // {
                    //     // Find agents defined in level (except agents already in this meta agent)
                    //     // with the same color as the ones in the meta agent
                    //     var agentsAbleToMerge = Level.Agents.Except(conflictedAgent.Agents)
                    //         .Where(a => conflictedAgent.Agents.Exists(ma => ma.Color == a.Color)).ToList();
                    //
                    //     // While there exists agents able to merge and we still can't solve level
                    //     while (agentsAbleToMerge.Any())
                    //     {
                    //         var mergeAgent = agentsAbleToMerge.First();
                    //
                    //         P.Solution.Remove(mergeAgent);
                    //         P.Solution.Remove(conflictedAgent);
                    //
                    //         var metaAgent = new MetaAgent();
                    //         metaAgent.Agents.Add(mergeAgent);
                    //         metaAgent.Agents.AddRange(conflictedAgent.Agents);
                    //
                    //         var agents = metaAgent.Agents;
                    //         var agentGoals = Level.AgentGoals
                    //             .Where(ag => metaAgent.Agents.Exists(a => a.Number == ag.Number)).ToList();
                    //         var boxes = metaAgent.Agents
                    //             .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxes[a]).ToList();
                    //         var boxGoals = metaAgent.Agents
                    //             .SelectMany(a => LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[a]).ToList();
                    //
                    //         var mergeState = new MAState(agents, agentGoals, boxes, boxGoals, P.Constraints);
                    //         P.Solution.Add(metaAgent,
                    //             GraphSearch.Search(mergeState, new BestFirstFrontier())?.ToList());
                    //
                    //         if (P.Solution[metaAgent] != null)
                    //         {
                    //             // if (P.GetConflict() == null)
                    //             // {
                    //             //     return ExtractMoves(P);
                    //             // }
                    //
                    //             var cost = P.Cost;
                    //
                    //             if (!OPEN.ContainsKey(cost))
                    //             {
                    //                 OPEN.Add(cost, new Queue<Node>());
                    //             }
                    //
                    //             OPEN[cost].Enqueue(P);
                    //             exploredNodes.Add(P);
                    //
                    //             break;
                    //         }
                    //
                    //         agentsAbleToMerge = Level.Agents.Except(metaAgent.Agents)
                    //             .Where(a => metaAgent.Agents.Exists(ma => ma.Color == a.Color)).ToList();
                    //     }
                    // }
                }
            }

            return null;
        }

        private static List<List<Action>> ExtractMoves(Node node)
        {
            List<Action>[] actionsArray = new List<Action>[Level.Agents.Count];

            foreach (var (iAgent, plan) in node.Solution)
            {
                if (iAgent is MetaAgent ma)
                {
                    foreach (var agent in ma.Agents)
                    {
                        actionsArray[agent.Number] =
                            plan.Select(s => ((MAStep) s).JointActions?[agent])?.ToList();
                    }
                }

                if (iAgent is Agent a)
                {
                    actionsArray[iAgent.ReferenceAgent.Number] = plan.Select(s => ((SAStep) s).Action).ToList();
                }
            }

            return actionsArray.ToList();
        }
    }
}
