using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiAgent.searchClient
{
    public class State
    {
        // Static properties
        public static List<Agent> AgentGoals;
        public static List<Box> BoxGoals;
        public static bool[,] Walls;

        // State information
        public readonly List<Agent> Agents;
        public readonly List<Box> Boxes;
        public Action[] JointActions;

        public State Parent;
        public int Depth;

        private int Hash = 0;

        public State(List<Agent> agents, List<Box> boxes)
        {
            Agents = agents;
            Boxes = boxes;
        }

        public State(State parent, Action[] jointActions)
        {
            // Copy parent information
            Agents = parent.Agents.Select(a => new Agent(a.Number, a.Color, new Position(a.Position.Row, a.Position.Column))).ToList();
            Boxes = parent.Boxes.Select(b => new Box(b.Letter, b.Color, new Position(b.Position.Row, b.Position.Column))).ToList();

            Parent = parent;
            JointActions = jointActions.Select(
                a => new Action(a.Name, a.Type, a.AgentRowDelta, a.AgentColumnDelta, a.BoxRowDelta, a.BoxColumnDelta)
            ).ToArray();
            Depth = parent.Depth + 1;

            // Apply actions
            foreach (var agent in Agents)
            {
                var agentAction = jointActions[agent.Number];

                Box box;

                switch (agentAction.Type)
                {
                    case ActionType.NoOp:
                        break;
                    case ActionType.Move:
                        agent.Position.Row += agentAction.AgentRowDelta;
                        agent.Position.Column += agentAction.AgentColumnDelta;

                        break;
                    case ActionType.Push:
                        agent.Position.Row += agentAction.AgentRowDelta;
                        agent.Position.Column += agentAction.AgentColumnDelta;

                        // Get the box character
                        box = BoxAt(new Position(agent.Position.Row, agent.Position.Column));

                        box.Position.Row += agentAction.BoxRowDelta;
                        box.Position.Column += agentAction.BoxColumnDelta;

                        break;
                    case ActionType.Pull:
                        // Find box before pull
                        box = BoxAt(new Position(agent.Position.Row - agentAction.BoxRowDelta,
                            agent.Position.Column - agentAction.BoxColumnDelta));

                        // Move agent
                        agent.Position.Row += agentAction.AgentRowDelta;
                        agent.Position.Column += agentAction.AgentColumnDelta;


                        // Update box position
                        box.Position.Row += agentAction.BoxRowDelta;
                        box.Position.Column += agentAction.BoxColumnDelta;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public List<State> GetExpandedStates()
        {
            // Determine list of applicable actions for each individual agent.
            var applicableActions = new Dictionary<Agent, List<Action>>(Agents.Count);
            foreach (var agent in Agents)
            {
                var applicableAgentActions = new List<Action>(Action.AllActions.Count);
                foreach (var action in Action.AllActions)
                {
                    if (IsApplicable(agent, action))
                    {
                        applicableAgentActions.Add(action);
                    }
                }

                applicableActions.Add(agent, applicableAgentActions);
            }

            // Iterate over joint actions, check conflict and generate child states.
            var jointAction = new Action[Agents.Count];
            var actionsPermutation = new int[Agents.Count];
            var expandedStates = new List<State>(32);
            while (true)
            {
                foreach (var agent in Agents)
                {
                    jointAction[agent.Number] = applicableActions[agent][actionsPermutation[agent.Number]];
                }

                if (!IsConflicting(jointAction))
                {
                    expandedStates.Add(new State(this, jointAction));
                }

                // Advance permutation
                var done = false;
                foreach (var agent in Agents)
                {
                    if (actionsPermutation[agent.Number] < applicableActions[agent].Count - 1)
                    {
                        ++actionsPermutation[agent.Number];
                        break;
                    }
                    else
                    {
                        actionsPermutation[agent.Number] = 0;
                        if (agent.Number == Agents.Count - 1)
                        {
                            done = true;
                        }
                    }
                }

                // Last permutation?
                if (done)
                {
                    break;
                }
            }

            return expandedStates;
        }

        private bool IsApplicable(Agent agent, Action action)
        {
            int boxRow;
            int boxColumn;
            Box box;
            int destinationRow;
            int destinationColumn;

            switch (action.Type)
            {
                case ActionType.NoOp:
                    return true;

                case ActionType.Move:
                    destinationRow = agent.Position.Row + action.AgentRowDelta;
                    destinationColumn = agent.Position.Column + action.AgentColumnDelta;

                    return CellIsFree(new Position(destinationRow, destinationColumn));

                case ActionType.Push:
                    // Calculate the possible location of the box to be moved
                    boxRow = agent.Position.Row + action.AgentRowDelta;
                    boxColumn = agent.Position.Column + action.AgentColumnDelta;

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
                    // Calculate the destination of the agent
                    destinationRow = agent.Position.Row + action.AgentRowDelta;
                    destinationColumn = agent.Position.Column + action.AgentColumnDelta;

                    // Calculate the possible location of the box to be moved
                    boxRow = agent.Position.Row - action.BoxRowDelta;
                    boxColumn = agent.Position.Column - action.BoxColumnDelta;

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

        private bool IsConflicting(Action[] jointActions)
        {
            var destinationRows = new int[Agents.Count]; // row of new cell to become occupied by action
            var destinationColumns = new int[Agents.Count]; // column of new cell to become occupied by action
            var boxRows = new int[Agents.Count]; // current row of box moved by action
            var boxColumns = new int[Agents.Count]; // current column of box moved by action

            foreach (var agent in Agents)
            {
                var action = jointActions[agent.Number];
                int boxRow;
                int boxColumn;

                switch (action.Type)
                {
                    case ActionType.NoOp:
                        break;

                    // Move and pull behave similarly with conflicts, since with a pull, only the agent moves to a square that must be free
                    case ActionType.Move:
                        // Calculate destination of agent
                        destinationRows[agent.Number] = agent.Position.Row + action.AgentRowDelta;
                        destinationColumns[agent.Number] = agent.Position.Column + action.AgentColumnDelta;

                        break;

                    case ActionType.Pull:
                        // Calculate destination of agent
                        destinationRows[agent.Number] = agent.Position.Row + action.AgentRowDelta;
                        destinationColumns[agent.Number] = agent.Position.Column + action.AgentColumnDelta;

                        boxRow = agent.Position.Row - action.BoxRowDelta;
                        boxColumn = agent.Position.Column - action.BoxColumnDelta;

                        boxRows[agent.Number] = boxRow;
                        boxColumns[agent.Number] = boxColumn;

                        break;

                    case ActionType.Push:
                        // Get current location of box
                        boxRow = agent.Position.Row + action.AgentRowDelta;
                        boxColumn = agent.Position.Column + action.AgentColumnDelta;

                        // Calculate destination of box
                        destinationRows[agent.Number] = boxRow + action.BoxRowDelta;
                        destinationColumns[agent.Number] = boxColumn + action.BoxColumnDelta;

                        boxRows[agent.Number] = boxRow;
                        boxColumns[agent.Number] = boxColumn;
                        break;
                }
            }

            for (var agent1 = 0; agent1 < Agents.Count; ++agent1)
            {
                if (jointActions[agent1].Type == ActionType.NoOp)
                {
                    continue;
                }

                for (int agent2 = agent1 + 1; agent2 < Agents.Count; ++agent2)
                {
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
                    if (boxRows[agent1] == boxRows[agent2] && boxColumns[agent1] == boxColumns[agent2])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CellIsFree(Position position)
        {
            return !Walls[position.Row, position.Column] && BoxAt(position) == null && AgentAt(position) == null;
        }

        private Agent AgentAt(Position position)
        {
            return Agents.FirstOrDefault(a => a.Position.Equals(position));
        }

        private Box BoxAt(Position position)
        {
            return Boxes.FirstOrDefault(b => b.Position.Equals(position));
        }

        public bool IsGoalState()
        {
            var agentsMatch = !AgentGoals.Any() ||
                              AgentGoals.All(agentGoal =>
                                  Agents.Exists(agent =>
                                      agentGoal.Number == agent.Number && agentGoal.Position.Equals(agent.Position)));

            var boxesMatch = !BoxGoals.Any() || BoxGoals.All(boxGoal =>
                Boxes.Exists(box => boxGoal.Letter == box.Letter && boxGoal.Position.Equals(box.Position)));

            return agentsMatch && boxesMatch;
        }

        public Action[][] ExtractPlan()
        {
            var plan = new Action[Depth][];
            var state = this;
            while (state.JointActions != null)
            {
                plan[state.Depth - 1] = state.JointActions;
                state = state.Parent;
            }

            return plan;
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            for (var row = 0; row < Walls.GetLength(0); row++)
            {
                for (var column = 0; column < Walls.GetLength(1); column++)
                {
                    var box = BoxAt(new Position(row, column));
                    var agent = AgentAt(new Position(row, column));
                    if (box != null)
                    {
                        s.Append(box.Letter);
                    }
                    else if (Walls[row, column])
                    {
                        s.Append('+');
                    }
                    else if (agent != null)
                    {
                        s.Append(agent.Number);
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
            if (obj is not State state)
            {
                return false;
            }

            // This should never occur but is kept for safety
            // if (Boxes.Count != state.Boxes.Count || Agents.Count != state.Agents.Count)
            // {
            //     return false;
            // }

            return !Boxes.Except(state.Boxes).Any() && !Agents.Except(state.Agents).Any();
        }

        public override int GetHashCode()
        {
            if (Hash != 0)
            {
                return Hash;
            }

            var prime = 31;
            var result = 1;

            foreach (var agent in Agents)
            {
                result = prime * result + (((agent.Position.Row + 1) * 21) * Agents.Count + (agent.Position.Column + 1) * 32) * (agent.Number + 1);
            }

            foreach (var box in Boxes)
            {
                result = prime * result + (((box.Position.Row + 1) * 41) * Walls.GetLength(0) + (box.Position.Column + 1) * 62) * box.Letter;
            }

            Hash = result;

            return Hash;
        }
    }
}
