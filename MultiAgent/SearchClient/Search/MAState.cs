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

        public List<Action> JointActions;

        public MAState Parent;
        public int Time;
        public HashSet<Constraint> Constraints;

        private int Hash = 0;

        public MAState(List<Agent> agents, List<Agent> agentGoals, List<Box> boxes, List<Box> boxGoals,
            HashSet<Constraint> constraints)
        {
            Agents = agents;
            AgentGoals = agentGoals;

            Boxes = boxes;
            BoxGoals = boxGoals;

            AgentPositions = new Dictionary<Agent, Position>(Agents.Count);
            PositionsOfAgents = new Dictionary<Position, Agent>(Agents.Count);
            foreach (var agent in Agents)
            {
                AgentPositions.Add(agent, agent.GetInitialLocation());
                PositionsOfAgents.Add(agent.GetInitialLocation(), agent);
            }

            PositionsOfBoxes = new Dictionary<Position, Box>(Boxes.Count);
            foreach (var box in Boxes)
            {
                PositionsOfBoxes.Add(box.GetInitialLocation(), box);
            }

            Constraints = constraints;
        }
        
        public MAState(MAState parent, List<Action> jointActions)
        {
            Parent = parent;
            JointActions = jointActions;
            Time = parent.Time + 1;
            Constraints = parent.Constraints;

            Agents = parent.Agents;
            AgentGoals = parent.AgentGoals;
            AgentPositions = new Dictionary<Agent, Position>(parent.Agents.Count);
            PositionsOfAgents = new Dictionary<Position, Agent>(parent.Agents.Count);
            foreach (var (agentPosition, agent) in parent.PositionsOfAgents)
            {
                AgentPositions.Add(agent, agentPosition);
                PositionsOfAgents.Add(agentPosition, agent);
            }

            Boxes = parent.Boxes;
            BoxGoals = parent.BoxGoals;
            PositionsOfBoxes = new Dictionary<Position, Box>(parent.Boxes.Count);
            foreach (var (boxPosition, currentBox) in parent.PositionsOfBoxes)
            {
                PositionsOfBoxes.Add(boxPosition, currentBox);
            }

            foreach (var agent in Agents)
            {
                var agentAction = jointActions[agent.Number];

                Position agentPosition;
                Box box;

                switch (agentAction.Type)
                {
                    case ActionType.NoOp:
                        break;
                    case ActionType.Move:
                        agentPosition = PositionOfAgent(agent);

                        // Remove old position:
                        PositionsOfAgents.Remove(agentPosition);
                        AgentPositions.Remove(agent);

                        // Move agent:
                        agentPosition = new Position(
                            agentPosition.Row + agentAction.AgentRowDelta,
                            agentPosition.Column + agentAction.AgentColumnDelta
                        );
                        PositionsOfAgents.Add(agentPosition, agent);
                        AgentPositions.Add(agent, agentPosition);

                        break;
                    case ActionType.Push:
                        agentPosition = PositionOfAgent(agent);

                        // Remove old position:
                        PositionsOfAgents.Remove(agentPosition);
                        AgentPositions.Remove(agent);

                        // Move agent:
                        agentPosition = new Position(
                            agentPosition.Row + agentAction.AgentRowDelta,
                            agentPosition.Column + agentAction.AgentColumnDelta
                        );
                        PositionsOfAgents.Add(agentPosition, agent);
                        AgentPositions.Add(agent, agentPosition);


                        // Get the box character
                        box = BoxAt(agentPosition);
                        // Remove previous location:
                        PositionsOfBoxes.Remove(agentPosition);
                        // Set the new location:
                        PositionsOfBoxes.Add(
                            new Position(
                                agentPosition.Row + agentAction.BoxRowDelta,
                                agentPosition.Column + agentAction.BoxColumnDelta
                            ),
                            box
                        );

                        break;
                    case ActionType.Pull:
                        agentPosition = PositionOfAgent(agent);

                        // Find box before pull
                        var oldBoxPosition = new Position(
                            agentPosition.Row - agentAction.BoxRowDelta,
                            agentPosition.Column - agentAction.BoxColumnDelta
                        );

                        box = BoxAt(oldBoxPosition);

                        // Move agent
                        // Remove old position:
                        PositionsOfAgents.Remove(agentPosition);
                        AgentPositions.Remove(agent);

                        // Move agent:
                        agentPosition = new Position(
                            agentPosition.Row + agentAction.AgentRowDelta,
                            agentPosition.Column + agentAction.AgentColumnDelta
                        );
                        PositionsOfAgents.Add(agentPosition, agent);
                        AgentPositions.Add(agent, agentPosition);

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


        private bool ConstraintsSatisfied()
        {
            var constraints = GetRelevantConstraints();
            var constrainedPositions = constraints.Select(c => c.Position).ToList();

            var conflictingPositions = GetStatePositions().Intersect(constrainedPositions).ToList();

            return !conflictingPositions.Any();
        }

        public List<Position> GetStatePositions()
        {
            var positions = new List<Position> {AgentPosition};
            positions.AddRange(PositionsOfBoxes.Keys);

            return positions;
        }

        private List<Constraint> GetRelevantConstraints()
        {
            return Constraints.Where(c => c.Time == Time).ToList();
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
            var destinationRows = new int[Agents.Count]; // row of new cell to become occupied by action
            var destinationColumns = new int[Agents.Count]; // column of new cell to become occupied by action
            var boxRows = new int[Agents.Count]; // current row of box moved by action
            var boxColumns = new int[Agents.Count]; // current column of box moved by action

            foreach (var agent in Agents)
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

            for (var agent1 = 0; agent1 < Agents.Count; ++agent1)
            {
                if (jointActions[agent1].Type == ActionType.NoOp)
                {
                    continue;
                }

                for (var agent2 = agent1 + 1; agent2 < Agents.Count; ++agent2)
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

        public bool IsGoalState()
        {
            foreach (var agentGoal in AgentGoals)
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

            foreach (var boxGoal in BoxGoals)
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

        public List<SAStep> ExtractPlan()
        {
            var plan = new SAStep[Time + 1];
            var state = this;
            while (state.Action != null)
            {
                plan[state.Time] = new SAStep(state);
                state = state.Parent;
            }

            plan[0] = new SAStep(state);

            return plan.ToList();
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
            for (var row = 0; row < Level.Rows; row++)
            {
                for (var column = 0; column < Level.Columns; column++)
                {
                    var box = BoxAt(new Position(row, column));
                    if (box != null)
                    {
                        s.Append(box.Letter);
                    }
                    else if (Level.Walls[row, column])
                    {
                        s.Append('+');
                    }
                    else if (AgentPosition.Row == row && AgentPosition.Column == column)
                    {
                        s.Append(Agent.Number);
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


            var boxesEqual = true;

            foreach (var (boxPosition, box) in PositionsOfBoxes)
            {
                if (!state.PositionsOfBoxes.TryGetValue(boxPosition, out var box2))
                {
                    boxesEqual = false;
                }

                if (box != box2)
                {
                    boxesEqual = false;
                }
            }

            // TODO: Optimization 
            // return AgentPosition == state.AgentPosition && Time == state.Time;
            var constraints = GetRelevantConstraints();
            var constraints2 = state.GetRelevantConstraints();

            var isEqual = AgentPosition == state.AgentPosition
                          && constraints.Count == constraints2.Count
                          && !constraints.Except(constraints2).Any()
                          && boxesEqual;

            return isEqual;
        }

        public override int GetHashCode()
        {
            if (Hash != 0)
            {
                return Hash;
            }

            var prime = 31;
            var result = 1;

            result = prime * result +
                     ((AgentPosition.Row + 1) * 21 + (AgentPosition.Column + 1) * 32) * (Agent.Number + 1);

            foreach (var (boxPosition, box) in PositionsOfBoxes)
            {
                result = prime * result + (((boxPosition.Row + 1) * 41) * Level.Rows + (boxPosition.Column + 1) * 62) *
                    box.Letter;
            }

            Hash = result;

            return Hash;
        }
    }
}
