using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.searchClient
{
    public static class SearchClient
    {
        public static State ParseLevel(LevelReader levelReader)
        {
            // We can assume that the level file is conforming to specification, since the server verifies this.
            // Read domain
            levelReader.ReadLine(); // #domain // Console.ReadLine(); // #domain
            levelReader.ReadLine(); // hospital // Console.ReadLine(); // hospital

            // Read Level name
            levelReader.ReadLine(); // #levelname // Console.ReadLine(); // #levelname
            levelReader.ReadLine(); // <name> // Console.ReadLine(); // <name>

            // Read colors
            levelReader.ReadLine(); // #colors // Console.ReadLine(); // #colors
            var agents = new List<Agent>();
            var boxes = new List<Box>();
            var line = levelReader.ReadLine(); // Console.ReadLine();
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
                        agents.Add(new Agent(c - '0', color));
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        boxes.Add(new Box(c, color));
                    }
                }

                line = levelReader.ReadLine(); // Console.ReadLine();
            }

            // Read initial state
            // line is currently "#initial"
            var numRows = 0;
            var numCols = 0;
            var levelLines = new List<string>();
            line = levelReader.ReadLine(); // Console.ReadLine();
            while (!line.StartsWith("#"))
            {
                levelLines.Add(line);
                numCols = Math.Max(numCols, line.Length);
                ++numRows;
                line = levelReader.ReadLine(); // Console.ReadLine();
            }

            int row;
            var walls = new bool[numRows, numCols];
            for (row = 0; row < numRows; ++row)
            {
                line = levelLines[row];
                for (var col = 0; col < line.Length; ++col)
                {
                    var c = line[col];

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
            var agentGoals = new List<Agent>();
            var boxGoals = new List<Box>();
            line = levelReader.ReadLine(); // Console.ReadLine();
            row = 0;
            while (!line.StartsWith("#"))
            {
                for (var col = 0; col < line.Length; ++col)
                {
                    var c = line[col];

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
                line = levelReader.ReadLine(); // Console.ReadLine();
            }

            State.AgentGoals = agentGoals;
            State.BoxGoals = boxGoals;
            State.Walls = walls;

            return new State(agents, boxes);
        }

        public static State ParseLevelFromFile(string file)
        {
            var counter = 0;
            var fileBuffer = System.IO.File.ReadAllLines(file);

            // We can assume that the level file is conforming to specification, since the server verifies this.
            // Read domain
            counter++; // Console.ReadLine(); // #domain
            counter++; // Console.ReadLine(); // hospital

            // Read Level name
            counter++; // Console.ReadLine(); // #levelname
            counter++; // Console.ReadLine(); // <name>

            // Read colors
            counter++; // Console.ReadLine(); // #colors
            var agents = new List<Agent>();
            var boxes = new List<Box>();
            var line = fileBuffer[counter++]; // Console.ReadLine();
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
                        agents.Add(new Agent(c - '0', color));
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        boxes.Add(new Box(c, color));
                    }
                }

                line = fileBuffer[counter++]; // Console.ReadLine();
            }

            // Read initial state
            // line is currently "#initial"
            var numRows = 0;
            var numCols = 0;
            var levelLines = new List<string>();
            line = fileBuffer[counter++]; // Console.ReadLine();
            while (!line.StartsWith("#"))
            {
                levelLines.Add(line);
                numCols = Math.Max(numCols, line.Length);
                ++numRows;
                line = fileBuffer[counter++]; // Console.ReadLine();
            }

            int row;
            var walls = new bool[numRows, numCols];
            for (row = 0; row < numRows; ++row)
            {
                line = levelLines[row];
                for (var col = 0; col < line.Length; ++col)
                {
                    var c = line[col];

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
            var agentGoals = new List<Agent>();
            var boxGoals = new List<Box>();
            line = fileBuffer[counter++]; // Console.ReadLine();
            row = 0;
            while (!line.StartsWith("#"))
            {
                for (var col = 0; col < line.Length; ++col)
                {
                    var c = line[col];

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
                line = fileBuffer[counter++]; // Console.ReadLine();
            }

            State.AgentGoals = agentGoals;
            State.BoxGoals = boxGoals;
            State.Walls = walls;

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
