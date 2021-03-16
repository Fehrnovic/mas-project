using MultiAgent.searchClient;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

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

            SearchClient.ParseLevel();

            Console.Write("Move(S)");
            Console.WriteLine();
            Console.Write("Move(E)");
            Console.WriteLine();
        }
    }
}
