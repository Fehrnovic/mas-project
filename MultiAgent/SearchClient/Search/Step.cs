using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public class Step
    {
        public List<Position> Positions { get; set; }
        public Action Action { get; set; }

        public Step(List<Position> positions, Action action)
        {
            Positions = positions;
            Action = action;
        }

        public Step(State state)
        {
            Action = state.Action;

            Positions = state.GetStatePositions();
        }
    }
}
