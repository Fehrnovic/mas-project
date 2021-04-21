using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public class Heuristic
    {
        public Heuristic(State initialState)
        {
        }

        public int CalculateHeuristic(State state)
        {
            var agentDistance = state.AgentGoal == null
                ? 0
                : Level.DistanceBetweenPositions[(state.AgentPosition, state.AgentGoal.GetInitialLocation())];

            // var goalCount = state.BoxGoals.Count(boxGoal =>
            // {
            //     if (state.PositionsOfBoxes.TryGetValue(boxGoal.GetInitialLocation(), out var box))
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
    }
}
