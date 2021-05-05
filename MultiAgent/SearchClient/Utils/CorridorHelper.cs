using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient.Utils
{
    public static class CorridorHelper
    {
        public static HashSet<Position> CorridorOfPosition(Position position)
        {
            var corridor = Level.Corridors.FirstOrDefault(c => c.Any(c =>
                c.Row == position.Row &&
                c.Column == position.Column));

            if (corridor == null)
            {
                return null;
            }

            var positions = new HashSet<Position>();
            foreach (var graphNode in corridor)
            {
                positions.Add(new Position(graphNode.Row, graphNode.Column));
            }

            return positions;
        }
    }
}
