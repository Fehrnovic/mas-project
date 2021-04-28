using System.Collections.Generic;
using MultiAgent.SearchClient;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Search;
using MultiAgent.SearchClient.Utils;
using Action = System.Action;

namespace MultiAgent.searchClient.Search
{
    public interface IState
    {
        public bool IsGoalState(HashSet<IState> exploredStates);
        public IEnumerable<IState> GetExpandedStates();
        public IEnumerable<IStep> ExtractPlan();

    }
}
