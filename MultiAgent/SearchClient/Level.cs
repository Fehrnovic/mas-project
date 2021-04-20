using System;
using System.Collections.Generic;
using System.IO;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient
{
    public class Level
    {
        public static List<Agent> AgentGoals;
        public static List<Box> BoxGoals;
        public static List<Agent> Agents;
        public static List<Box> Boxes;
        public static bool[,] Walls;

        public static int Rows;
        public static int Columns;

        public static void ParseLevel(string levelName = null)
        {
            LevelReader levelReader;
            if (levelName == null || (Program.Args.Length > 1 && Program.Args[1] == "console"))
            {
                levelReader = new LevelReader(LevelReader.Type.Console);
            }
            else
            {
                var filePath = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName + "/levels/" + levelName;
                levelReader = new LevelReader(LevelReader.Type.File, File.ReadAllLines(filePath));
            }

            // We can assume that the level file is conforming to specification, since the server verifies this.
            // Read domain
            levelReader.ReadLine(); // #domain
            levelReader.ReadLine(); // hospital

            // Read Level name
            levelReader.ReadLine(); // #levelname
            levelReader.ReadLine(); // <name>

            // Read colors
            levelReader.ReadLine(); // #colors

            var agentColors = new Dictionary<int, Color>();
            var boxColors = new Dictionary<char, Color>();
            // var boxes = new List<Box>();
            var line = levelReader.ReadLine();
            while (!line.StartsWith("#"))
            {
                var split = line.Split(":");
                var color = ColorExtension.FromString(split[0].Trim());
                var entities = split[1].Split(",");
                foreach (var entity in entities)
                {
                    var c = entity.Trim()[0];
                    if ('0' <= c && c <= '9')
                    {
                        agentColors.Add(c - '0', color);
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        boxColors.Add(c, color);
                    }
                }

                line = levelReader.ReadLine();
            }

            // Read initial state
            // line is currently "#initial"
            var rowsCount = 0;
            var columnsCount = 0;
            var levelLines = new List<string>();
            line = levelReader.ReadLine();
            while (!line.StartsWith("#"))
            {
                levelLines.Add(line);
                columnsCount = Math.Max(columnsCount, line.Length);
                ++rowsCount;
                line = levelReader.ReadLine();
            }

            int row;
            var agents = new List<Agent>();
            var boxes = new List<Box>();
            var walls = new bool[rowsCount, columnsCount];
            for (row = 0; row < rowsCount; ++row)
            {
                line = levelLines[row];
                for (var column = 0; column < line.Length; ++column)
                {
                    var c = line[column];

                    if ('0' <= c && c <= '9')
                    {
                        agents.Add(new Agent(c - '0', agentColors[c - '0'], new Position(row, column)));
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        boxes.Add(new Box(c, boxColors[c], new Position(row, column)));
                    }
                    else if (c == '+')
                    {
                        walls[row, column] = true;
                    }
                }
            }

            // Read goal state
            // line is currently "#goal"
            var agentGoals = new List<Agent>();
            var boxGoals = new List<Box>();
            line = levelReader.ReadLine(); // Console.ReadLine();
            row = 0;
            while (!line.StartsWith("#"))
            {
                for (var column = 0; column < line.Length; ++column)
                {
                    var c = line[column];

                    if (('0' <= c && c <= '9'))
                    {
                        agentGoals.Add(new Agent(c - '0', agentColors[c - '0'], new Position(row, column)));
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        boxGoals.Add(new Box(c, boxColors[c], new Position(row, column)));
                    }
                }

                ++row;
                line = levelReader.ReadLine(); // Console.ReadLine();
            }

            AgentGoals = agentGoals;
            BoxGoals = boxGoals;
            Walls = walls;
            Boxes = boxes;
            Agents = agents;

            Rows = rowsCount;
            Columns = columnsCount;
        }
    }
}
