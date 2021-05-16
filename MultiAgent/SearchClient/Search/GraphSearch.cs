using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.CBS;

namespace MultiAgent.SearchClient.Search
{
    public class GraphSearch
    {
        public static IEnumerable<SAStep> Search(SAState initialState, IFrontier frontier)
        {
            if (initialState is SAState saState)
            {
                if (saState.Constraints.Any(c =>
                {
                    if (c is Constraint constraint)
                    {
                        var constrainedPosition = constraint.Position;
                        var agentToPositionTime =
                            Level.GetDistanceBetweenPosition(saState.AgentPosition, constrainedPosition);

                        if (agentToPositionTime > constraint.Time + 1)
                        {
                            return true;
                        }
                    }

                    return false;
                }))
                {
                    return null;
                }
            }

            var iterations = 0;

            frontier.Add(initialState);
            var exploredStates = new HashSet<SAState>();

            while (true)
            {
                if (frontier.IsEmpty())
                {
                    return null;
                }

                var state = frontier.Pop();
                exploredStates.Add(state);

                if (state.IsGoalState(exploredStates))
                {
                    return state.ExtractPlan();
                }


                var reachableStates = state.GetExpandedStates();

                foreach (var reachableState in reachableStates)
                {
                    if (!frontier.Contains(reachableState) && !exploredStates.Contains(reachableState))
                    {
                        frontier.Add(reachableState);
                    }
                }
            }
        }
    }
}
