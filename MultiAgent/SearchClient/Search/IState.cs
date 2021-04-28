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
        public List<IState> GetExpandedStates();

        public bool ConstraintsSatisfied();

        public List<Position> GetStatePositions();

        public List<Constraint> GetRelevantConstraints();

        public bool IsApplicable(Action action);

        public bool CellIsFree(Position position);

        public Box BoxAt(Position position);

        public bool IsGoalState(HashSet<IState> exploredStates);

        public List<Step> ExtractPlan();

    }
}
