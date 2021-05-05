using System.Collections.Generic;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public interface IConflict
    {
        public List<IAgent> ConflictedAgents { get; }
    }

    public class PositionConflict : IConflict
    {
        public IAgent Agent1;
        public IAgent Agent2;
        public Position Position;
        public int Time;

        public List<IAgent> ConflictedAgents => new() {Agent1, Agent2};
    }

    public class FollowConflict : IConflict
    {
        public IAgent Leader;
        public IAgent Follower;
        public Position FollowerPosition;
        public int FollowerTime;

        public List<IAgent> ConflictedAgents => new() {Leader, Follower};
    }

    // Box Conflicts?
}
