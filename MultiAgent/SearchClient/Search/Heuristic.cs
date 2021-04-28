using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public class Heuristic
    {
        public Heuristic(SAState initialSaState)
        {
        }

        public int CalculateHeuristic(SAState saState)
        {
            var agentDistance = saState.AgentGoal == null
                ? 0
                : Level.DistanceBetweenPositions[(saState.AgentPosition, saState.AgentGoal.GetInitialLocation())];

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

            foreach (var goal in saState.BoxGoals)
            {
                // Goal is complete
                if (saState.BoxAt(goal.GetInitialLocation()) != null)
                {
                    if (saState.BoxAt(goal.GetInitialLocation()).Letter == goal.Letter)
                    {
                        continue;
                    }

                    goalScore += 100;
                }

                
                foreach (var box in saState.PositionsOfBoxes.Where(b => b.Value.Letter == goal.Letter))
                {
                    // Box placed correctly already
                    if (saState.BoxGoals.Exists(g => g.Letter == box.Value.Letter && g.GetInitialLocation().Equals(box.Key)))
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
