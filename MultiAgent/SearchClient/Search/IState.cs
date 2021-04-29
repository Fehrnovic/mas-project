using System.Collections.Generic;

namespace MultiAgent.SearchClient.Search
{
    public interface IState
    {
        public bool IsGoalState(HashSet<IState> exploredStates);
        public IEnumerable<IState> GetExpandedStates();
        public IEnumerable<IStep> ExtractPlan();
    }
}
