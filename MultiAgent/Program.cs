using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MultiAgent.SearchClient;
using MultiAgent.SearchClient.Search;

namespace MultiAgent
{
    class Program
    {
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

            // Initialize the level
            Level.ParseLevel("SAFirefly.lvl");

            // Set the GraphSearch to output progress (notice: only quick solutions will crash editor...)
            // GraphSearch.OutputProgress = true;

            var plan = GraphSearch.Search(new State(Level.Agents, Level.Boxes), new BFSFrontier());
            if (plan == null)
            {
                Console.Error.WriteLine("Unable to solve level.");
                Environment.Exit(0);
            }

            Environment.Exit(0);

            foreach (var jointAction in plan)
            {
                Console.Write(jointAction[0].Name);

                for (int action = 1; action < jointAction.Length; ++action)
                {
                    Console.Write("|");
                    Console.Write(jointAction[action].Name);
                }
                Console.WriteLine();
                // We must read the server's response to not fill up the stdin buffer and block the server.
                Console.ReadLine();
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
