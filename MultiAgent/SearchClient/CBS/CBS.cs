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
                Solution = new Dictionary<Agent, List<SAStep>>(),
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

                Node.CM[agent1.Number, agent2.Number] += 1;
                Node.CM[agent2.Number, agent1.Number] += 1;

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

                    SAState state;

                    state = CreateSAState(conflictedAgent,
                        delegation[conflictedAgent], A.Constraints);


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

        private static IConstraint CreateConstraint(Agent conflictedAgent, IConflict conflict, Node parentNode)
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
                            Agent = conflictedAgent,
                            CorridorPositions = corridor.ToList(),
                            Time = (minTime, maxTime),
                            Conflict = conflict,
                        };
                    }
                    else
                    {
                        constraint = new Constraint
                        {
                            Agent = conflictedAgent,
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
                        Agent = conflictedAgent,
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
