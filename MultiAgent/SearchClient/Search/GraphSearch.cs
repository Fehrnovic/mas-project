using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MultiAgent.SearchClient.CBS;

namespace MultiAgent.SearchClient.Search
{
    public class GraphSearch
    {
        public static bool OutputProgress = true;

        public static readonly Stopwatch Timer = new();

        public static IEnumerable<IStep> Search(IState initialState, IFrontier frontier)
        {
            if (initialState is SAState saState)
            {
                if (saState.Constraints.Any(c =>
                {
                    if (c is Constraint constraint)
                    {
                        var constrainedPosition = constraint.Position;
                        var agentToPositionTime =
                            Level.GetDistanceBetweenPosition(saState.AgentPosition, constrainedPosition);

                        if (agentToPositionTime > constraint.Time + 1)
                        {
                            return true;
                        }
                    }

                    return false;
                }))
                {
                    return null;
                }
            }

            Timer.Restart();

            var iterations = 0;

            frontier.Add(initialState);
            var exploredStates = new HashSet<IState>();

            while (true)
            {
                if (frontier.IsEmpty())
                {
                    if (Program.ShouldPrint >= 5)
                    {
                        if (initialState is SAState initialSaState)
                        {
                            Console.Error.WriteLine($"NO SOLUTION FOR {(initialSaState).Agent.Number}");
                        }
                    }

                    return null;
                }

                var state = frontier.Pop();
                exploredStates.Add(state);

                if (OutputProgress)
                {
                    if (++iterations % 100000 == 0)
                    {
                        if (Program.ShouldPrint >= 2)
                        {
                            PrintSearchStatus(exploredStates, frontier);
                        }

                        if (Program.ShouldPrint >= 2)
                        {
                            Console.Error.WriteLine($"{state}");
                        }
                    }
                }

                if (state.IsGoalState(exploredStates))
                {
                    if (OutputProgress)
                    {
                        // Console.Error.WriteLine("Found goal with following status:");
                        // PrintSearchStatus(exploredStates, frontier);
                    }

                    return state.ExtractPlan();
                }


                var reachableStates = state.GetExpandedStates();

                foreach (var reachableState in reachableStates)
                {
                    if (!frontier.Contains(reachableState) && !exploredStates.Contains(reachableState))
                    {
                        frontier.Add(reachableState);
                    }
                }
            }
        }

        private static void PrintSearchStatus(HashSet<IState> exploredStates, IFrontier frontier)
        {
            var elapsedTime = (Program.Timer.ElapsedMilliseconds) / 1000.0;
            Console.Error.WriteLine(
                $"#Expanded {exploredStates.Count}, #Frontier: {frontier.Size()}, #Generated: {exploredStates.Count + frontier.Size()}, Time: {elapsedTime}");
        }
    }
}
