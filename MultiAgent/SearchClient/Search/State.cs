using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiAgent.SearchClient.Utils;
using MultiAgent.SearchClient.CBS;

namespace MultiAgent.SearchClient.Search
{
    public class State
    {
        public Agent Agent;
        public Position AgentPosition;
        public Agent AgentGoal;

        public List<Box> Boxes;
        public List<Box> BoxGoals;

        // State information
        public readonly Dictionary<Position, Box> PositionsOfBoxes;

        public Action Action;

        public State Parent;
        public int Time;
        public HashSet<Constraint> Constraints;

        private int Hash = 0;

        public State(Agent agent, Agent agentGoal, List<Box> boxes, List<Box> boxGoals,
            HashSet<Constraint> constraints)
        {
            Agent = agent;
            AgentPosition = agent.GetInitialLocation();
            AgentGoal = agentGoal;

            Boxes = boxes;
            BoxGoals = boxGoals;

            PositionsOfBoxes = new Dictionary<Position, Box>(Boxes.Count);
            foreach (var box in Boxes)
            {
                PositionsOfBoxes.Add(box.GetInitialLocation(), box);
            }

            Constraints = constraints.Where(c => c.Agent == Agent).ToHashSet();
        }

        public State(State parent, Action action)
        {
            Parent = parent;
            Action = action;
            Time = parent.Time + 1;
            Constraints = parent.Constraints;

            Agent = parent.Agent;
            AgentPosition = parent.AgentPosition;
            AgentGoal = parent.AgentGoal;

            Boxes = parent.Boxes;
            BoxGoals = parent.BoxGoals;
            PositionsOfBoxes = new Dictionary<Position, Box>(parent.Boxes.Count);
            foreach (var (boxPosition, currentBox) in parent.PositionsOfBoxes)
            {
                PositionsOfBoxes.Add(boxPosition, currentBox);
            }

            Box box = null;

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

                case ActionType.Push:
                    // Move agent:
                    AgentPosition = new Position(
                        parent.AgentPosition.Row + action.AgentRowDelta,
                        parent.AgentPosition.Column + action.AgentColumnDelta
                    );

                    // Get the box character from parent
                    Parent.PositionsOfBoxes.TryGetValue(AgentPosition, out box);

                    // Set the new location:
                    PositionsOfBoxes.Remove(AgentPosition);
                    PositionsOfBoxes.Add(
                        new Position(
                            AgentPosition.Row + action.BoxRowDelta,
                            AgentPosition.Column + action.BoxColumnDelta
                        ),
                        box
                    );

                    break;
                case ActionType.Pull:
                    // Find box before pull
                    var oldBoxPosition = new Position(
                        AgentPosition.Row - action.BoxRowDelta,
                        AgentPosition.Column - action.BoxColumnDelta
                    );

                    // Get the box from parent
                    Parent.PositionsOfBoxes.TryGetValue(oldBoxPosition, out box);

                    // Move agent
                    AgentPosition = new Position(
                        parent.AgentPosition.Row + action.AgentRowDelta,
                        parent.AgentPosition.Column + action.AgentColumnDelta
                    );

                    // Update box position
                    // Remove old location:
                    PositionsOfBoxes.Remove(oldBoxPosition);
                    // Set new location:
                    PositionsOfBoxes.Add(
                        new Position(
                            oldBoxPosition.Row + action.BoxRowDelta,
                            oldBoxPosition.Column + action.BoxColumnDelta
                        ),
                        box
                    );

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public List<State> GetExpandedStates()
        {
            // Determine list of applicable actions for the agent
            List<State> reachableStates = new(16);

            foreach (var action in Action.AllActions)
            {
                if (IsApplicable(action))
                {
                    var state = new State(this, action);

                    if (state.ConstraintsSatisfied())
                    {
                        reachableStates.Add(state);
                    }
                }
            }

            return reachableStates;
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

        private bool IsApplicable(Action action)
        {
            int destinationRow;
            int destinationColumn;
            Box box;
            int boxRow;
            int boxColumn;


            switch (action.Type)
            {
                case ActionType.NoOp:
                    return true;

                case ActionType.Move:
                    destinationRow = AgentPosition.Row + action.AgentRowDelta;
                    destinationColumn = AgentPosition.Column + action.AgentColumnDelta;

                    return CellIsFree(new Position(destinationRow, destinationColumn));

                case ActionType.Push:
                    // Calculate the possible location of the box to be moved
                    boxRow = AgentPosition.Row + action.AgentRowDelta;
                    boxColumn = AgentPosition.Column + action.AgentColumnDelta;

                    // Get the box character
                    box = BoxAt(new Position(boxRow, boxColumn));

                    // If box is not found return false
                    // If box and agent colors do not match, then return false
                    if (box == null || Agent.Color != box.Color)
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
                    destinationRow = AgentPosition.Row + action.AgentRowDelta;
                    destinationColumn = AgentPosition.Column + action.AgentColumnDelta;

                    // Calculate the possible location of the box to be moved
                    boxRow = AgentPosition.Row - action.BoxRowDelta;
                    boxColumn = AgentPosition.Column - action.BoxColumnDelta;

                    // Get the box character
                    box = BoxAt(new Position(boxRow, boxColumn));

                    // If box is not found return false
                    // If box and agent colors do not match, then return false
                    if (box == null || Agent.Color != box.Color)
                    {
                        return false;
                    }

                    // Check that the agent can be moved
                    return CellIsFree(new Position(destinationRow, destinationColumn));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool CellIsFree(Position position)
        {
            return !Level.Walls[position.Row, position.Column] && BoxAt(position) == null;
        }

        public Box BoxAt(Position position)
        {
            return PositionsOfBoxes.TryGetValue(position, out var box) ? box : null;
        }

        public bool IsGoalState(HashSet<State> exploredStates)
        {
            var boxesCompleted = true;
            var agentCompleted = false;

            foreach (var boxGoal in BoxGoals)
            {
                if (!PositionsOfBoxes.TryGetValue(boxGoal.GetInitialLocation(), out var box))
                {
                    boxesCompleted = false;
                }

                if (box != null && boxGoal.Letter != box.Letter)
                {
                    boxesCompleted = false;
                }
            }

            // If agent is placed correctly AND all box goal satisfied
            if ((AgentGoal == null || AgentPosition == AgentGoal.GetInitialLocation()) && boxesCompleted)
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

        public List<Step> ExtractPlan()
        {
            var plan = new Step[Time + 1];
            var state = this;
            while (state.Action != null)
            {
                plan[state.Time] = new Step(state);
                state = state.Parent;
            }

            plan[0] = new Step(state);

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
