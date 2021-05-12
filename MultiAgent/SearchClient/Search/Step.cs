using System.Collections.Generic;
using System.Linq;
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
        public SAState State { get; set; }

        public SAStep(List<Position> positions, Action action)
        {
            Positions = positions;
            Action = action;
        }

        public SAStep(Agent agent, Action action, MAState maState)
        {
            var agentBoxes = LevelDelegationHelper.LevelDelegation.AgentToBoxes[agent];
            var agentBoxGoals = LevelDelegationHelper.LevelDelegation.AgentToBoxGoals[agent];
            var boxPositions = maState.PositionsOfBoxes
                .Where(kvp => agentBoxes.Contains(kvp.Value)).ToList();
            var boxGoals = maState.BoxGoals.Where(bg => agentBoxGoals.Exists(kvp => kvp.box == bg)).ToList();

            Action = action;
            Positions = boxPositions.Select(kvp => kvp.Key).Append(maState.AgentPositions[agent]).ToList();
            State = new SAState(
                agent,
                maState.AgentPositions[agent],
                maState.AgentGoals.FirstOrDefault(ag => ag.Number == agent.Number),
                boxPositions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                boxGoals,
                new HashSet<IConstraint>()
            );
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

    public class MAStep : IStep
    {
        public List<Position> Positions { get; set; }
        public Dictionary<Agent, Action> JointActions { get; set; }
        public MAState State;

        public MAStep(MAState state)
        {
            State = state;
            Positions = state.GetStatePositions();
            JointActions = state.JointActions;
        }
    }
}
