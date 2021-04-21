using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public class State
    {
        // State information
        public readonly Dictionary<Position, Box> PositionsOfBoxes;

        public Agent Agent;
        public Position AgentPosition;
        public Agent AgentGoal;

        public Action Action;

        public State Parent;
        public int Time;
        public HashSet<IConstraint> Constraints;

        private int Hash = 0;

        public State(Agent agent, Agent agentGoal, List<Box> boxes, HashSet<IConstraint> constraints)
        {
            Agent = agent;
            AgentPosition = agent.GetInitialLocation();
            AgentGoal = agentGoal;

            Constraints = constraints.Where(c => c.Agent == Agent).ToHashSet();

            PositionsOfBoxes = new Dictionary<Position, Box>(Level.Boxes.Count);
            foreach (var box in boxes)
            {
                PositionsOfBoxes.Add(box.GetInitialLocation(), box);
            }
        }

        public State(State parent, Action action)
        {
            PositionsOfBoxes = new Dictionary<Position, Box>(Level.Boxes.Count);
            foreach (var (boxPosition, box) in parent.PositionsOfBoxes)
            {
                PositionsOfBoxes.Add(boxPosition, box);
            }

            Parent = parent;
            Time = parent.Time + 1;
            Constraints = parent.Constraints;

            Agent = parent.Agent;
            AgentPosition = parent.AgentPosition;
            AgentGoal = parent.AgentGoal;

            Action = action;

            switch (action.Type)
            {
                case ActionType.NoOp:
                    break;

                case ActionType.Move:
                    AgentPosition = new Position(
                        parent.AgentPosition.Row + action.AgentRowDelta,
                        parent.AgentPosition.Column + action.AgentColumnDelta
                    );
                    break;

                // case ActionType.Push:
                //     // Move agent:
                //     agentPosition = new Position(
                //         agentPosition.Row + agentAction.AgentRowDelta,
                //         agentPosition.Column + agentAction.AgentColumnDelta
                //     );
                //     PositionsOfAgents.Add(agentPosition, agent);
                //     AgentPositions[agent.Number] = agentPosition;
                //
                //     // Get the box character from parent
                //     Parent.PositionsOfBoxes.TryGetValue(agentPosition, out box);
                //
                //     // Set the new location:
                //     PositionsOfBoxes.Remove(agentPosition);
                //     PositionsOfBoxes.Add(
                //         new Position(
                //             agentPosition.Row + agentAction.BoxRowDelta,
                //             agentPosition.Column + agentAction.BoxColumnDelta
                //         ),
                //         box
                //     );
                //
                //     break;
                // case ActionType.Pull:
                //     // Find box before pull
                //     var oldBoxPosition = new Position(
                //         agentPosition.Row - agentAction.BoxRowDelta,
                //         agentPosition.Column - agentAction.BoxColumnDelta
                //     );
                //
                //     // Get the box from parent
                //     Parent.PositionsOfBoxes.TryGetValue(oldBoxPosition, out box);
                //
                //     // Move agent
                //     agentPosition = new Position(
                //         agentPosition.Row + agentAction.AgentRowDelta,
                //         agentPosition.Column + agentAction.AgentColumnDelta
                //     );
                //     PositionsOfAgents.Add(agentPosition, agent);
                //     AgentPositions[agent.Number] = agentPosition;
                //
                //     // Update box position
                //     // Remove old location:
                //     PositionsOfBoxes.Remove(oldBoxPosition);
                //     // Set new location:
                //     PositionsOfBoxes.Add(
                //         new Position(
                //             oldBoxPosition.Row + agentAction.BoxRowDelta,
                //             oldBoxPosition.Column + agentAction.BoxColumnDelta
                //         ),
                //         box
                //     );
                //
                //     break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public List<State> GetExpandedStates()
        {
            // Determine list of applicable actions for the agent
            List<State> reachableStates = new();

            foreach (var action in Action.AllActions)
            {
                if (IsApplicable(action))
                {
                    reachableStates.Add(new State(this, action));
                }
            }

            return reachableStates;
        }

        private bool IsApplicable(Action action)
        {
            var constraints = Constraints.Where(c => c.Time == Time + 1).ToList();
            int destinationRow;
            int destinationColumn;

            switch (action.Type)
            {
                case ActionType.NoOp:
                    foreach (var constraint in constraints)
                    {
                        switch (constraint)
                        {
                            case AgentConstraint agentConstraint:
                                if (agentConstraint.Position == AgentPosition)
                                {
                                    return false;
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(constraint));
                        }
                    }

                    return true;

                case ActionType.Move:
                    destinationRow = AgentPosition.Row + action.AgentRowDelta;
                    destinationColumn = AgentPosition.Column + action.AgentColumnDelta;

                    foreach (var constraint in constraints)
                    {
                        switch (constraint)
                        {
                            case AgentConstraint agentConstraint:
                                if (agentConstraint.Position.Row == destinationRow && agentConstraint.Position.Column == destinationColumn)
                                {
                                    return false;
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(constraint));
                        }
                    }

                    return CellIsFree(new Position(destinationRow, destinationColumn));

                // case ActionType.Push:
                //     agentPosition = PositionOfAgent(agent);
                //
                //     // Calculate the possible location of the box to be moved
                //     boxRow = agentPosition.Row + action.AgentRowDelta;
                //     boxColumn = agentPosition.Column + action.AgentColumnDelta;
                //
                //     // Get the box character
                //     box = BoxAt(new Position(boxRow, boxColumn));
                //
                //     // If box is not found return false
                //     if (box == null)
                //     {
                //         return false;
                //     }
                //
                //     // If box and agent colors do not match, then return false
                //     if (agent.Color != box.Color)
                //     {
                //         return false;
                //     }
                //
                //     // Calculate the destination of the box
                //     destinationRow = boxRow + action.BoxRowDelta;
                //     destinationColumn = boxColumn + action.BoxColumnDelta;
                //
                //     // Check the box can be moved
                //     return CellIsFree(new Position(destinationRow, destinationColumn));
                //
                // case ActionType.Pull:
                //     agentPosition = PositionOfAgent(agent);
                //
                //     // Calculate the destination of the agent
                //     destinationRow = agentPosition.Row + action.AgentRowDelta;
                //     destinationColumn = agentPosition.Column + action.AgentColumnDelta;
                //
                //     // Calculate the possible location of the box to be moved
                //     boxRow = agentPosition.Row - action.BoxRowDelta;
                //     boxColumn = agentPosition.Column - action.BoxColumnDelta;
                //
                //     // Get the box character
                //     box = BoxAt(new Position(boxRow, boxColumn));
                //
                //     // If box is not found return false
                //     if (box == null)
                //     {
                //         return false;
                //     }
                //
                //     // If box and agent colors do not match, then return false
                //     if (agent.Color != box.Color)
                //     {
                //         return false;
                //     }
                //
                //     // Check that the agent can be moved
                //     return CellIsFree(new Position(destinationRow, destinationColumn));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool CellIsFree(Position position)
        {
            return !Level.Walls[position.Row, position.Column]; // && BoxAt(position) == null;
        }

        private Box BoxAt(Position position)
        {
            return PositionsOfBoxes.TryGetValue(position, out var box) ? box : null;
        }

        public bool IsGoalState(HashSet<State> exploredStates)
        {
            if (AgentGoal == null || AgentPosition == AgentGoal.GetInitialLocation())
            {
                if (Constraints.Any() && Constraints.Max(c => c.Time) > Time)
                {
                    exploredStates.Clear();
                    return false;
                }

                return true;
            }

            return false;
        }

        public List<(Position position, Action action)> ExtractPlan()
        {
            var plan = new (Position position, Action action)[Time + 1];
            var state = this;
            while (state.Action != null)
            {
                plan[state.Time] = (state.AgentPosition, state.Action);
                state = state.Parent;
            }

            plan[0] = (state.AgentPosition, state.Action);

            return plan.ToList();
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
            if (obj is not State state)
            {
                return false;
            }

            // Should +1?

            var constraints = Constraints.Where(c => c.Time == Time).ToList();
            var constraints2 = state.Constraints.Where(c => c.Time == state.Time).ToList();

            var isEqual = Agent == state.Agent
                   && AgentPosition == state.AgentPosition
                   && constraints.Count == constraints2.Count
                   && !constraints.Except(constraints2).Any();

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

            result = prime * result + ((AgentPosition.Row + 1) * 21 + (AgentPosition.Column + 1) * 32) * (Agent.Number + 1);

            // foreach (var (boxPosition, box) in PositionsOfBoxes)
            // {
            //     result = prime * result + (((boxPosition.Row + 1) * 41) * Level.Rows + (boxPosition.Column + 1) * 62) * box.Letter;
            // }

            Hash = result;

            return Hash;
        }
    }
}
