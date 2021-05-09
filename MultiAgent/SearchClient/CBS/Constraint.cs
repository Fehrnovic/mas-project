using System;
using System.Collections.Generic;
using System.Linq;
using MultiAgent.SearchClient.Utils;

namespace MultiAgent.SearchClient.CBS
{
    public interface IConstraint
    {
        public Agent Agent { get; set; }
        public bool Relevant(int time);
        public IEnumerable<Position> Positions { get; }
        public int MaxTime { get; }
        public IConflict Conflict { get; set; }
    }

    public class Constraint : IConstraint, IEquatable<Constraint>
    {
        public Agent Agent { get; set; }
        public Position Position;
        public int Time { get; set; }
        public int MaxTime => Time;
        public IConflict Conflict { get; set; }
        public IEnumerable<Position> Positions => new[] {Position};

        public bool Relevant(int time)
        {
            return time == Time;
        }

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

    public class CorridorConstraint : IConstraint, IEquatable<CorridorConstraint>
    {
        public Agent Agent { get; set; }
        public IEnumerable<Position> CorridorPositions;
        public (int min, int max) Time { get; set; }
        public int MaxTime => Time.max;
        public IConflict Conflict { get; set; }
        public IEnumerable<Position> Positions => CorridorPositions;

        public bool Relevant(int time)
        {
            return time >= Time.min && time <= Time.max;
        }

        public bool Equals(CorridorConstraint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Positions.Count() == other.Positions.Count() && !Positions.Except(other.Positions).Any() &&
                   Equals(Agent, other.Agent) && Time == other.Time;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CorridorConstraint) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Positions, Agent, Time);
        }
    }
}
