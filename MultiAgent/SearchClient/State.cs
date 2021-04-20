using System;
using System.Collections.Generic;
using System.Text;

namespace MultiAgent.SearchClient
{
    public class State
    {
        // State information
        public Action[] JointActions;
        public readonly Dictionary<Position, Box> PositionsOfBoxes;

        // Both a reference from Agent -> Position and Position -> Agent is kept to allow for quick lookup
        public readonly Dictionary<Position, Agent> PositionsOfAgents;
        public readonly Position[] AgentPositions;

        public State Parent;
        public int Depth;

        private int Hash = 0;

        public State(List<Agent> agents, List<Box> boxes)
        {
            PositionsOfBoxes = new Dictionary<Position, Box>(Level.Boxes.Count);
            PositionsOfAgents = new Dictionary<Position, Agent>(Level.Agents.Count);
            AgentPositions = new Position[Level.Agents.Count];

            foreach (var agent in agents)
            {
                PositionsOfAgents.Add(agent.GetInitialLocation(), agent);
                AgentPositions[agent.Number] = agent.GetInitialLocation();
            }

            foreach (var box in boxes)
            {
                PositionsOfBoxes.Add(box.GetInitialLocation(), box);
            }
        }

        public State(State parent, Action[] jointActions)
        {
            PositionsOfBoxes = new Dictionary<Position, Box>(Level.Boxes.Count);
            PositionsOfAgents = new Dictionary<Position, Agent>(Level.Agents.Count);
            AgentPositions = new Position[Level.Agents.Count];

            // Copy parent information
            CopyParentInformation(parent);

            // Copy joint actions
            CopyJointActions(jointActions);

            // Apply actions
            ApplyActions();
        }

        private void CopyJointActions(Action[] jointActions)
        {
            JointActions = jointActions;
        }

        private void CopyParentInformation(State parent)
        {
            foreach (var (boxPosition, box) in parent.PositionsOfBoxes)
            {
                PositionsOfBoxes.Add(boxPosition, box);
            }

            Parent = parent;
            Depth = parent.Depth + 1;
        }

        private void ApplyActions()
        {
            foreach (var agent in Level.Agents)
            {
                var agentAction = JointActions[agent.Number];
                var agentPosition = Parent.AgentPositions[agent.Number];;
                Box box;

                switch (agentAction.Type)
                {
                    case ActionType.NoOp:
                        PositionsOfAgents.Add(agentPosition, agent);
                        AgentPositions[agent.Number] = agentPosition;

                        break;
                    case ActionType.Move:
                        // Move agent:
                        agentPosition = new Position(
                            agentPosition.Row + agentAction.AgentRowDelta,
                            agentPosition.Column + agentAction.AgentColumnDelta
                        );
                        PositionsOfAgents.Add(agentPosition, agent);
                        AgentPositions[agent.Number] = agentPosition;

                        break;
                    case ActionType.Push:
                        // Move agent:
                        agentPosition = new Position(
                            agentPosition.Row + agentAction.AgentRowDelta,
                            agentPosition.Column + agentAction.AgentColumnDelta
                        );
                        PositionsOfAgents.Add(agentPosition, agent);
                        AgentPositions[agent.Number] = agentPosition;

                        // Get the box character from parent
                        Parent.PositionsOfBoxes.TryGetValue(agentPosition, out box);

                        // Set the new location:
                        PositionsOfBoxes.Remove(agentPosition);
                        PositionsOfBoxes.Add(
                            new Position(
                                agentPosition.Row + agentAction.BoxRowDelta,
                                agentPosition.Column + agentAction.BoxColumnDelta
                            ),
                            box
                        );

                        break;
                    case ActionType.Pull:
                        // Find box before pull
                        var oldBoxPosition = new Position(
                            agentPosition.Row - agentAction.BoxRowDelta,
                            agentPosition.Column - agentAction.BoxColumnDelta
                        );

                        // Get the box from parent
                        Parent.PositionsOfBoxes.TryGetValue(oldBoxPosition, out box);

                        // Move agent
                        agentPosition = new Position(
                            agentPosition.Row + agentAction.AgentRowDelta,
                            agentPosition.Column + agentAction.AgentColumnDelta
                        );
                        PositionsOfAgents.Add(agentPosition, agent);
                        AgentPositions[agent.Number] = agentPosition;

                        // Update box position
                        // Remove old location:
                        PositionsOfBoxes.Remove(oldBoxPosition);
                        // Set new location:
                        PositionsOfBoxes.Add(
                            new Position(
                                oldBoxPosition.Row + agentAction.BoxRowDelta,
                                oldBoxPosition.Column + agentAction.BoxColumnDelta
                            ),
                            box
                        );

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public List<State> GetExpandedStates()
        {
            // Determine list of applicable actions for each individual agent.
            var applicableActions = new Dictionary<Agent, List<Action>>(Level.Agents.Count);
            foreach (var agent in Level.Agents)
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
            var actionsPermutation = new int[Level.Agents.Count];
            var expandedStates = new List<State>(32);
            while (true)
            {
                var jointActions = new Action[Level.Agents.Count];
                foreach (var agent in Level.Agents)
                {
                    jointActions[agent.Number] = applicableActions[agent][actionsPermutation[agent.Number]];
                }

                if (!IsConflicting(jointActions))
                {
                    expandedStates.Add(new State(this, jointActions));
                }

                // Advance permutation
                var done = false;
                foreach (var agent in Level.Agents)
                {
                    if (actionsPermutation[agent.Number] < applicableActions[agent].Count - 1)
                    {
                        ++actionsPermutation[agent.Number];
                        break;
                    }
                    else
                    {
                        actionsPermutation[agent.Number] = 0;
                        if (agent.Number == Level.Agents.Count - 1)
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

        private bool IsConflicting(Action[] jointActions)
        {
            var destinationRows = new int[Level.Agents.Count]; // row of new cell to become occupied by action
            var destinationColumns = new int[Level.Agents.Count]; // column of new cell to become occupied by action
            var boxRows = new int[Level.Agents.Count]; // current row of box moved by action
            var boxColumns = new int[Level.Agents.Count]; // current column of box moved by action

            foreach (var agent in Level.Agents)
            {
                var action = jointActions[agent.Number];
                int boxRow;
                int boxColumn;
                Position agentPosition;

                switch (action.Type)
                {
                    case ActionType.NoOp:
                        break;

                    // Move and pull behave similarly with conflicts, since with a pull, only the agent moves to a square that must be free
                    case ActionType.Move:
                        agentPosition = PositionOfAgent(agent);

                        // Calculate destination of agent
                        destinationRows[agent.Number] = agentPosition.Row + action.AgentRowDelta;
                        destinationColumns[agent.Number] = agentPosition.Column + action.AgentColumnDelta;

                        break;

                    case ActionType.Pull:
                        agentPosition = PositionOfAgent(agent);

                        // Calculate destination of agent
                        destinationRows[agent.Number] = agentPosition.Row + action.AgentRowDelta;
                        destinationColumns[agent.Number] = agentPosition.Column + action.AgentColumnDelta;

                        boxRow = agentPosition.Row - action.BoxRowDelta;
                        boxColumn = agentPosition.Column - action.BoxColumnDelta;

                        boxRows[agent.Number] = boxRow;
                        boxColumns[agent.Number] = boxColumn;

                        break;

                    case ActionType.Push:
                        agentPosition = PositionOfAgent(agent);

                        // Get current location of box
                        boxRow = agentPosition.Row + action.AgentRowDelta;
                        boxColumn = agentPosition.Column + action.AgentColumnDelta;

                        // Calculate destination of box
                        destinationRows[agent.Number] = boxRow + action.BoxRowDelta;
                        destinationColumns[agent.Number] = boxColumn + action.BoxColumnDelta;

                        boxRows[agent.Number] = boxRow;
                        boxColumns[agent.Number] = boxColumn;
                        break;
                }
            }

            for (var agent1 = 0; agent1 < Level.Agents.Count; ++agent1)
            {
                if (jointActions[agent1].Type == ActionType.NoOp)
                {
                    continue;
                }

                for (var agent2 = agent1 + 1; agent2 < Level.Agents.Count; ++agent2)
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
            return !Level.Walls[position.Row, position.Column] && BoxAt(position) == null && AgentAt(position) == null;
        }

        private Agent AgentAt(Position position)
        {
            return PositionsOfAgents.TryGetValue(position, out var agent) ? agent : null;
        }

        private Position PositionOfAgent(Agent agent)
        {
            return AgentPositions[agent.Number];
        }

        private Box BoxAt(Position position)
        {
            return PositionsOfBoxes.TryGetValue(position, out var box) ? box : null;
        }

        public bool IsGoalState()
        {
            foreach (var agentGoal in Level.AgentGoals)
            {
                if (!PositionsOfAgents.TryGetValue(agentGoal.GetInitialLocation(), out var agent))
                {
                    return false;
                }

                if (agentGoal.Number != agent.Number)
                {
                    return false;
                }
            }

            foreach (var boxGoal in Level.BoxGoals)
            {
                if (!PositionsOfBoxes.TryGetValue(boxGoal.GetInitialLocation(), out var box))
                {
                    return false;
                }

                if (boxGoal.Letter != box.Letter)
                {
                    return false;
                }
            }

            return true;
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
            for (var row = 0; row < Level.Walls.GetLength(0); row++)
            {
                for (var column = 0; column < Level.Walls.GetLength(1); column++)
                {
                    var box = BoxAt(new Position(row, column));
                    var agent = AgentAt(new Position(row, column));
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

            return true;
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
                result = prime * result + (((agentPosition.Row + 1) * 21) * Level.Agents.Count + (agentPosition.Column + 1) * 32) * (agent.Number + 1);
            }

            foreach (var (boxPosition, box) in PositionsOfBoxes)
            {
                result = prime * result + (((boxPosition.Row + 1) * 41) * Level.Walls.GetLength(0) + (boxPosition.Column + 1) * 62) * box.Letter;
            }

            Hash = result;

            return Hash;
        }
    }
}
