using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient.Utils
{
    public class Graph
    {
        public readonly GraphNode[,] NodeGrid;

        public Graph()
        {
            NodeGrid = new GraphNode[Level.Rows, Level.Columns];

            for (var row = 0; row < Level.Rows; row++)
            {
                for (var column = 0; column < Level.Columns; column++)
                {
                    if (Level.OutsideWorld[row, column] || Level.Walls[row, column])
                    {
                        continue;
                    }

                    var graphNode = new GraphNode(row, column);

                    GraphNode neighborGraphNode;
                    if (row > 0)
                    {
                        neighborGraphNode = NodeGrid[row - 1, column];
                        if (neighborGraphNode != null)
                        {
                            graphNode.AddOutgoingNode(neighborGraphNode);
                            neighborGraphNode.AddOutgoingNode(graphNode);
                        }
                    }

                    if (column > 0)
                    {
                        neighborGraphNode = NodeGrid[row, column - 1];
                        if (neighborGraphNode != null)
                        {
                            graphNode.AddOutgoingNode(neighborGraphNode);
                            neighborGraphNode.AddOutgoingNode(graphNode);
                        }
                    }

                    NodeGrid[row, column] = graphNode;
                }
            }
        }

        public int BFS(GraphNode startNode, GraphNode finishNode)
        {
            Queue<GraphNode> queue = new();
            List<GraphNode> visitedNodes = new();

            Dictionary<GraphNode, int> nodeDepths = new();

            queue.Enqueue(startNode);
            visitedNodes.Add(startNode);
            nodeDepths.Add(startNode, 0);

            while (queue.Any())
            {
                GraphNode currentNode = queue.Dequeue();

                if (finishNode.Row == currentNode.Row && finishNode.Column == currentNode.Column)
                {
                    return nodeDepths[currentNode];
                }

                currentNode.OutgoingNodes.ForEach(neighbor =>
                {
                    if (!visitedNodes.Contains(neighbor))
                    {
                        visitedNodes.Add(neighbor);

                        int depth = nodeDepths[currentNode];

                        nodeDepths.Add(neighbor, depth + 1);

                        queue.Enqueue(neighbor);
                    }
                });
            }

            return int.MaxValue;
        }
    }

    public class GraphNode
    {
        public readonly int Row, Column;
        public readonly List<GraphNode> OutgoingNodes = new();

        public GraphNode(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public void AddOutgoingNode(GraphNode graphNode)
        {
            OutgoingNodes.Add(graphNode);
        }

        public bool Equals(GraphNode other)
        {
            return Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            return obj is GraphNode graphNode && Equals(graphNode);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public override string ToString()
        {
            return $"({Row},{Column})";
        }
    }
}
