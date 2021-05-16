using System.Collections.Generic;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public class SAStep
    {
        public List<Position> Positions { get; set; }
        public Action Action { get; set; }
        public SAState State { get; set; }
        public int ActionCount => Action != null ? 1 : 0;

        public SAStep(List<Position> positions, Action action)
        {
            Positions = positions;
            Action = action;
        }

        public SAStep(SAStep previousStep)
        {
            Action = Action.NoOp;
            Positions = previousStep.Positions;
            State = previousStep.State;
        }

        public SAStep(SAState state, bool useNoOp = false)
        {
            State = state;
            Action = useNoOp ? Action.NoOp : state.Action;
            Positions = state.GetStatePositions();
        }

        public override string ToString()
        {
            return $"{Action.Name}";
        }
    }
}
