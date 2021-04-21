using System.Collections.Generic;
using System.Linq;

namespace MultiAgent.SearchClient.Utils
{
    public class Graph
    {
        public readonly GraphNode[,] NodeGrid;
        public HashSet<GraphNode> Nodes = new();

        public Graph()
        {
            NodeGrid = new GraphNode[Level.Rows, Level.Columns];

            for (var row = 1; row < Level.Rows; row++)
            {
                for (var column = 1; column < Level.Columns; column++)
                {
                    if (Level.Walls[row, column])
                    {
                        continue;
                    }

                    GraphNode graphNode = new GraphNode(row, column);
                    
                    GraphNode neighborGraphNode;
                    if (row > 1)
                    {
                        neighborGraphNode = NodeGrid[row - 1, column];
                        if (neighborGraphNode != null)
                        {
                            graphNode.AddOutgoingNode(neighborGraphNode);
                            neighborGraphNode.AddOutgoingNode(graphNode);
                        }
                    }
                    
                    if (column > 1)
                    {
                        neighborGraphNode = NodeGrid[row, column - 1];
                        if (neighborGraphNode != null)
                        {
                            graphNode.AddOutgoingNode(neighborGraphNode);
                            neighborGraphNode.AddOutgoingNode(graphNode);
                        }
                    }

                    NodeGrid[row, column] = graphNode;
                    Nodes.Add(graphNode);
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
    }
}
