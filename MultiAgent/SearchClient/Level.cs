using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient
{
    public class Level
    {
        public static List<Agent> Agents;
        public static List<Agent> AgentGoals;
        public static List<Box> Boxes;
        public static List<Box> BoxGoals;
        public static bool[,] Walls;

        public static int WallCount = 0;
        public static bool UseBfs => Rows * Columns - WallCount < 310;

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
                var filePath = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName +
                               "/levels/" + levelName;
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
                        var boxColor = boxColors[c];

                        if (!agentColors.Values.Contains(boxColor))
                        {
                            // No agent can move the box - consider it a wall
                            walls[row, column] = true;
                            WallCount += 1;
                            continue;
                        }

                        boxes.Add(new Box(c, boxColor, new Position(row, column)));
                    }
                    else if (c == '+')
                    {
                        walls[row, column] = true;
                        WallCount += 1;
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

            Console.Error.WriteLine("Starting initialization of distance map");
            InitializeDistanceMap();
            Console.Error.WriteLine("Distance map initialized");
        }

        public static Dictionary<(Position From, Position To), int> DistanceBetweenPositions = new();

        public static void InitializeDistanceMap()
        {
            DistanceBetweenPositions = new Dictionary<(Position From, Position To), int>();

            // Create graph representation of level in order to pre-analyze levle
            var graph = new Graph();

            for (int firstPositionRow = 1; firstPositionRow < Walls.GetLength(0); firstPositionRow++)
            {
                for (int firstPositionCol = 1; firstPositionCol < Walls.GetLength(1); firstPositionCol++)
                {
                    // For each cell in the level that is NOT a wall:
                    if (Walls[firstPositionRow, firstPositionCol])
                    {
                        continue;
                    }

                    // Iterate over every other position
                    for (int secondPositionRow = 1; secondPositionRow < Walls.GetLength(0); secondPositionRow++)
                    {
                        for (int secondPositionCol = 1; secondPositionCol < Walls.GetLength(1); secondPositionCol++)
                        {
                            // For each cell in the level that is NOT a wall:
                            if (Walls[secondPositionRow, secondPositionCol])
                            {
                                continue;
                            }

                            var positionFrom = new Position(firstPositionRow, firstPositionCol);
                            var positionTo = new Position(secondPositionRow, secondPositionCol);

                            if (DistanceBetweenPositions.ContainsKey((positionFrom, positionTo)) ||
                                DistanceBetweenPositions.ContainsKey((positionTo, positionFrom)))
                            {
                                continue;
                            }

                            // If positions equal - distance between them = 0
                            if (positionFrom.Equals(positionTo))
                            {
                                DistanceBetweenPositions.Add((positionFrom, positionTo), 0);

                                continue;
                            }

                            var startNode = graph.NodeGrid[firstPositionRow, firstPositionCol];
                            var finishNode = graph.NodeGrid[secondPositionRow, secondPositionCol];

                            var distance = UseBfs
                                ? graph.BFS(startNode, finishNode)
                                : Math.Abs(firstPositionRow - secondPositionRow) +
                                  Math.Abs(firstPositionCol - secondPositionCol);

                            DistanceBetweenPositions.Add((positionFrom, positionTo), distance);
                            DistanceBetweenPositions.Add((positionTo, positionFrom), distance);
                        }
                    }
                }
            }
        }
    }
}
