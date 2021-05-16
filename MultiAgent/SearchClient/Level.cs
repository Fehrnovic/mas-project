using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using MultiAgent.SearchClient.CBS;
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
        public static Graph Graph;
        public static bool[,] OutsideWorld;

        public static int WallCount = 0;

        public static bool UseBfs = false;

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

            int row, column;
            var agents = new List<Agent>();
            var boxes = new List<Box>();
            var walls = new bool[rowsCount, columnsCount];
            var outsideMapPositions = new List<Position>();
            for (row = 0; row < rowsCount; ++row)
            {
                line = levelLines[row];
                for (column = 0; column < line.Length; ++column)
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

                    // Add outer row if not wall to outside map
                    if (((row == 0 || column == 0) || (row == rowsCount - 1 || column == columnsCount - 1)) && c != '+')
                    {
                        outsideMapPositions.Add(new Position(row, column));
                    }
                }

                for (var i = line.Length; i < columnsCount; i++)
                {
                    outsideMapPositions.Add(new Position(row, i));
                }
            }

            //Remove all bad position neighbors
            var badPositions = new bool[rowsCount, columnsCount];
            foreach (var mapPosition in outsideMapPositions)
            {
                if (badPositions[mapPosition.Row, mapPosition.Column])
                {
                    // Position already explored
                    continue;
                }

                Queue<Position> queue = new();
                List<Position> visitedNodes = new();

                queue.Enqueue(mapPosition);
                visitedNodes.Add(mapPosition);

                while (queue.Any())
                {
                    var currentNode = queue.Dequeue();
                    badPositions[currentNode.Row, currentNode.Column] = true;

                    var badNeighbors = new List<Position>();
                    if (currentNode.Row > 0)
                    {
                        if (walls[currentNode.Row - 1, currentNode.Column] == false)
                        {
                            badNeighbors.Add(new Position(currentNode.Row - 1, currentNode.Column));
                        }
                    }

                    if (currentNode.Row < rowsCount - 1)
                    {
                        if (walls[currentNode.Row + 1, currentNode.Column] == false)
                        {
                            badNeighbors.Add(new Position(currentNode.Row + 1, currentNode.Column));
                        }
                    }

                    if (currentNode.Column > 0)
                    {
                        if (walls[currentNode.Row, currentNode.Column - 1] == false)
                        {
                            badNeighbors.Add(new Position(currentNode.Row, currentNode.Column - 1));
                        }
                    }

                    if (currentNode.Column < columnsCount - 1)
                    {
                        if (walls[currentNode.Row, currentNode.Column + 1] == false)
                        {
                            badNeighbors.Add(new Position(currentNode.Row, currentNode.Column + 1));
                        }
                    }

                    badNeighbors.ForEach(neighbor =>
                    {
                        if (!visitedNodes.Contains(neighbor))
                        {
                            visitedNodes.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    });
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
                for (column = 0; column < line.Length; ++column)
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
            OutsideWorld = badPositions;
            Boxes = boxes;
            Agents = agents;

            Rows = rowsCount;
            Columns = columnsCount;

            Graph = new Graph();
            for (var i = 0; i < rowsCount; i++)
            {
                for (var j = 0; j < columnsCount; j++)
                {
                    var graphNode = Graph.NodeGrid[i, j];
                    if (graphNode == null)
                    {
                        continue;
                    }
                }
            }
            
            var modifiedLevel = false;
            foreach (var box in boxes)
            {
                var agentOfSameColor = agents.Where(a => a.Color == box.Color).ToList();

                if (!agentOfSameColor.Exists(a =>
                    GetDistanceBetweenPosition(box.GetInitialLocation(), a.GetInitialLocation()) < int.MaxValue))
                {
                    Walls[box.GetInitialLocation().Row, box.GetInitialLocation().Column] = true;
                    modifiedLevel = true;
                }
            }

            if (modifiedLevel)
            {
                Graph = new Graph();
                for (var i = 0; i < rowsCount; i++)
                {
                    for (var j = 0; j < columnsCount; j++)
                    {
                        var graphNode = Graph.NodeGrid[i, j];
                        if (graphNode == null)
                        {
                            continue;
                        }
                    }
                }
            }

            if (Program.ShouldPrint >= 2)
            {
                Console.Error.WriteLine((double) WallCount / ((double) Rows * (double) Columns));
            }
            // if ((double) WallCount / ((double) Rows * (double) Columns) < 0.2)
            // {
                // Console.Error.WriteLine("Does not use bfs");
                // UseBfs = false;
            // }

            if (Program.ShouldPrint >= 2)
            {
                Console.Error.WriteLine("Initialize delegation data");
            }


            LevelDelegationHelper.InitializeDelegationData();

            if (Program.ShouldPrint >= 2)
            {
                Console.Error.WriteLine("Delegation data initialized");
            }

            if (Program.ShouldPrint >= 2)
            {
                Console.Error.WriteLine("Starting level delegation");
            }

            LevelDelegationHelper.DelegateLevel();

            if (Program.ShouldPrint >= 2)
            {
                Console.Error.WriteLine("Level delegated");
            }
        }

        private static Dictionary<(Position From, Position To), int> DistanceBetweenPositions = new();

        public static int GetDistanceBetweenPosition(Position from, Position to)
        {
            if (!UseBfs)
            {
                return Math.Abs(from.Row - to.Row) + Math.Abs(from.Column - to.Column);
            }

            if (from == to)
            {
                return 0;
            }

            if (DistanceBetweenPositions.ContainsKey((from, to)) ||
                DistanceBetweenPositions.ContainsKey((to, from)))
            {
                var temp = DistanceBetweenPositions[(from, to)];
                return temp;
            }

            var startNode = Graph.NodeGrid[from.Row, from.Column];
            var finishNode = Graph.NodeGrid[to.Row, to.Column];

            if (startNode == null || finishNode == null)
            {
                return int.MaxValue;
            }

            var distance = Graph.BFS(startNode, finishNode);

            DistanceBetweenPositions.Add((from, to), distance);
            DistanceBetweenPositions.Add((to, from), distance);

            return distance;
        }
    }
}
