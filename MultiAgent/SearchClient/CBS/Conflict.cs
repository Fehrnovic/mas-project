using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public interface IConflict
    {
    }

    public class Conflict : IConflict
    {
        public Agent AgentI;
        public Agent AgentJ;
        public Position Position;
        public int Time;
    }

    // Two agents moving to each other previous positions
    public class EdgeConflict : IConflict
    {
        public Agent AgentI;
        public Agent AgentJ;
        public Position Position1;
        public Position Position2;
        public int Time;
    }

    // How to handle TrainConflict? (Two agents moving right after each other...)
    // public class TrainConflict : IConflict
    // {
    //     public Agent AgentI;
    //     public Agent AgentJ;
    //     ...
    // }

    // Box Conflicts?
}
