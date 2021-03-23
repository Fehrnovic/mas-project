using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.searchClient
{
    public static class SearchClient
    {
        public static void ParseLevel()
        {
            // We can assume that the level file is conforming to specification, since the server verifies this.
            // Read domain
            Console.ReadLine(); // #domain
            Console.ReadLine(); // hospital

            // Read Level name
            Console.ReadLine(); // #levelname
            Console.ReadLine(); // <name>

            // Read colors
            Console.ReadLine(); // #colors
            List<Agent> agents = new List<Agent>();
            List<Box> boxes = new List<Box>();
            string line = Console.ReadLine();
            while (!line.StartsWith("#"))
            {
                string[] split = line.Split(":");
                Color color = ColorExtension.FromString(split[0].Trim());
                string[] entities = split[1].Split(",");
                foreach (string entity in entities)
                {
                    char c = entity.Trim()[0];
                    if ('0' <= c && c <= '9')
                    {
                        agents.Add(new Agent(c - '0', color));
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        boxes.Add(new Box(c, color));
                    }
                }

                line = Console.ReadLine();
            }

            // Read initial state
            // line is currently "#initial"
            int numRows = 0;
            int numCols = 0;
            List<string> levelLines = new List<string>();
            line = Console.ReadLine();
            while (!line.StartsWith("#"))
            {
                levelLines.Add(line);
                numCols = Math.Max(numCols, line.Length);
                ++numRows;
                line = Console.ReadLine();
            }

            int row;
            bool[,] walls = new bool[numRows, numCols];
            for (row = 0; row < numRows; ++row)
            {
                line = levelLines[row];
                for (int col = 0; col < line.Length; ++col)
                {
                    char c = line[col];

                    if ('0' <= c && c <= '9')
                    {
                        agents.First(a => a.Number == c - '0').Position = new Position(row, col);
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        boxes.First(b => b.Letter == c).Position = new Position(row, col);
                    }
                    else if (c == '+')
                    {
                        walls[row, col] = true;
                    }
                }
            }

            // Read goal state
            // line is currently "#goal"
            List<Agent> agentGoals = new List<Agent>();
            List<Box> boxGoals = new List<Box>();
            line = Console.ReadLine();
            row = 0;
            while (!line.StartsWith("#"))
            {
                for (int col = 0; col < line.Length; ++col)
                {
                    char c = line[col];

                    if (('0' <= c && c <= '9'))
                    {
                        agentGoals.Add(new Agent(c - '0', new Position(row, col)));
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        var color = boxes.First(b => b.Letter == c).Color;
                        boxGoals.Add(new Box(c, color, new Position(row, col)));
                    }
                }

                ++row;
                line = Console.ReadLine();
            }

            Console.Write("Hej");
        }
    }
}
