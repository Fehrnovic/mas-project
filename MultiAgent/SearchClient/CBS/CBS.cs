using System;
using System.Collections.Generic;
using MultiAgent.searchClient.CBS;
using MultiAgent.SearchClient.Search;

namespace MultiAgent.SearchClient.CBS
{
    public static class CBS
    {
        public static Dictionary<Agent, List<SAStep>> Run(Dictionary<Agent, SAState> delegation,
            Dictionary<Agent, bool> finishedAgents)
        {
            var OPEN = new Open();
            var exploredNodes = new HashSet<Node>();

            var root = new Node
            {
                Constraints = new HashSet<IConstraint>(),
                Solution = new Dictionary<Agent, List<SAStep>>(),
            };

            foreach (var agent in Level.Agents)
            {
                root.InvokeLowLevelSearch(agent, delegation[agent]);
            }

            OPEN.AddNode(root);
            exploredNodes.Add(root);

            while (!OPEN.IsEmpty)
            {
                var P = OPEN.GetMinNode();

                var conflict = P.GetConflict(finishedAgents);
                if (conflict == null)
                {
                    return P.ExtractMoves();
                }

                // CONFLICT!
                foreach (var conflictedAgent in conflict.ConflictedAgents)
                {
                    var A = new Node {Constraints = new HashSet<IConstraint>(P.Constraints)};

                    var constraint = CreateConstraint(conflictedAgent, conflict, P);
                    if (constraint == null)
                    {
                        continue;
                    }

                    A.Constraints.Add(constraint);
                    A.Solution = P.CloneSolution();

                    SAState state;

                    state = CreateSAState(conflictedAgent,
                        delegation[conflictedAgent], A.Constraints);


                    var foundSolution = A.InvokeLowLevelSearch(conflictedAgent, state);

                    // Agent found a solution
                    if (foundSolution)
                    {
                        OPEN.AddNode(A);
                        exploredNodes.Add(A);
                    }
                }
            }

            return null;
        }

        public static SAState CreateSAState(Agent agent, SAState previousState, HashSet<IConstraint> constraints)
        {
            var state = new SAState(agent, previousState.AgentPosition, previousState.AgentGoal,
                previousState.PositionsOfBoxes, previousState.BoxGoals, constraints);

            state.RelevantBoxToSolveGoal = previousState.RelevantBoxToSolveGoal;
            state.CurrentBoxGoal = previousState.CurrentBoxGoal;
            return state;
        }

        private static IConstraint CreateConstraint(Agent conflictedAgent, IConflict conflict, Node parentNode)
        {
            IConstraint constraint;
            switch (conflict)
            {
                case PositionConflict positionConflict:
                    constraint = new Constraint
                    {
                        Agent = conflictedAgent,
                        Position = positionConflict.Position,
                        Time = positionConflict.Time,
                        Conflict = conflict
                    };

                    break;

                case FollowConflict followConflict:
                    var constraintTime = conflictedAgent == followConflict.Follower
                        ? followConflict.FollowerTime
                        : followConflict.FollowerTime - 1;

                    if (constraintTime <= 0)
                    {
                        return null;
                    }

                    constraint = new Constraint
                    {
                        Agent = conflictedAgent,
                        Position = followConflict.FollowerPosition,
                        Time = constraintTime,
                        Conflict = conflict,
                    };

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conflict));
            }

            return constraint;
        }
    }
}
