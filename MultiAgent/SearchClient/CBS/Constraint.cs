using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public interface IConstraint
    {
        public Agent Agent { get; set; }
        public int Time { get; set; }
    }

    public class AgentConstraint : IConstraint
    {
        public Agent Agent { get; set; }
        public int Time { get; set; }

        public Position Position;
    }

    // Box constraints? PullConstraint, PushConstraint or BoxConstraint?
    // public class BoxConstraint : IConstraint
    // {
    //     public Agent Agent;
    //     public Box Box;
    //     public int Time;
    // }
    // OR
    // public class PullConstraint : IConstraint
    // {
    //     public Agent Agent;
    //     public Box Box;
    //     public int Time;
    // }
}
