using MultiAgent.searchClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Action = MultiAgent.searchClient.Action;

namespace MultiAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));

            Console.WriteLine("SearchClient");

            if (args.Length > 0 && args[0] == "debug")
            {
                Debugger.Launch();
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }
            }

            var initialState = SearchClient.ParseLevel();

            Action[][] plan = GraphSearch.Search(initialState, new BFSFrontier());

            if (plan == null)
            {
                Console.Error.WriteLine("Unable to solve level.");
                Environment.Exit(0);
            }

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
    }
}
