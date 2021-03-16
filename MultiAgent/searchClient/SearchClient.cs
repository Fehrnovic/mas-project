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
                    } else if ('A' <= c && c <= 'Z')
                    {
                        boxes.Add(new Box(c, color));
                    }
                }

                line = Console.ReadLine();
            }

            Console.Write("Hej");
        }
    }
}
