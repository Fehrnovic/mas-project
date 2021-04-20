using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MultiAgent.SearchClient
{
    public class GraphSearch
    {
        private static readonly Stopwatch Timer = new();

        public static Action[][] Search(State initialState, IFrontier frontier)
        {
            Timer.Start();

            var iterations = 0;

            frontier.Add(initialState);
            var exploredStates = new HashSet<State>();

            while (true)
            {
                if (++iterations % 100000 == 0)
                {
                    PrintSearchStatus(exploredStates, frontier);
                }

                if (frontier.IsEmpty())
                {
                    return null;
                }

                State state = frontier.Pop();
                //
                // if (iterations % 100000 == 0)
                // {
                //     Console.Error.WriteLine(state.ToString());
                // }

                // Console.WriteLine(state.Agents.Count);
                // Console.WriteLine(state.Agents[0].Position.Row + " " + state.Agents[0].Position.Col);
                // Agent ag = state.AgentAt(new Position(1, 1));
                // Console.Error.WriteLine($"Found agent: {ag.Number}");
                // Console.Error.WriteLine(state.ToString());
                //
                // return null;


                if (state.IsGoalState())
                {
                    Console.Error.WriteLine("Found goal with following status:");
                    PrintSearchStatus(exploredStates, frontier);

                    return state.ExtractPlan();
                }

                exploredStates.Add(state);

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

        private static void PrintSearchStatus(HashSet<State> exploredStates, IFrontier frontier)
        {
            var elapsedTime = (Timer.ElapsedMilliseconds) / 1000.0;
            Console.Error.WriteLine(
                $"#Expanded {exploredStates.Count}, #Frontier: {frontier.Size()}, #Generated: {exploredStates.Count + frontier.Size()}, Time: {elapsedTime}");
        }
    }
}
