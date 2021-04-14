using MultiAgent.searchClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MultiAgent
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            Console.WriteLine("SearchClient");

            // Test if the debug flag is enabled
            ShouldDebug(args);

            // Read from file (FileBuffer) if level name is specified. Use Console otherwise
            // var initialState = ParseLevel("SAFirefly.lvl");
            var initialState = ParseLevel();

            var plan = GraphSearch.Search(initialState, new BFSFrontier());
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

        private static State ParseLevel(string levelName = null)
        {
            if (levelName == null)
            {
                // Read from Console (stdin)
                return SearchClient.ParseLevel(new LevelReader(LevelReader.Type.Console));
            }

            var filePath = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName + "/levels/" + levelName;

            return SearchClient.ParseLevel(new LevelReader(LevelReader.Type.File, File.ReadAllLines(filePath)));
        }

        private static void ShouldDebug(string[] args)
        {
            if (args.Length <= 0 || args[0] != "debug")
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
