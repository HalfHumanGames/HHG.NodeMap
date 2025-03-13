using HHG.Common.Runtime;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public static class AStar
    {
        public static List<Node> FindPath(Node start, Node end, List<Path> connections)
        {
            Dictionary<Node, float> gScore = new() { [start] = 0 };
            Dictionary<Node, Node> cameFrom = new();
            PriorityQueue<Node, float> openSet = new();
            openSet.Enqueue(start, 0);

            while (openSet.Count > 0)
            {
                Node current = openSet.Dequeue();

                if (current == end)
                {
                    List<Node> path = new();
                    while (current != null)
                    {
                        path.Add(current);
                        cameFrom.TryGetValue(current, out current);
                    }
                    path.Reverse();
                    return path;
                }

                foreach (Path connection in connections)
                {
                    //if (connection.Source != current && connection.Destination != current)
                    if (connection.Source != current)
                    {
                        continue;
                    }

                    Node neighbor = connection.Destination;
                    float tentativeGScore = gScore[current] + Vector2.Distance(current.Position, neighbor.Position);

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        float fScore = tentativeGScore + Vector2.Distance(neighbor.Position, end.Position);
                        openSet.Enqueue(neighbor, fScore);
                    }
                }
            }

            return new List<Node>();
        }
    } 
}