using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public static class Heuristic
    {
        public static int CalculateHeuristicSA(SAState state)
        {
            var agentDistance = state.AgentGoal == null
                ? 0
                : Level.DistanceBetweenPositions[(state.AgentPosition, state.AgentGoal.GetInitialLocation())];

            // var goalCount = saState.BoxGoals.Count(boxGoal =>
            // {
            //     if (saState.PositionsOfBoxes.TryGetValue(boxGoal.GetInitialLocation(), out var box))
            //     {
            //         return box.Letter != boxGoal.Letter;
            //     }
            //
            //     return true;
            // });

            var goalScore = 0;

            foreach (var goal in state.BoxGoals)
            {
                // Goal is complete
                if (state.BoxAt(goal.GetInitialLocation()) != null)
                {
                    if (state.BoxAt(goal.GetInitialLocation()).Letter == goal.Letter)
                    {
                        continue;
                    }

                    goalScore += 100;
                }

                foreach (var box in state.PositionsOfBoxes.Where(b => b.Value.Letter == goal.Letter))
                {
                    // Box placed correctly already
                    if (state.BoxGoals.Exists(g => g.Letter == box.Value.Letter && g.GetInitialLocation().Equals(box.Key)))
                    {
                        // Disregard box
                        continue;
                    }

                    goalScore += Level.DistanceBetweenPositions[(goal.GetInitialLocation(), box.Key)];
                }
            }

            return agentDistance + goalScore;
        }

        public static int CalculateHeuristicMA(MAState state)
        {
            // Calculate goal count heuristic
            int goalCount = HGoalCount(state);

            // Calculate manhattan distance / actual distance
            int distance = HDistance(state);

            // Calculate manhattan distance / actual distance
            int actionPenalty = HActionPenalty(state);

            // Weigh goal count heuristic higher
            return (100000 * goalCount) + distance + actionPenalty;
        }

        private static int HGoalCount(MAState state)
        {
            var boxGoalScore = 0;

            foreach (var boxGoal in state.BoxGoals)
            {
                if (state.BoxAt(boxGoal.GetInitialLocation()) != null)
                {
                    // Box goal is complete
                    if (state.BoxAt(boxGoal.GetInitialLocation()).Letter == boxGoal.Letter)
                    {
                        // Box is placed correctly
                        continue;
                    }

                    // Wrong box is placed on the goal
                    boxGoalScore += 100;
                }
                else
                {
                    // Goal is not complete
                    boxGoalScore += 1;
                }
            }

            var agentGoalScore = 0;
            //foreach (var agentGoal in state.AgentGoals)
            //{
            //    var agentAtGoal = state.PositionsOfAgents[agentGoal.GetInitialLocation()];
            //    if (agentAtGoal != null)
            //    {
            //        // Agent goal is complete
            //        if (agentAtGoal.Number == agentGoal.Number)
            //        {
            //            // Agent is placed correctly
            //            continue;
            //        }

            //        // Wrong agent is placed on the goal
            //        agentGoalScore += 2;
            //    }
            //    else
            //    {
            //        // Agent goal is not complete
            //        agentGoalScore += 1;
            //    }
            //}

            return boxGoalScore + agentGoalScore;

            //// Calculate distance between all boxes
            //foreach (var boxKeyValuePair in state.PositionsOfBoxes.Where(b => b.Value.Letter == goal.Letter))
            //{
            //    // Box placed correctly already
            //    if (state.BoxGoals.Exists(g => g.Letter == boxKeyValuePair.Value.Letter && g.GetInitialLocation().Equals(boxKeyValuePair.Key)))
            //    {
            //        // Disregard box
            //        continue;
            //    }

            //    goalScore += Level.DistanceBetweenPositions[(goal.GetInitialLocation(), boxKeyValuePair.Key)];
            //}
        }

        private static int HDistance(MAState state)
        {
            // Get distance of closest box to boxGoal, and closest agent to the box
            var distance = 0;
            foreach (var boxGoal in state.BoxGoals)
            {
                // Find closest box to boxGoal
                var (boxPosition, box) = state.PositionsOfBoxes
                    .Where(kvp => kvp.Value.Letter == boxGoal.Letter)
                    .OrderBy(kvp => Level.DistanceBetweenPositions[(kvp.Key, boxGoal.GetInitialLocation())])
                    .First();

                var boxToGoalDistance = Level.DistanceBetweenPositions[(boxPosition, boxGoal.GetInitialLocation())];

                // Find position of agent closest to box
                var closestAgentPosition = state.PositionsOfAgents
                    .Where(kvp => kvp.Value.Color == box.Color)
                    .OrderBy(kvp => Level.DistanceBetweenPositions[(kvp.Key, boxPosition)])
                    .First().Key;

                var agentToBoxDistance = Level.DistanceBetweenPositions[(closestAgentPosition, boxPosition)];

                distance += boxToGoalDistance + agentToBoxDistance;
            }

            var agentToGoalDistance = 0;

            // If all boxGoals for an agent are completed, then we calculate the distance to his goal
            foreach (var agent in state.Agents)
            {
                // Find all the agent's boxes
                var boxGoals = state.BoxGoals.Where(bg => bg.Color == agent.Color).ToList();

                var agentGoalsSatisfied = true;

                if (boxGoals.Count > 0)
                {
                    foreach (var boxGoal in boxGoals)
                    {
                        state.PositionsOfBoxes.TryGetValue(boxGoal.GetInitialLocation(), out var box);
                        if (box == null || box.Letter != boxGoal.Letter)
                        {
                            agentGoalsSatisfied = false;
                            break;
                        }
                    }
                }

                if (agentGoalsSatisfied)
                {
                    var agentGoal = state.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number);
                    if (agentGoal != null)
                    {
                        agentToGoalDistance +=
                            Level.DistanceBetweenPositions[
                                (state.AgentPositions[agent], agentGoal.GetInitialLocation())];
                    }
                }
            }

            return distance + agentToGoalDistance;
        }

        private static int HActionPenalty(MAState state)
        {
            if (state.JointActions == null)
            {
                return 0;
            }

            var actionPenalty = 0;
            foreach (var action in state.JointActions.Values)
            {
                if (action.Type == ActionType.NoOp)
                {
                    actionPenalty += 0;
                }
                else
                {
                    actionPenalty += 1;
                }
            }

            return actionPenalty;
        }
    }
}