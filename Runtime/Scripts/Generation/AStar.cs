using HHG.Common.Runtime;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public static class AStar
    {
        public static bool FindPath(Node start, Node end, List<Connection> connections, List<Node> path)
        {
            path.Clear();

            Dictionary<Node, float> gScore = new() { [start] = 0 };
            Dictionary<Node, Node> cameFrom = new();
            PriorityQueue<Node, float> openSet = new();

            openSet.Enqueue(start, 0);

            while (openSet.Count > 0)
            {
                Node current = openSet.Dequeue();

                if (current == end)
                {
                    while (current != null)
                    {
                        path.Add(current);
                        cameFrom.TryGetValue(current, out current);
                    }
                    path.Reverse();
                    return true;
                }

                foreach (Connection connection in connections)
                {
                    if (connection.Source != current)
                    {
                        continue;
                    }

                    Node neighbor = connection.Destination;
                    float tentativeGScore = gScore[current] + Vector2.Distance(current.LocalPosition, neighbor.LocalPosition);

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        float fScore = tentativeGScore + Vector2.Distance(neighbor.LocalPosition, end.LocalPosition);
                        openSet.Enqueue(neighbor, fScore);
                    }
                }
            }

            return false;
        }
    } 
}