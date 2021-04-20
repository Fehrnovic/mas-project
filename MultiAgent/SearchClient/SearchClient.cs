using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient
{
    public static class SearchClient
    {
        public static State ParseLevel(LevelReader levelReader)
        {
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
            var numRows = 0;
            var numCols = 0;
            var levelLines = new List<string>();
            line = levelReader.ReadLine();
            while (!line.StartsWith("#"))
            {
                levelLines.Add(line);
                numCols = Math.Max(numCols, line.Length);
                ++numRows;
                line = levelReader.ReadLine();
            }

            int row;
            var agents = new List<Agent>();
            var boxes = new List<Box>();
            var walls = new bool[numRows, numCols];
            for (row = 0; row < numRows; ++row)
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

            Level.AgentGoals = agentGoals;
            Level.BoxGoals = boxGoals;
            Level.Walls = walls;
            Level.Boxes = boxes;
            Level.Agents = agents;

            return new State(agents, boxes);
        }
    }

    public class LevelReader
    {
        private readonly Type _readerType;
        private int _fileCounter;
        private readonly string[] _fileBuffer;

        public LevelReader(Type readerType, string[] fileBuffer = null)
        {
            _readerType = readerType;
            _fileBuffer = fileBuffer;

            if (_readerType == Type.File && fileBuffer == null)
            {
                throw new ArgumentException("FileBuffer is needed when type if File");
            }
        }

        public string ReadLine()
        {
            return _readerType switch
            {
                Type.Console => Console.ReadLine(),
                Type.File => _fileBuffer[_fileCounter++],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public enum Type
        {
            Console,
            File,
        }
    }
}
