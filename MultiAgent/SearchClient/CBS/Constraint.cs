using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public interface IConstraint
    {
    }

    public class Constraint : IConstraint
    {
        public Agent Agent;
        public Position Position;
        public int Time;
    }

    public class EdgeConstraint : IConstraint
    {
        public Agent Agent;
        public Position Position1;
        public Position Position2;
        public int Time;
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
