using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiAgent.searchClient
{
    public class State
    {
        // Static properties
        public static List<Agent> AgentGoals;
        public static List<Box> BoxGoals;
        public static bool[,] Walls;

        // State information
        public List<Agent> Agents;
        public List<Box> Boxes;
        public Action[] JointAction;

        public State Parent;
        public int Depth;

        public State(List<Agent> agents, List<Box> boxes)
        {
            Agents = agents;
            Boxes = boxes;
        }

        public State(State parent, Action[] jointAction)
        {
            // Copy parent information
            var agents = parent.Agents.Select(a => new Agent(a.Number, a.Color, a.Position)).ToList();
            var boxes = parent.Boxes.Select(b => new Box(b.Letter, b.Color, b.Position)).ToList();

            Parent = parent;
            JointAction = jointAction;
            Depth = parent.Depth + 1;

            // Apply actions 
            foreach (var agent in Agents)
            {
                var agentAction = jointAction[agent.Number];

                Box box;

                switch (agentAction.Type)
                {
                    case ActionType.NoOp:
                        break;
                    case ActionType.Move:
                        agent.Position.Row += agentAction.AgentRowDelta;
                        agent.Position.Col += agentAction.AgentColDelta;

                        break;
                    case ActionType.Push:
                        agent.Position.Row += agentAction.AgentRowDelta;
                        agent.Position.Col += agentAction.AgentColDelta;

                        // Get the box character
                        box = BoxAt(new Position(agent.Position.Row, agent.Position.Col));

                        box.Position.Row += agentAction.BoxRowDelta;
                        box.Position.Col += agentAction.BoxColDelta;

                        break;
                    case ActionType.Pull:
                        // Find box before pull
                        box = BoxAt(new Position(agent.Position.Row - agentAction.BoxRowDelta,
                            agent.Position.Col - agentAction.BoxColDelta));

                        // Move agent
                        agent.Position.Row += agentAction.AgentRowDelta;
                        agent.Position.Col += agentAction.AgentColDelta;


                        // Update box position
                        box.Position.Row += agentAction.BoxRowDelta;
                        box.Position.Col += agentAction.BoxColDelta;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public List<State> GetExpandedStates()
        {
            // Determine list of applicable actions for each individual agent.
            Dictionary<Agent, List<Action>> applicableActions = new Dictionary<Agent, List<Action>>();
            foreach (var agent in Agents)
            {
                List<Action> applicableAgentActions = new List<Action>();
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
            Action[] jointAction = new Action[Agents.Count];
            int[] actionsPermutation = new int[Agents.Count];
            List<State> expandedStates = new List<State>();
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
                bool done = false;
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

            return expandedStates.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        private bool IsApplicable(Agent agent, Action action)
        {
            int boxRow;
            int boxCol;
            Box box;
            int destinationRow;
            int destinationCol;

            switch (action.Type)
            {
                case ActionType.NoOp:
                    return true;

                case ActionType.Move:
                    destinationRow = agent.Position.Row + action.AgentRowDelta;
                    destinationCol = agent.Position.Col + action.AgentColDelta;

                    return CellIsFree(new Position(destinationRow, destinationCol));

                case ActionType.Push:
                    // Calculate the possible location of the box to be moved
                    boxRow = agent.Position.Row + action.AgentRowDelta;
                    boxCol = agent.Position.Col + action.AgentColDelta;

                    // Get the box character
                    box = BoxAt(new Position(boxRow, boxCol));

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
                    destinationCol = boxCol + action.BoxColDelta;

                    // Check the box can be moved
                    return CellIsFree(new Position(destinationRow, destinationCol));

                case ActionType.Pull:
                    // Calculate the destination of the agent
                    destinationRow = agent.Position.Row + action.AgentRowDelta;
                    destinationCol = agent.Position.Col + action.AgentColDelta;

                    // Calculate the possible location of the box to be moved
                    boxRow = agent.Position.Row - action.BoxRowDelta;
                    boxCol = agent.Position.Col - action.BoxColDelta;

                    // Get the box character
                    box = BoxAt(new Position(boxRow, boxCol));

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
                    return CellIsFree(new Position(destinationRow, destinationCol));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool IsConflicting(Action[] jointAction)
        {
            int[] destinationRows = new int[Agents.Count]; // row of new cell to become occupied by action
            int[] destinationCols = new int[Agents.Count]; // column of new cell to become occupied by action
            int[] boxRows = new int[Agents.Count]; // current row of box moved by action
            int[] boxCols = new int[Agents.Count]; // current column of box moved by action

            foreach (var agent in Agents)
            {
                Action action = jointAction[agent.Number];
                int boxRow;
                int boxCol;

                switch (action.Type)
                {
                    case ActionType.NoOp:
                        break;

                    // Move and pull behave similarly with conflicts, since with a pull, only the agent moves to a square that must be free
                    case ActionType.Move:
                        // Calculate destination of agent
                        destinationRows[agent.Number] = agent.Position.Row + action.AgentRowDelta;
                        destinationCols[agent.Number] = agent.Position.Col + action.AgentColDelta;

                        break;

                    case ActionType.Pull:
                        // Calculate destination of agent
                        destinationRows[agent.Number] = agent.Position.Row + action.AgentRowDelta;
                        destinationCols[agent.Number] = agent.Position.Col + action.AgentColDelta;

                        boxRow = agent.Position.Row - action.BoxRowDelta;
                        boxCol = agent.Position.Col - action.BoxColDelta;

                        boxRows[agent.Number] = boxRow;
                        boxCols[agent.Number] = boxCol;

                        break;

                    case ActionType.Push:
                        // Get current location of box
                        boxRow = agent.Position.Row + action.AgentRowDelta;
                        boxCol = agent.Position.Col + action.AgentColDelta;

                        // Calculate destination of box
                        destinationRows[agent.Number] = boxRow + action.BoxRowDelta;
                        destinationCols[agent.Number] = boxCol + action.BoxColDelta;

                        boxRows[agent.Number] = boxRow;
                        boxCols[agent.Number] = boxCol;
                        break;
                }
            }

            for (int agent1 = 0; agent1 < Agents.Count; ++agent1)
            {
                if (jointAction[agent1].Type == ActionType.NoOp)
                {
                    continue;
                }

                for (int agent2 = agent1 + 1; agent2 < Agents.Count; ++agent2)
                {
                    if (jointAction[agent2].Type == ActionType.NoOp)
                    {
                        continue;
                    }

                    // Agents or boxes moving into same cell?
                    if (destinationRows[agent1] == destinationRows[agent2] &&
                        destinationCols[agent1] == destinationCols[agent2])
                    {
                        return true;
                    }

                    // Agents moving the same box?
                    if (boxRows[agent1] == boxRows[agent2] && boxCols[agent1] == boxCols[agent2])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CellIsFree(Position position)
        {
            return !State.Walls[position.Row, position.Col] && BoxAt(position) == null && AgentAt(position) == null;
        }

        private Agent AgentAt(Position position)
        {
            return Agents.FirstOrDefault(a => a.Position == position);
        }

        private Box BoxAt(Position position)
        {
            return Boxes.FirstOrDefault(b => b.Position == position);
        }
    }
}
