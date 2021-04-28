using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiAgent.SearchClient.CBS;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.Search
{
    public interface IStep
    {
        public List<Position> Positions { get; set; }
    }
    public class SAStep : IStep
    {
        public List<Position> Positions { get; set; }
        public Action Action { get; set; }

        public SAStep(List<Position> positions, Action action)
        {
            Positions = positions;
            Action = action;
        }

        public SAStep(SAState state)
        {
            Action = state.Action;

            Positions = state.GetStatePositions();
        }
    }

    public class MAStep : IStep
    {
        public List<Position> Positions { get; set; }
        public Dictionary<Agent, Action> JointActions { get; set; }

        public MAStep(List<Position> positions, Dictionary<Agent, Action> jointActions)
        {
            Positions = positions;
            JointActions = jointActions;
        }

        public MAStep(MAState state)
        {
            Positions = state.GetStatePositions();

            JointActions = state.JointActions;
        }
    }
}
