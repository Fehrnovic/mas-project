using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MultiAgent.SearchClient;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Search;
using MultiAgent.SearchClient.Utils;
using Action = MultiAgent.SearchClient.Action;

namespace MultiAgent
{
    class Program
    {
        public static readonly Stopwatch Timer = new();
        public static readonly int ShouldPrint = 6;

        public static string[] Args;

        public static void Main(string[] args)
        {
            // Set the program args to a static field
            Args = args;

            // Setup the Console
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            Console.WriteLine("SearchClient");

            // Test if the debug flag is enabled
            ShouldDebug();

            Timer.Start();

            // Initialize the level
            Level.ParseLevel("MAchallenge.lvl");

            Console.Error.WriteLine($"Level initialized in {Timer.ElapsedMilliseconds / 1000.0} seconds");

            // Set the GraphSearch to output progress (notice: only quick solutions will crash editor...)
            // GraphSearch.OutputProgress = true;

            Timer.Restart();

            var usedBoxes = new List<Box>(Level.Boxes.Count);
            var previousSolutionStates = new Dictionary<Agent, SAState>(Level.Agents.Count);
            var currentBoxGoal = new Dictionary<Agent, Box>(Level.Agents.Count);
            var currentMostRelevantBox = new Dictionary<Agent, Box>(Level.Agents.Count); // Use to guide agent
            var finishedAgents = new Dictionary<Agent, bool>(Level.Agents.Count);
            var finishedSubGoal = new Dictionary<Agent, bool>(Level.Agents.Count);
            var missingBoxGoals = new Dictionary<Agent, Queue<Box>>(Level.Agents.Count);
            var agentSolutionsSteps = new Dictionary<Agent, List<SAStep>>(Level.Agents.Count);
            foreach (var agent in Level.Agents)
            {
                var list = new Queue<Box>();
                foreach (var box in LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[agent])
                {
                    list.Enqueue(box);
                }

                missingBoxGoals.Add(agent, list);
                currentBoxGoal.Add(agent, null);
                currentMostRelevantBox.Add(agent, null);
                finishedAgents.Add(agent, false);
                finishedSubGoal.Add(agent, true);
                agentSolutionsSteps.Add(agent, new List<SAStep>());
                previousSolutionStates.Add(agent, new SAState(
                    agent,
                    null,
                    LevelDelegationHelper.LevelDelegation.AgentToBoxes[agent],
                    new List<Box>(),
                    new HashSet<IConstraint>()
                ));
            }

            while (finishedAgents.Any(f => !f.Value)) // while All goals not satisfied
            {
                var delegation = new Dictionary<Agent, SAState>(Level.Agents.Count);
                foreach (var agent in Level.Agents)
                {
                    if (!finishedSubGoal[agent] && !finishedAgents[agent])
                    {
                        // Continue previous goals :)
                        var agentToBoxState = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            previousSolutionStates[agent].AgentGoal,
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals,
                            new HashSet<IConstraint>()
                        );

                        agentToBoxState.CurrentBoxGoal = previousSolutionStates[agent].CurrentBoxGoal;
                        agentToBoxState.RelevantBoxToSolveGoal = previousSolutionStates[agent].RelevantBoxToSolveGoal;

                        delegation.Add(agent, agentToBoxState);

                        continue;
                    }

                    if (currentBoxGoal[agent] != null)
                    {
                        // Previous sub-goal was to get to the box to solve a box-goal. Now solve the box goal:
                        var boxGoals = previousSolutionStates[agent].BoxGoals.ToHashSet();
                        boxGoals.Add(currentBoxGoal[agent]);

                        var agentToBoxState = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            null,
                            previousSolutionStates[agent].PositionsOfBoxes,
                            boxGoals.ToList(),
                            new HashSet<IConstraint>()
                        );

                        if (currentMostRelevantBox[agent] != null)
                        {
                            agentToBoxState.RelevantBoxToSolveGoal = currentMostRelevantBox[agent];
                            agentToBoxState.CurrentBoxGoal = currentBoxGoal[agent];
                        }

                        // Reset that the agent no longer is finishing a box goal
                        currentBoxGoal[agent] = null;
                        currentMostRelevantBox[agent] = null;

                        delegation.Add(agent, agentToBoxState);
                    }
                    else if (missingBoxGoals[agent].Any())
                    {
                        // Sub-goal: Get to box to solve first box-goal
                        var boxGoal = missingBoxGoals[agent].Dequeue();

                        Box closestBox = null;
                        Position? closestBoxPosition = null;
                        foreach (var (boxPosition, box) in previousSolutionStates[agent].PositionsOfBoxes
                            .Where(b => !usedBoxes.Contains(b.Value)))
                        {
                            if (box.Letter != boxGoal.Letter)
                            {
                                continue;
                            }

                            if (closestBox == null || !closestBoxPosition.HasValue)
                            {
                                closestBox = box;
                                closestBoxPosition = boxPosition;
                                continue;
                            }

                            if (Level.GetDistanceBetweenPosition(closestBoxPosition.Value,
                                    boxGoal.GetInitialLocation()) >
                                Level.GetDistanceBetweenPosition(boxPosition, boxGoal.GetInitialLocation()))
                            {
                                closestBox = box;
                                closestBoxPosition = boxPosition;
                            }
                        }

                        usedBoxes.Add(closestBox);

                        var allBoxPositions = previousSolutionStates.Values.SelectMany(state =>
                            agent.Number == state.Agent.Number
                                ? new List<Position>()
                                : state.PositionsOfBoxes.Keys.ToList()).ToList();

                        var neighborPositions = Level.Graph
                            .NodeGrid[closestBoxPosition.Value.Row, closestBoxPosition.Value.Column].OutgoingNodes
                            .Select(n => new Position(n.Row, n.Column)).ToList();

                        var freeNeighborPositions = neighborPositions.Except(allBoxPositions).ToList();

                        neighborPositions = freeNeighborPositions.Any() ? freeNeighborPositions : neighborPositions;

                        var neighborPositionNode = neighborPositions
                            .OrderBy(p =>
                                Level.GetDistanceBetweenPosition(p, previousSolutionStates[agent].AgentPosition))
                            .FirstOrDefault(g => !previousSolutionStates[agent]
                                .BoxGoals.Exists(b =>
                                    b.GetInitialLocation().Row == g.Row &&
                                    b.GetInitialLocation().Column == g.Column));

                        Agent agentGoal;
                        // If neighbor position contains a box goal for this agent-
                        if (neighborPositionNode == null)
                        {
                            agentGoal = new Agent(agent.Number, agent.Color,
                                previousSolutionStates[agent].AgentPosition);
                        }
                        else
                        {
                            agentGoal = new Agent(agent.Number, agent.Color,
                                new Position(neighborPositionNode.Row, neighborPositionNode.Column));
                        }

                        var agentToBoxState = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            agentGoal,
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals,
                            new HashSet<IConstraint>()
                        );

                        // Set the box goal as the next sub-goal to solve
                        currentBoxGoal[agent] = boxGoal;
                        currentMostRelevantBox[agent] = closestBox;

                        delegation.Add(agent, agentToBoxState);
                    }
                    else
                    {
                        // All box goals has been satisfied
                        // Is there any agents this agent can help?
                        Agent agentToHelp = null;
                        foreach (var otherAgent in Level.Agents.Where(a => a.Color == agent.Color))
                        {
                            if (otherAgent == agent)
                            {
                                continue;
                            }

                            if (missingBoxGoals[otherAgent].Count > 1)
                            {
                                agentToHelp = otherAgent;
                                break;
                            }
                        }

                        if (agentToHelp != null)
                        {
                            Console.Error.WriteLine($"Agent {agent} can help {agentToHelp}");

                            // Take first goal to help
                            var goalToHelpWith = missingBoxGoals[agentToHelp].Dequeue();

                            // Find a box that can help solve the goal
                            var (positionOfBoxToHelpSolve, boxToHelpSolve) = previousSolutionStates[agentToHelp]
                                .PositionsOfBoxes.First(kv =>
                                    kv.Value.Letter == goalToHelpWith.Letter && !usedBoxes.Contains(kv.Value));


                            // Remove the box from the agent you're helping
                            previousSolutionStates[agentToHelp].PositionsOfBoxes.Remove(positionOfBoxToHelpSolve);

                            // Add the box to your boxes
                            previousSolutionStates[agent].PositionsOfBoxes
                                .Add(positionOfBoxToHelpSolve, boxToHelpSolve);

                            var agentGoal = new Agent(agent.Number, agent.Color,
                                previousSolutionStates[agent].AgentPosition);

                            var agentToBoxState = new SAState(
                                agent,
                                previousSolutionStates[agent].AgentPosition,
                                agentGoal,
                                previousSolutionStates[agent].PositionsOfBoxes,
                                previousSolutionStates[agent].BoxGoals,
                                new HashSet<IConstraint>()
                            );

                            // Set the box goal as the next sub-goal to solve
                            currentBoxGoal[agent] = goalToHelpWith;
                            currentMostRelevantBox[agent] = boxToHelpSolve;

                            delegation.Add(agent, agentToBoxState);
                            finishedAgents[agent] = false;

                            continue;
                        }

                        // . Solve agent goal now.
                        // OR No remaining gaols. Create a dummy state, with all goals, such that CBS won't destroy goal state.
                        var state = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number),
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals,
                            new HashSet<IConstraint>()
                        );

                        finishedAgents[agent] = true;

                        delegation.Add(agent, state);
                    }
                }

                // foreach (var (agent, state) in delegation)
                // {
                //     Console.Error.WriteLine($"Agent: {agent} state:");
                //     Console.Error.WriteLine(state.ToString());
                // }

                // Do CBS - need to return the state for the finished solution for each agent to be used later on
                var solution = CBS.Run(delegation, finishedAgents);

                // Find the minimum solution.
                var availableAgents = finishedAgents.Any(f => !f.Value)
                    ? solution.Where(a => !finishedAgents[a.Key])
                    : solution;

                var minSolution = availableAgents.Min(a => a.Value.Count);

                if (ShouldPrint >= 1)
                {
                    Console.Error.WriteLine(
                        $"Found sub-goal solution with min solution of {minSolution} in {Timer.ElapsedMilliseconds / 1000.0} seconds");
                }

                // Retrieve the state of the index of the mininum solution and set as previous solution
                foreach (var agent in Level.Agents)
                {
                    if (finishedAgents.All(f => f.Value))
                    {
                        // All agents are finished. Just take their last actions.
                        foreach (var step in solution[agent].Skip(1).Take(solution[agent].Count - 1))
                        {
                            agentSolutionsSteps[agent].Add(step);
                        }

                        continue;
                    }
                    // TODO: Convert all MASteps to SASteps

                    // How to handle finished states? needs to add no-ops.
                    var agentMinimumLength = Math.Min(minSolution - 1, solution[agent].Count - 1);
                    previousSolutionStates[agent] = solution[agent][agentMinimumLength].State;

                    // Solutions that are not equal to the minimum solution are not finished with their current goal.
                    finishedSubGoal[agent] = solution[agent].Count == minSolution;

                    var steps = solution[agent].Skip(1).Take(agentMinimumLength);

                    if (agentMinimumLength == 0)
                    {
                        for (var i = 0; i < minSolution - 1; i++)
                        {
                            agentSolutionsSteps[agent].Add(new SAStep(agentSolutionsSteps[agent].Last()));
                        }
                    }
                    else
                    {
                        // For all steps take steps up to minimum solution, add to the agent solution steps
                        foreach (var step in steps)
                        {
                            agentSolutionsSteps[agent].Add(step);
                        }
                    }
                }
            }

            Console.Error.WriteLine($"Found solution in {Timer.ElapsedMilliseconds / 1000.0} seconds");

            var sortedAgentSolutions = agentSolutionsSteps.OrderBy(a => a.Key.Number).ToList();

            // Find the max length solution and run solution
            for (var i = 0; i < agentSolutionsSteps.Max(a => a.Value.Count); i++)
            {
                var counter = 0;
                // Foreach agent, get their step of the current i index
                foreach (var (agent, stepsList) in sortedAgentSolutions)
                {
                    // If still has steps print those- else print no-op
                    Console.Write(i < stepsList.Count ? stepsList[i].Action.Name : Action.NoOp.Name);
                    Console.Error.Write(i < stepsList.Count ? stepsList[i].Action.Name : Action.NoOp.Name);
                    if (counter++ != agentSolutionsSteps.Count - 1)
                    {
                        Console.Write("|");
                        Console.Error.Write("|");
                    }
                }

                Console.WriteLine();
                Console.Error.WriteLine();
            }
        }

        private static void ShouldDebug()
        {
            if (Args.Length <= 0 || Args[0] != "debug")
            {
                return;
            }

            Debugger.Launch();
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
