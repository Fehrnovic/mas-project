using System;
using System.Collections.Generic;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public class Constraint : IEquatable<Constraint>
    {
        public Agent Agent { get; set; }
        public Position Position;
        public int Time { get; set; }
        public IConflict Conflict { get; set; }

        public bool Equals(Constraint other)
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
            return Equals((Constraint) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Agent, Time);
        }
    }
}
