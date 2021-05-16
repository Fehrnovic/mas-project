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
        public static readonly int ShouldPrint = 2;
        public static int MaxMovesAllowed = 2;

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
            Level.ParseLevel("MAmultiagentSort.lvl");

            if (ShouldPrint >= 2)
            {
                Console.Error.WriteLine($"Level initialized in {Timer.ElapsedMilliseconds / 1000.0} seconds");
            }

            // Set the GraphSearch to output progress (notice: only quick solutions will crash editor...)
            // GraphSearch.OutputProgress = true;

            Timer.Restart();

            var usedBoxes = new List<Box>(Level.Boxes.Count);
            var previousSolutionStates = new Dictionary<Agent, SAState>(Level.Agents.Count);
            var currentBoxGoal = new Dictionary<Agent, Box>(Level.Agents.Count);
            var currentMostRelevantBox = new Dictionary<Agent, Box>(Level.Agents.Count); // Use to guide agent
            var finishedAgents = new Dictionary<Agent, bool>(Level.Agents.Count);
            var aboutToFinishAgents = new Dictionary<Agent, bool>(Level.Agents.Count);
            var finishedSubGoal = new Dictionary<Agent, bool>(Level.Agents.Count);
            var missingBoxGoals = new Dictionary<Agent, Queue<Box>>(Level.Agents.Count);
            var agentSolutionsSteps = new Dictionary<Agent, List<SAStep>>(Level.Agents.Count);
            foreach (var agent in Level.Agents)
            {
                var list = new Queue<Box>();
                foreach (var kvp1 in LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[agent]
                    .OrderByDescending(kvp => kvp.cost))
                {
                    list.Enqueue(kvp1.box);
                }

                missingBoxGoals.Add(agent, list);
                currentBoxGoal.Add(agent, null);
                currentMostRelevantBox.Add(agent, null);
                finishedAgents.Add(agent, false);
                aboutToFinishAgents.Add(agent, false);
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

            MAState previousDummyState = null;

            if (Level.Agents.Count == 1)
            {
                MaxMovesAllowed = int.MaxValue;
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

                        Agent agentGoal;
                        // If neighbor position contains a box goal for this agent-
                        if (!neighborPositions.Exists(g => !previousSolutionStates[agent]
                            .BoxGoals.Exists(b =>
                                b.GetInitialLocation().Row == g.Row &&
                                b.GetInitialLocation().Column == g.Column)))
                        {
                            agentGoal = new Agent(agent.Number, agent.Color,
                                previousSolutionStates[agent].AgentPosition);
                        }
                        else
                        {
                            var neighborPositionNode = neighborPositions
                                .OrderBy(p =>
                                    Level.GetDistanceBetweenPosition(p, previousSolutionStates[agent].AgentPosition))
                                .FirstOrDefault(g => !previousSolutionStates[agent]
                                    .BoxGoals.Exists(b =>
                                        b.GetInitialLocation().Row == g.Row &&
                                        b.GetInitialLocation().Column == g.Column));

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

                        aboutToFinishAgents[agent] = true;

                        delegation.Add(agent, state);
                    }
                }

                // Do CBS - need to return the state for the finished solution for each agent to be used later on
                var solution = CBS.Run(delegation, finishedAgents);
                if (solution == null)
                {
                    Console.Error.WriteLine("NO SOLUTION FOUND FOR CBS! EXITING...");
                    Environment.Exit(-1);
                }

                // Find the minimum solution.
                var availableAgents = finishedAgents.Any(f => !f.Value)
                    ? solution.Where(a => !finishedAgents[a.Key])
                    : solution;

                var minSolution = Math.Min(availableAgents.Min(a => a.Value.Count), MaxMovesAllowed);

                // Amount of agents that aren't finished and only performs no ops
                var count = solution.Where(kvp => !finishedAgents[kvp.Key]).Count(kvp =>
                    kvp.Value.Skip(1).Take(minSolution - 1).All(s => s.Action.Type == ActionType.NoOp));

                if (count >= 1 && minSolution > 1)
                {
                    MaxMovesAllowed += 1;
                }

                if (ShouldPrint >= 1)
                {
                    Console.Error.WriteLine(
                        $"Found sub-goal solution with min solution of {Math.Max(minSolution - 1, 0)} in {Timer.ElapsedMilliseconds / 1000.0} seconds");
                }

                // Retrieve the state of the index of the mininum solution and set as previous solution
                foreach (var agent in Level.Agents)
                {
                    // Solutions that are not equal to the minimum solution are not finished with their current goal.
                    finishedSubGoal[agent] = solution[agent].Count == minSolution;

                    // Set finished agents
                    if (aboutToFinishAgents[agent] && solution[agent].Count == minSolution)
                    {
                        finishedAgents[agent] = true;
                    }

                    // Calculate the minimum length of this agents solution.
                    var agentMinimumLength = Math.Min(minSolution - 1, solution[agent].Count - 1);

                    // Update the previous solution states with previous state - Solutions will always have the 0 index as just a state with no action
                    previousSolutionStates[agent] = solution[agent][agentMinimumLength].State;

                    if (agentMinimumLength == 0 && !agentSolutionsSteps[agent].Any())
                    {
                        // No last steps. Add no-ops with previous state
                        for (var i = 0; i < minSolution - 1; i++)
                        {
                            agentSolutionsSteps[agent].Add(new SAStep(previousSolutionStates[agent], true));
                        }
                    }
                    else if (agentMinimumLength == 0)
                    {
                        // Add the last step as last solution position with no-ops
                        for (var i = 0; i < minSolution - 1; i++)
                        {
                            agentSolutionsSteps[agent].Add(new SAStep(agentSolutionsSteps[agent].Last()));
                        }
                    }
                    else
                    {
                        // For all steps take steps up to minimum solution, add to the agent solution steps
                        var steps = solution[agent].Skip(1).Take(agentMinimumLength).ToList();
                        foreach (var step in steps)
                        {
                            agentSolutionsSteps[agent].Add(step);
                        }

                        // Add no-ops for rest of min solution length
                        for (var i = 0; i < minSolution - 1 - steps.Count; i++)
                        {
                            agentSolutionsSteps[agent].Add(new SAStep(previousSolutionStates[agent], true));
                        }
                    }
                }


                var agents = new Dictionary<Agent, Position>();
                var agentGoals = new List<Agent>();
                var boxes = new Dictionary<Position, Box>();
                var boxGoalsDummy = new List<Box>();
                foreach (var agent in Level.Agents)
                {
                    var previousState = previousSolutionStates[agent];

                    agents.Add(agent, previousState.AgentPosition);
                    if (previousState.AgentGoal != null)
                    {
                        agentGoals.Add(previousState.AgentGoal);
                    }

                    foreach (var previousStateBoxGoal in previousState.BoxGoals)
                    {
                        boxGoalsDummy.Add(previousStateBoxGoal);
                    }

                    foreach (var (position, box) in previousState.PositionsOfBoxes)
                    {
                        boxes.Add(position, box);
                    }
                }

                var currentState = new MAState(agents, agentGoals, boxes, boxGoalsDummy, new HashSet<IConstraint>(),
                    new Dictionary<Agent, Box>(), new Dictionary<Agent, Box>());

                if (currentState.Equals(previousDummyState) && minSolution > 1)
                {
                    foreach (var (agent, steps) in agentSolutionsSteps)
                    {
                        steps.RemoveRange(steps.Count - (minSolution - 1), minSolution - 1);
                    }

                    MaxMovesAllowed += 1;
                }

                previousDummyState = currentState;

                if (ShouldPrint >= 2)
                {
                    Console.Error.WriteLine(currentState.ToString());
                }
            }

            if (ShouldPrint >= 1)
            {
                Console.Error.WriteLine($"Found solution in {Timer.ElapsedMilliseconds / 1000.0} seconds");
            }

            var sortedAgentSolutions = agentSolutionsSteps.OrderBy(a => a.Key.Number).ToList();

            // Find the max length solution and run solution
            for (var i = 0; i < agentSolutionsSteps.Max(a => a.Value.Count); i++)
            {
                if (ShouldPrint >= 2)
                {
                    Console.Error.Write($"{i + 1}: ");
                }

                var counter = 0;
                // Foreach agent, get their step of the current i index
                foreach (var (agent, stepsList) in sortedAgentSolutions)
                {
                    // If still has steps print those- else print no-op
                    Console.Write(i < stepsList.Count ? stepsList[i].Action.Name : Action.NoOp.Name);

                    if (ShouldPrint >= 2)
                    {
                        Console.Error.Write(i < stepsList.Count ? stepsList[i].Action.Name : Action.NoOp.Name);
                    }

                    if (counter++ != agentSolutionsSteps.Count - 1)
                    {
                        Console.Write("|");
                        if (ShouldPrint >= 2)
                        {
                            Console.Error.Write("|");
                        }
                    }
                }

                Console.WriteLine();
                if (ShouldPrint >= 2)
                {
                    var outcome = Console.ReadLine();
                    var hasFalse = outcome.Split('|').Any(o => o == "false");

                    Console.Error.WriteLine(" (" + outcome + ")" + (hasFalse ? " <-------- FALSE!!!" : ""));
                }
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
