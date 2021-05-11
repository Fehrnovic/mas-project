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

        public static Dictionary<Agent, List<SAStep>> Run(Dictionary<Agent, SAState> delegation,
            Dictionary<Agent, bool> finishedAgents)
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
                if (Program.ShouldPrint >= 5)
                {
                    Console.Error.Write(agent.Number);
                }

                root.InvokeLowLevelSearch(agent, delegation[agent]);
            }

            OPEN.Add(root.Cost, new Queue<Node>(new[] {root}));
            exploredNodes.Add(root);

            while (OPEN.Any())
            {
                if (++Counter % 100 == 0)
                {
                    if (Program.ShouldPrint >= 3)
                    {
                        Console.Error.WriteLine(
                            $"OPEN has size : {OPEN.Values.Count}. Time spent: {timer.ElapsedMilliseconds / 1000.0} s");
                    }
                }

                var minCost = OPEN.Keys.Min();

                var P = OPEN[minCost].Dequeue();

                if (!OPEN[minCost].Any())
                {
                    OPEN.Remove(minCost);
                }

                var conflict = P.GetConflict(finishedAgents);
                if (conflict == null)
                {
                    if (Program.ShouldPrint >= 5)
                    {
                        Console.Error.WriteLine();
                    }

                    return P.ExtractMoves();
                }

                // CONFLICT!
                if (Program.ShouldPrint >= 5)
                {
                    Console.Error.Write('.');
                }

                // Update Conflict Matrix
                var agent1 = conflict.ConflictedAgents[0];
                var agent2 = conflict.ConflictedAgents[1];

                Node.CM[agent1.ReferenceAgent.Number, agent2.ReferenceAgent.Number] += 1;
                Node.CM[agent2.ReferenceAgent.Number, agent1.ReferenceAgent.Number] += 1;

                if (Node.ShouldMerge(agent1, agent2))
                {
                    var metaAgent = CreateMetaAgent(agent1, agent2, P);

                    // Remove constraints from internal conflicts
                    P.RemoveInternalConstraints(metaAgent);

                    var state = CreateMAState(metaAgent, delegation, P.Constraints);

                    var foundSolution = P.InvokeLowLevelSearch(metaAgent, state);

                    if (foundSolution)
                    {
                        if (P.GetConflict(finishedAgents) == null)
                        {
                            return P.ExtractMoves();
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

                            state = CreateMAState(metaAgent, delegation, P.Constraints);

                            foundSolution = P.InvokeLowLevelSearch(metaAgent, state);

                            if (foundSolution)
                            {
                                if (P.GetConflict(finishedAgents) == null)
                                {
                                    return P.ExtractMoves();
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
                    var A = new Node {Constraints = new HashSet<IConstraint>(P.Constraints)};

                    var constraint = CreateConstraint(conflictedAgent, conflict, P);
                    if (constraint == null)
                    {
                        continue;
                    }

                    if (Program.ShouldPrint >= 5)
                    {
                        Console.Error.Write(constraint is CorridorConstraint ? 'C' : 'P');
                    }

                    A.Constraints.Add(constraint);
                    A.Solution = P.CloneSolution();

                    IState state;

                    if (conflictedAgent is MetaAgent ma)
                    {
                        state = CreateMAState(ma, delegation, A.Constraints);
                    }
                    else
                    {
                        state = CreateSAState(conflictedAgent.ReferenceAgent,
                            delegation[conflictedAgent.ReferenceAgent], A.Constraints);
                    }

                    var foundSolution = A.InvokeLowLevelSearch(conflictedAgent, state);

                    // Agent found a solution
                    if (foundSolution)
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
                    //     // TODO: FIX MERGING FOR SUB-GOALS
                    //
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
                    //             if (P.GetConflict(finishedAgents) == null)
                    //             {
                    //                 return ExtractMoves(P);
                    //             }
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

        public static SAState CreateSAState(Agent agent, SAState previousState, HashSet<IConstraint> constraints)
        {
            var state = new SAState(agent, previousState.AgentPosition, previousState.AgentGoal,
                previousState.PositionsOfBoxes, previousState.BoxGoals, constraints);

            state.RelevantBoxToSolveGoal = previousState.RelevantBoxToSolveGoal;
            state.CurrentBoxGoal = previousState.CurrentBoxGoal;
            return state;
        }

        public static MAState CreateMAState(MetaAgent ma, Dictionary<Agent, SAState> delegation,
            HashSet<IConstraint> constraints)
        {
            var agents = new Dictionary<Agent, Position>();
            var agentGoals = new List<Agent>();
            var boxes = new Dictionary<Position, Box>();
            var boxGoals = new List<Box>();
            var agentToRelevantBox = new Dictionary<Agent, Box>();
            var agentToRelevantGoal = new Dictionary<Agent, Box>();
            foreach (var agent in ma.Agents)
            {
                var previousState = delegation[agent];

                agentToRelevantGoal.Add(agent, previousState.CurrentBoxGoal);
                agentToRelevantBox.Add(agent, previousState.RelevantBoxToSolveGoal);

                agents.Add(agent, previousState.AgentPosition);
                if (previousState.AgentGoal != null)
                {
                    agentGoals.Add(previousState.AgentGoal);
                }

                foreach (var previousStateBoxGoal in previousState.BoxGoals)
                {
                    boxGoals.Add(previousStateBoxGoal);
                }

                foreach (var (position, box) in previousState.PositionsOfBoxes)
                {
                    boxes.Add(position, box);
                }
            }

            return new MAState(agents, agentGoals, boxes, boxGoals, constraints, agentToRelevantGoal,
                agentToRelevantBox);
        }

        private static MetaAgent CreateMetaAgent(IAgent agent1, IAgent agent2, Node P)
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

            Console.Error.WriteLine($"Merging agent: {agent1} and {agent2} and created metaagent: {metaAgent}");

            return metaAgent;
        }

        private static IConstraint CreateConstraint(IAgent conflictedAgent, IConflict conflict, Node parentNode)
        {
            IConstraint constraint;
            switch (conflict)
            {
                case PositionConflict positionConflict:
                    HashSet<Position> corridor = CorridorHelper.CorridorOfPosition(positionConflict.Position);
                    if (corridor != null)
                    {
                        // Find other agent
                        var otherAgent = conflict.ConflictedAgents[0] == conflictedAgent
                            ? conflict.ConflictedAgents[1]
                            : conflict.ConflictedAgents[0];

                        // Find time other agent is still in the corridor
                        var timeCounter = positionConflict.Time;
                        while (timeCounter < parentNode.Solution[otherAgent].Count
                               && parentNode.Solution[otherAgent][timeCounter].Positions
                                   .Exists(p => corridor.Contains(p)))
                        {
                            timeCounter++;
                        }

                        // Add constraints for agent saying it cannot go into corridor while the other agent is still there
                        var maxTime = timeCounter;

                        // Find entry time for agent
                        timeCounter = positionConflict.Time;
                        if (timeCounter >= parentNode.Solution[conflictedAgent].Count)
                        {
                            timeCounter = 0;
                        }
                        else
                        {
                            while (timeCounter > 0
                                   && parentNode.Solution[conflictedAgent][timeCounter].Positions
                                       .Exists(p => corridor.Contains(p)))
                            {
                                timeCounter--;
                            }
                        }


                        // Add constraint saying the agent should not enter while the other agent is still in there
                        var minTime = timeCounter;

                        constraint = new CorridorConstraint
                        {
                            Agent = conflictedAgent.ReferenceAgent,
                            CorridorPositions = corridor.ToList(),
                            Time = (minTime, maxTime),
                            Conflict = conflict,
                        };
                    }
                    else
                    {
                        constraint = new Constraint
                        {
                            Agent = conflictedAgent.ReferenceAgent,
                            Position = positionConflict.Position,
                            Time = positionConflict.Time,
                            Conflict = conflict
                        };
                    }

                    break;

                case FollowConflict followConflict:
                    var constraintTime = conflictedAgent == followConflict.Follower
                        ? followConflict.FollowerTime
                        : followConflict.FollowerTime - 1;

                    if (constraintTime <= 0)
                    {
                        return null;
                    }

                    constraint = new Constraint
                    {
                        Agent = conflictedAgent.ReferenceAgent,
                        Position = followConflict.FollowerPosition,
                        Time = constraintTime,
                        Conflict = conflict,
                    };

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conflict));
            }

            return constraint;
        }
    }
}
