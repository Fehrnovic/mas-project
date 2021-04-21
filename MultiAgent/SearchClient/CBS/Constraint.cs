using System;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public interface IConstraint
    {
        public Agent Agent { get; set; }
        public int Time { get; set; }
    }

    public class AgentConstraint : IConstraint, IEquatable<AgentConstraint>
    {
        public Agent Agent { get; set; }
        public int Time { get; set; }

        public Position Position;

        public bool Equals(AgentConstraint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Position.Equals(other.Position) && Equals(Agent, other.Agent) && Time == other.Time;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AgentConstraint) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Agent, Time);
        }
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
