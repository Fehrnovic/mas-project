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
            Level.ParseLevel("SAsoko3_04.lvl");

            Console.Error.WriteLine($"Level initialized in {Timer.ElapsedMilliseconds / 1000.0} seconds");

            // Set the GraphSearch to output progress (notice: only quick solutions will crash editor...)
            // GraphSearch.OutputProgress = true;

            Timer.Restart();

            var usedBoxes = new List<Box>();
            var previousSolutionStates = new Dictionary<Agent, SAState>();
            var currentBoxGoal = new Dictionary<Agent, Box>();
            var finishedAgents = new Dictionary<Agent, bool>();
            var missingBoxGoals = new Dictionary<Agent, Queue<Box>>();
            var agentSolutionsSteps = new Dictionary<Agent, List<SAStep>>();
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
                var delegation = new Dictionary<Agent, SAState>();
                foreach (var agent in Level.Agents)
                {
                    var hasUnfinishedBoxGoals = missingBoxGoals[agent].Any();
                    var hasCurrentBoxGoal = currentBoxGoal[agent] != null;
                    var hasAgentGoal = Level.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number) != null;

                    if (hasCurrentBoxGoal)
                    {
                        // Previous sub-goal was to get to the box to solve a box-goal. Now solve the box goal:
                        var agentToBoxState = new SAState(
                            agent,
                            previousSolutionStates[agent].AgentPosition,
                            null,
                            previousSolutionStates[agent].PositionsOfBoxes,
                            previousSolutionStates[agent].BoxGoals.Append(currentBoxGoal[agent]).ToList(),
                            new HashSet<Constraint>()
                        );

                        // Reset that the agent no longer is finishing a box goal
                        currentBoxGoal[agent] = null;

                        delegation.Add(agent, agentToBoxState);
                    }
                    else if (hasUnfinishedBoxGoals)
                    {
                        // Sub-goal 1: Get to box to solve first box-goal
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

                // Do CBS - need to return the state for the finished solution for each agent to be used later on
                var solution = CBS.Run(delegation);

                // Find the minimum solution of none finished agents.
                var minSolution = solution.Where(a => a).Min(a => a.Count);

                // Retrieve the state of the index of the miminum solution and set as previous solution
                foreach (var agent in Level.Agents)
                {
                    // TODO: how to get state of a specific step? Make step have state included?
                    previousSolutionStates[agent] = solution[agent][minSolution].State;

                    // For all steps lower than minimum solution, add to the agent solution steps
                    foreach (var step in solution[agent].Where(s => s < minSolution))
                    {
                        agentSolutionsSteps[agent].Add(step);
                    }
                }
            }

            // Solution has now been found using sub-goals. Create action plan
            // agentSolutionsSteps holds the list of steps to be performed. Convert to action.

            Console.Error.WriteLine($"Found solution in {Timer.ElapsedMilliseconds / 1000.0} seconds");

            var noOp = new Action("NoOp", ActionType.NoOp, 0, 0, 0, 0);

            var maxIndex = solution.Max(a => a.Count);

            foreach (var actionList in solution)
            {
                if (actionList.Count < maxIndex)
                {
                    for (var i = actionList.Count; i < maxIndex; i++)
                    {
                        actionList.Add(noOp);
                    }
                }
            }

            for (var i = 1; i < solution[0].Count; i++)
            {
                for (var j = 0; j < solution.Count; j++)
                {
                    Console.Write(solution[j][i].Name);

                    if (j != solution.Count - 1)
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
