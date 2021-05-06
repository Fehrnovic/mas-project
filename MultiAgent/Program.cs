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
            Level.ParseLevel("MAsimple1.lvl");

            Console.Error.WriteLine($"Level initialized in {Timer.ElapsedMilliseconds / 1000.0} seconds");

            // Set the GraphSearch to output progress (notice: only quick solutions will crash editor...)
            // GraphSearch.OutputProgress = true;

            Timer.Restart();

            var usedBoxes = new List<Box>(Level.Boxes.Count);
            var previousSolutionStates = new Dictionary<Agent, SAState>(Level.Agents.Count);
            var currentBoxGoal = new Dictionary<Agent, Box>(Level.Agents.Count);
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
                finishedAgents.Add(agent, false);
                finishedSubGoal.Add(agent, false);
                agentSolutionsSteps.Add(agent, new List<SAStep>());
                previousSolutionStates.Add(agent, new SAState(
                    agent,
                    null,
                    LevelDelegationHelper.LevelDelegation.AgentToBoxes[agent],
                    new List<Box>(),
                    new HashSet<Constraint>()
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
                            null,
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals.Append(currentBoxGoal[agent]).ToHashSet().ToList(),
                            new HashSet<Constraint>()
                        );

                        delegation.Add(agent, agentToBoxState);

                        continue;
                    }

                    if (currentBoxGoal[agent] != null)
                    {
                        // Previous sub-goal was to get to the box to solve a box-goal. Now solve the box goal:
                        var agentToBoxState = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            null,
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals.Append(currentBoxGoal[agent]).ToHashSet().ToList(),
                            new HashSet<Constraint>()
                        );

                        // Reset that the agent no longer is finishing a box goal
                        currentBoxGoal[agent] = null;

                        delegation.Add(agent, agentToBoxState);
                    }
                    else if (missingBoxGoals[agent].Any())
                    {
                        // Sub-goal: Get to box to solve first box-goal
                        var boxGoal = missingBoxGoals[agent].Dequeue();

                        Box closestBox = null;
                        Position? closestBoxPosition = null;
                        foreach (var (boxPosition, box) in previousSolutionStates[agent].PositionsOfBoxes.Where(b => !usedBoxes.Contains(b.Value)))
                        {
                            if (closestBox == null || !closestBoxPosition.HasValue)
                            {
                                closestBox = box;
                                closestBoxPosition = boxPosition;
                                continue;
                            }

                            if (Level.GetDistanceBetweenPosition(closestBoxPosition.Value, boxGoal.GetInitialLocation()) >
                                Level.GetDistanceBetweenPosition(boxPosition, boxGoal.GetInitialLocation()))
                            {
                                closestBox = box;
                                closestBoxPosition = boxPosition;
                            }
                        }
                        usedBoxes.Add(closestBox);

                        var neighborPositionNode = Level.Graph.NodeGrid[closestBoxPosition.Value.Row,
                                closestBoxPosition.Value.Column]
                            .OutgoingNodes.First();

                        var agentGoal = new Agent(agent.Number, agent.Color, new Position(neighborPositionNode.Row, neighborPositionNode.Column));
                        var agentToBoxState = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            agentGoal,
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals,
                            new HashSet<Constraint>()
                        );

                        // Set the box goal as the next sub-goal to solve
                        currentBoxGoal[agent] = boxGoal;

                        delegation.Add(agent, agentToBoxState);
                    }
                    else
                    {
                        // All box goals has been satisfied. Solve agent goal now.
                        // OR No remaining gaols. Create a dummy state, with all goals, such that CBS won't destroy goal state.
                        var state = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number),
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals,
                            new HashSet<Constraint>()
                        );

                        finishedAgents[agent] = true;

                        delegation.Add(agent, state);
                    }
                }

                foreach (var (agent, state) in delegation)
                {
                    Console.Error.WriteLine($"Agent: {agent} state:");
                    Console.Error.WriteLine(state.ToString());
                }

                // Do CBS - need to return the state for the finished solution for each agent to be used later on
                var solution = CBS.Run(delegation, finishedAgents);

                // Find the minimum solution of none finished agents.
                var minSolution = solution.Where(a => !finishedAgents[a.Key]).Min(a => a.Value.Count);

                Console.Error.WriteLine($"Found sub-goal solution with min solution of {minSolution} in {Timer.ElapsedMilliseconds / 1000.0} seconds");

                // Retrieve the state of the index of the mininum solution and set as previous solution
                foreach (var agent in Level.Agents)
                {
                    // TODO: Convert all MASteps to SASteps
                    previousSolutionStates[agent] = solution[agent][minSolution - 1].State;

                    // Solutions that are not equal to the minimum solution are not finished with their current goal.
                    finishedSubGoal[agent] = solution[agent].Count == minSolution;

                    // For all steps take steps up to minimum solution, add to the agent solution steps
                    foreach (var step in solution[agent].Take(minSolution - 1))
                    {
                        agentSolutionsSteps[agent].Add(step);
                    }
                }
            }

            Console.Error.WriteLine($"Found solution in {Timer.ElapsedMilliseconds / 1000.0} seconds");

            // Find the max length solution and run solution
            for (var i = 0; i < agentSolutionsSteps.Max(a => a.Value.Count); i++)
            {
                var counter = 0;
                // Foreach agent, get their step of the current i index
                foreach (var stepsList in agentSolutionsSteps.Values)
                {
                    // If still has steps print those- else print no-op
                    Console.Write(i < stepsList.Count ? stepsList[i].Action.Name : Action.NoOp.Name);
                    if (counter++ != agentSolutionsSteps.Count - 1)
                    {
                        Console.Write("|");
                    }
                }

                Console.WriteLine();
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
