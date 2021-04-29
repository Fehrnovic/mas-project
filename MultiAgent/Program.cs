using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MultiAgent.SearchClient;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Search;
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
            Level.ParseLevel("custom/MA_Push.lvl");

            Console.Error.WriteLine($"Level initialized in {Timer.ElapsedMilliseconds / 1000.0} seconds");

            // Set the GraphSearch to output progress (notice: only quick solutions will crash editor...)
            // GraphSearch.OutputProgress = true;

            Timer.Restart();

            var initialState = new MAState(Level.Agents, Level.AgentGoals, Level.Boxes, Level.BoxGoals, new HashSet<Constraint>());
            var test = GraphSearch.Search(initialState, new BestFirstFrontier());
            
            var solution = CBS.Run();
            
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
