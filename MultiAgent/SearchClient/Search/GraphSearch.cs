using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public class GraphSearch
    {
        public static bool OutputProgress = true;

        public static readonly Stopwatch Timer = new();

        public static IEnumerable<IStep> Search(IState initialState, IFrontier frontier)
        {
            Timer.Restart();

            var iterations = 0;

            frontier.Add(initialState);
            var exploredStates = new HashSet<IState>();

            while (true)
            {
                if (OutputProgress)
                {
                    if (++iterations % 20000 == 0)
                    {
                        PrintSearchStatus(exploredStates, frontier);
                    }
                }


                if (frontier.IsEmpty())
                {
                    return null;
                }

                var state = frontier.Pop();
                exploredStates.Add(state);

                if (state.IsGoalState(exploredStates))
                {
                    if (OutputProgress)
                    {
                        Console.Error.WriteLine("Found goal with following status:");
                        PrintSearchStatus(exploredStates, frontier);
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
            var elapsedTime = (Timer.ElapsedMilliseconds) / 1000.0;
            var type = exploredStates.FirstOrDefault();
            var prefix = "";
            if (type != null)
            {
                prefix = type is MAState ? "MA" : "SA";
            }
            Console.Error.WriteLine(
                $"{prefix}: #Expanded {exploredStates.Count}, #Frontier: {frontier.Size()}, #Generated: {exploredStates.Count + frontier.Size()}, Time: {elapsedTime}");
        }
    }
}
