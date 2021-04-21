using System.Collections.Generic;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public interface IConflict
    {
        public List<Agent> ConflictedAgents { get; }
    }

    public class PositionConflict : IConflict
    {
        public Agent Agent1;
        public Agent Agent2;
        public Position Position;
        public int Time;

        public List<Agent> ConflictedAgents => new() {Agent1, Agent2};
    }

    public class FollowConflict : IConflict
    {
        public Agent Leader;
        public Agent Follower;
        public Position FollowerPosition;
        public int FollowerTime;

        public List<Agent> ConflictedAgents => new() {Leader, Follower};
    }

    // Box Conflicts?
}
