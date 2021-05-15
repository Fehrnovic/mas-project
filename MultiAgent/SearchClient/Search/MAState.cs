using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiAgent.SearchClient.Utils;
using MultiAgent.SearchClient.CBS;

namespace MultiAgent.SearchClient.Search
{
    public class MAState
    {
        public List<Agent> Agents;
        public List<Agent> AgentGoals;

        public List<Box> Boxes;
        public List<Box> BoxGoals;

        // State information
        public readonly Dictionary<Agent, Position> AgentPositions;
        public readonly Dictionary<Position, Agent> PositionsOfAgents;
        public readonly Dictionary<Position, Box> PositionsOfBoxes;

        public readonly Dictionary<Agent, Box> AgentToCurrentGoal;
        public readonly Dictionary<Agent, Box> AgentToRelevantBox;

        public Dictionary<Agent, bool> AgentFinishedWithSubGoal;

        public Dictionary<Agent, Action> JointActions;

        public MAState Parent;
        public int Time;
        public HashSet<IConstraint> Constraints;

        private int Hash = 0;

        public MAState(Dictionary<Agent, Position> agents, List<Agent> agentGoals, Dictionary<Position, Box> boxes,
            List<Box> boxGoals,
            HashSet<IConstraint> constraints, Dictionary<Agent, Box> agentToCurrentBoxGoal,
            Dictionary<Agent, Box> agentToRelevantBox)
        {
            Agents = agents.Keys.ToList();
            AgentGoals = agentGoals;

            Boxes = boxes.Values.ToList();
            BoxGoals = boxGoals;

            AgentToCurrentGoal = agentToCurrentBoxGoal;
            AgentToRelevantBox = agentToRelevantBox;

            AgentPositions = new Dictionary<Agent, Position>(Agents.Count);
            PositionsOfAgents = new Dictionary<Position, Agent>(Agents.Count);
            AgentFinishedWithSubGoal = new Dictionary<Agent, bool>(Agents.Count);
            foreach (var (agent, agentPosition) in agents)
            {
                AgentPositions.Add(agent, agentPosition);
                PositionsOfAgents.Add(agentPosition, agent);
                AgentFinishedWithSubGoal.Add(agent, false);
            }

            PositionsOfBoxes = new Dictionary<Position, Box>(Boxes.Count);
            foreach (var (boxPosition, box) in boxes)
            {
                PositionsOfBoxes.Add(boxPosition, box);
            }

            Constraints = constraints.Where(c => Agents.Exists(a => c.Agent == a)).ToHashSet();
        }

        public List<Position> GetStatePositions()
        {
            var positions = new List<Position>();
            positions.AddRange(PositionsOfAgents.Keys);
            positions.AddRange(PositionsOfBoxes.Keys);

            return positions;
        }

        private IEnumerable<IConstraint> GetRelevantConstraints()
        {
            return Constraints.Where(c => c.Relevant(Time));
        }

        private bool IsApplicable(Agent agent, Action action)
        {
            int boxRow;
            int boxColumn;
            Box box;
            int destinationRow;
            int destinationColumn;
            Position agentPosition;

            switch (action.Type)
            {
                case ActionType.NoOp:
                    return true;

                case ActionType.Move:
                    agentPosition = PositionOfAgent(agent);

                    destinationRow = agentPosition.Row + action.AgentRowDelta;
                    destinationColumn = agentPosition.Column + action.AgentColumnDelta;

                    return CellIsFree(new Position(destinationRow, destinationColumn));

                case ActionType.Push:
                    agentPosition = PositionOfAgent(agent);

                    // Calculate the possible location of the box to be moved
                    boxRow = agentPosition.Row + action.AgentRowDelta;
                    boxColumn = agentPosition.Column + action.AgentColumnDelta;

                    // Get the box character
                    box = BoxAt(new Position(boxRow, boxColumn));

                    // If box is not found return false
                    if (box == null)
                    {
                        return false;
                    }

                    // If box and agent colors do not match, then return false
                    if (agent.Color != box.Color)
                    {
                        return false;
                    }

                    // Calculate the destination of the box
                    destinationRow = boxRow + action.BoxRowDelta;
                    destinationColumn = boxColumn + action.BoxColumnDelta;

                    // Check the box can be moved
                    return CellIsFree(new Position(destinationRow, destinationColumn));

                case ActionType.Pull:
                    agentPosition = PositionOfAgent(agent);

                    // Calculate the destination of the agent
                    destinationRow = agentPosition.Row + action.AgentRowDelta;
                    destinationColumn = agentPosition.Column + action.AgentColumnDelta;

                    // Calculate the possible location of the box to be moved
                    boxRow = agentPosition.Row - action.BoxRowDelta;
                    boxColumn = agentPosition.Column - action.BoxColumnDelta;

                    // Get the box character
                    box = BoxAt(new Position(boxRow, boxColumn));

                    // If box is not found return false
                    if (box == null)
                    {
                        return false;
                    }

                    // If box and agent colors do not match, then return false
                    if (agent.Color != box.Color)
                    {
                        return false;
                    }

                    // Check that the agent can be moved
                    return CellIsFree(new Position(destinationRow, destinationColumn));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool IsConflicting(Dictionary<Agent, Action> jointActions)
        {
            var destinationRows =
                new Dictionary<Agent, int>(Agents.Count); // row of new cell to become occupied by action
            var destinationColumns =
                new Dictionary<Agent, int>(Agents.Count); // column of new cell to become occupied by action
            var boxRows = new Dictionary<Agent, int>(Agents.Count); // current row of box moved by action
            var boxColumns = new Dictionary<Agent, int>(Agents.Count); // current column of box moved by action
            foreach (var agent in Agents)
            {
                destinationRows.Add(agent, 0);
                destinationColumns.Add(agent, 0);
                boxRows.Add(agent, 0);
                boxColumns.Add(agent, 0);
            }

            foreach (var agent in Agents)
            {
                var action = jointActions[agent];
                int boxRow;
                int boxColumn;
                Position agentPosition;

                switch (action.Type)
                {
                    case ActionType.NoOp:
                        agentPosition = PositionOfAgent(agent);

                        destinationRows[agent] = agentPosition.Row;
                        destinationColumns[agent] = agentPosition.Column;
                        break;

                    // Move and pull behave similarly with conflicts, since with a pull, only the agent moves to a square that must be free
                    case ActionType.Move:
                        agentPosition = PositionOfAgent(agent);

                        // Calculate destination of agent
                        destinationRows[agent] = agentPosition.Row + action.AgentRowDelta;
                        destinationColumns[agent] = agentPosition.Column + action.AgentColumnDelta;

                        break;

                    case ActionType.Pull:
                        agentPosition = PositionOfAgent(agent);

                        // Calculate destination of agent
                        destinationRows[agent] = agentPosition.Row + action.AgentRowDelta;
                        destinationColumns[agent] = agentPosition.Column + action.AgentColumnDelta;

                        boxRow = agentPosition.Row - action.BoxRowDelta;
                        boxColumn = agentPosition.Column - action.BoxColumnDelta;

                        boxRows[agent] = boxRow;
                        boxColumns[agent] = boxColumn;

                        break;

                    case ActionType.Push:
                        agentPosition = PositionOfAgent(agent);

                        // Get current location of box
                        boxRow = agentPosition.Row + action.AgentRowDelta;
                        boxColumn = agentPosition.Column + action.AgentColumnDelta;

                        // Calculate destination of box
                        destinationRows[agent] = boxRow + action.BoxRowDelta;
                        destinationColumns[agent] = boxColumn + action.BoxColumnDelta;

                        boxRows[agent] = boxRow;
                        boxColumns[agent] = boxColumn;

                        break;
                }
            }

            for (var index1 = 0; index1 < Agents.Count; ++index1)
            {
                var agent1 = Agents[index1];
                if (jointActions[agent1].Type == ActionType.NoOp)
                {
                    continue;
                }

                for (var index2 = index1 + 1; index2 < Agents.Count; ++index2)
                {
                    var agent2 = Agents[index2];
                    if (jointActions[agent2].Type == ActionType.NoOp)
                    {
                        continue;
                    }

                    // Agents or boxes moving into same cell?
                    if (destinationRows[agent1] == destinationRows[agent2] &&
                        destinationColumns[agent1] == destinationColumns[agent2])
                    {
                        return true;
                    }

                    // Agents moving the same box?
                    var isMovingBox = boxRows[agent1] > 0 && boxColumns[agent1] > 0 && boxRows[agent2] > 0 &&
                                      boxColumns[agent2] > 0;
                    var isMovingSameBox =
                        boxRows[agent1] == boxRows[agent2] && boxColumns[agent1] == boxColumns[agent2];

                    if (isMovingBox && isMovingSameBox)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CellIsFree(Position position)
        {
            return !Level.Walls[position.Row, position.Column] && BoxAt(position) == null && AgentAt(position) == null;
        }

        private Position PositionOfAgent(Agent agent)
        {
            AgentPositions.TryGetValue(agent, out var agentPosition);

            return agentPosition;
        }

        private Agent AgentAt(Position position)
        {
            return PositionsOfAgents.TryGetValue(position, out var agent) ? agent : null;
        }

        public Box BoxAt(Position position)
        {
            return PositionsOfBoxes.TryGetValue(position, out var box) ? box : null;
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            for (var row = 0; row < Level.Walls.GetLength(0); row++)
            {
                for (var column = 0; column < Level.Walls.GetLength(1); column++)
                {
                    var box = BoxAt(new Position(row, column));
                    var agent = AgentAt(new Position(row, column));
                    var boxGoal = BoxGoals.FirstOrDefault(b =>
                        b.GetInitialLocation().Row == row && b.GetInitialLocation().Column == column);

                    if (box != null)
                    {
                        s.Append(box.Letter);
                    }
                    else if (Level.Walls[row, column])
                    {
                        s.Append('+');
                    }
                    else if (agent != null)
                    {
                        s.Append(agent.Number);
                    }
                    else if (boxGoal != null)
                    {
                        s.Append(char.ToLower(boxGoal.Letter));
                    }
                    else
                    {
                        s.Append(' ');
                    }
                }

                s.Append('\n');
            }

            return s.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is not MAState state)
            {
                return false;
            }

            foreach (var (agentPosition, agent) in PositionsOfAgents)
            {
                if (!state.PositionsOfAgents.TryGetValue(agentPosition, out var agent2))
                {
                    return false;
                }

                if (agent != agent2)
                {
                    return false;
                }
            }

            foreach (var (boxPosition, box) in PositionsOfBoxes)
            {
                if (!state.PositionsOfBoxes.TryGetValue(boxPosition, out var box2))
                {
                    return false;
                }

                if (box != box2)
                {
                    return false;
                }
            }

            return Time == state.Time;
        }

        public Position GetPositionOfBox(Box box)
        {
            var positionToBox = PositionsOfBoxes.First(pair => pair.Value == box);

            return positionToBox.Key;
        }

        public override int GetHashCode()
        {
            if (Hash != 0)
            {
                return Hash;
            }

            var prime = 31;
            var result = 1;

            foreach (var (agentPosition, agent) in PositionsOfAgents)
            {
                result = prime * result +
                         (((agentPosition.Row + 1) * 21) * Agents.Count + (agentPosition.Column + 1) * 32) *
                         (agent.Number + 1);
            }

            foreach (var (boxPosition, box) in PositionsOfBoxes)
            {
                result = prime * result + (((boxPosition.Row + 1) * 41) * Level.Rows + (boxPosition.Column + 1) * 62) *
                    box.Letter;
            }

            result = prime * result + Time;

            Hash = result;

            return Hash;
        }
    }
}
