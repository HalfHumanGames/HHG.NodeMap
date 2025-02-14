using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace HHG.NodeMapSystem.Runtime
{
    public class NodeMapGenerator
    {
        public NodeMap GenerateOrganic(Vector2 startPoint, Vector2 endPoint, Vector2 samplingAreaMin, Vector2 samplingAreaMax, float minDistance, float filterDistance)
        {
            if (Vector2.Distance(startPoint, endPoint) < minDistance)
            {
                throw new System.ArgumentException("startPoint and endPoint cannot be closer than minDistance.");
            }

            if (samplingAreaMin.x >= samplingAreaMax.x || samplingAreaMin.y >= samplingAreaMax.y)
            {
                throw new System.ArgumentException("Invalid sampling area: samplingAreaMin should be less than samplingAreaMax.");
            }

            if (minDistance <= 0)
            {
                throw new System.ArgumentException("minDistance should be greater than 0.");
            }

            if (filterDistance <= 0)
            {
                throw new System.ArgumentException("filterDistance should be greater than 0.");
            }

            if (filterDistance <= minDistance)
            {
                throw new System.ArgumentException("filterDistance should be greater than minDistance.");
            }

            NodeMap nodeMap = new NodeMap();
            List<Vector2> points = PoissonDiskSampling.Sampling(samplingAreaMin, samplingAreaMax, minDistance);

            Vector2 center = (samplingAreaMin + samplingAreaMax) / 2;
            points = points.Where(p => Vector2.Distance(p, center) <= filterDistance && Vector2.Distance(p, startPoint) > minDistance && Vector2.Distance(p, endPoint) > minDistance).ToList();
            points.Add(startPoint);
            points.Add(endPoint);

            Dictionary<Vector2, Node> pointToNode = points.ToDictionary(point => point, point => new Node { Position = point });
            nodeMap.Vertices.AddRange(pointToNode.Values);

            Delaunator.Point[] delaunayPoints = points.ConvertAll(p => new Delaunator.Point(p.x, p.y)).ToArray();

            if (delaunayPoints.Length < 3)
            {
                Debug.LogWarning("Parameters resulted in less than 3 points.");
                return nodeMap;
            }

            Delaunator delaunator = new Delaunator(delaunayPoints);

            List<(Vector2, Vector2)> edges = new List<(Vector2, Vector2)>();
            for (int i = 0; i < delaunator.Triangles.Length; i += 3)
            {
                Vector2 p0 = new Vector2((float)delaunator.Points[delaunator.Triangles[i]].X, (float)delaunator.Points[delaunator.Triangles[i]].Y);
                Vector2 p1 = new Vector2((float)delaunator.Points[delaunator.Triangles[i + 1]].X, (float)delaunator.Points[delaunator.Triangles[i + 1]].Y);
                Vector2 p2 = new Vector2((float)delaunator.Points[delaunator.Triangles[i + 2]].X, (float)delaunator.Points[delaunator.Triangles[i + 2]].Y);

                edges.Add((p0, p1));
                edges.Add((p1, p2));
                edges.Add((p2, p0));
            }

            foreach ((Vector2, Vector2) edge in edges)
            {
                Node fromNode = pointToNode[edge.Item1];
                Node toNode = pointToNode[edge.Item2];
                nodeMap.Connections.Add(new Path { Source = fromNode, Destination = toNode });
            }

            return nodeMap;
        }

        public NodeMap GenerateStructured(Vector2 startPoint, int size, Vector2 spacing, float filterDistanceX, int iterations)
        {
            NodeMap nodeMap = new NodeMap();

            List<Vector2> points = new List<Vector2>();
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    points.Add(new Vector2(x, y) * 1.41f);
                }
            }

            List<Vector2> rotatedPoints = new List<Vector2>();
            foreach (Vector2 point in points)
            {
                float rotatedX = (point.x - point.y) * Mathf.Cos(Mathf.PI / 4f);
                float rotatedY = (point.x + point.y) * Mathf.Sin(Mathf.PI / 4f);

                Vector2 rotatedAndSpaced = new Vector2(rotatedX * spacing.x, rotatedY * spacing.y);

                if (Mathf.Abs(rotatedAndSpaced.x) < filterDistanceX)
                {
                    rotatedPoints.Add(rotatedAndSpaced);
                }
            }

            Vector2 bottommostPoint = rotatedPoints.OrderBy(p => p.y).First();
            Vector2 translation = startPoint - bottommostPoint;
            List<Vector2> translatedPoints = rotatedPoints.Select(p => p + translation).ToList();

            // Create nodes and add them to the node map
            foreach  (Vector2 point in translatedPoints)
            {
                Node node = new Node { Position = point };
                nodeMap.Vertices.Add(node);
            }

            float dist = Mathf.Sqrt(Mathf.Pow(spacing.y, 2f) + Mathf.Pow(spacing.x, 2f));

            // Connect the nodes with edges
            for (int i = 0; i < translatedPoints.Count; i++)
            {
                for (int j = i + 1; j < translatedPoints.Count; j++)
                {
                    if (Vector2.Distance(translatedPoints[i], translatedPoints[j]) <= dist &&
                        !Mathf.Approximately(translatedPoints[i].x, translatedPoints[j].x) &&
                        !Mathf.Approximately(translatedPoints[i].y, translatedPoints[j].y))
                    {
                        nodeMap.Connections.Add(new Path { Source = nodeMap.Vertices[i], Destination = nodeMap.Vertices[j] });
                    }
                }
            }

            // Step 4: Find the path from the start point to the end point with A*
            List<Node> activePoints = new List<Node>();
            NodeMap copy = new NodeMap();
            copy.Vertices.AddRange(nodeMap.Vertices);
            copy.Connections.AddRange(nodeMap.Connections);
            for (int i = 0; i < iterations; i++)
            {
                List<Node> path = AStar(copy, copy.Vertices.First(), copy.Vertices.Last());
                if (path.Count == 0)
                {
                    break;
                }
                activePoints.AddRange(path);

                // Step 5: Exclude random points on the path
                if (path.Count > 2)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        if (path.Count > 2)
                        {
                            int randomIndex = Random.Range(1, path.Count - 2);
                            Node randomNode = path[randomIndex];

                            if (randomNode != null)
                            {
                                path.Remove(randomNode);
                                copy.Vertices.Remove(randomNode);
                                copy.Connections.RemoveAll(c => c.Source == randomNode || c.Destination == randomNode);
                            }
                        }

                    }
                }
            }

            nodeMap.Vertices.Clear();
            nodeMap.Vertices.AddRange(activePoints);
            nodeMap.Connections.RemoveAll(c => !nodeMap.Vertices.Contains(c.Source) || !nodeMap.Vertices.Contains(c.Destination));

            foreach (Node node in nodeMap.Vertices.Skip(1).Take(nodeMap.Vertices.Count - 2))
            {
                var inConnections = GetInConnections(nodeMap, node);
                var outConnections = GetOutConnections(nodeMap, node);

                if (inConnections.Count > 1)
                {
                    if (GetOutConnections(nodeMap, inConnections[0].Source).Count > 1 && Random.Range(0f, 1f) < .3f)
                    {
                        nodeMap.Connections.Remove(inConnections[0]);
                    }
                }

                if (outConnections.Count > 1) 
                {
                    if (GetInConnections(nodeMap, outConnections[0].Destination).Count > 1 && Random.Range(0f, 1f) < .3f)
                    {
                        nodeMap.Connections.Remove(outConnections[0]);
                    }
                }
            }

            return nodeMap;
        }

        private List<Path> GetInConnections(NodeMap nodeMap, Node node)
        {
            return nodeMap.Connections.Where(c => c.Destination == node).Shuffled().ToList();
        }

        private List<Path> GetOutConnections(NodeMap nodeMap, Node node)
        {
            return nodeMap.Connections.Where(c => c.Source == node).Shuffled().ToList();
        }

        private List<Node> AStar(NodeMap nodeMap, Node startNode, Node endNode)
        {
            HashSet<Node> closedSet = new HashSet<Node>();
            HashSet<Node> openSet = new HashSet<Node> { startNode };
            Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
            Dictionary<Node, float> gScore = nodeMap.Vertices.ToDictionary(node => node, node => float.MaxValue);
            gScore[startNode] = 0;
            Dictionary<Node, float> fScore = nodeMap.Vertices.ToDictionary(node => node, node => float.MaxValue);
            fScore[startNode] = Vector2.Distance(startNode.Position, endNode.Position);

            while (openSet.Count > 0)
            {
                Node current = openSet.OrderBy(node => fScore[node]).First();
                if (current == endNode)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (Path connection in nodeMap.Connections.Where(c => c.Source == current || c.Destination == current))
                {
                    Node neighbor = connection.Source == current ? connection.Destination : connection.Source;
                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    float tentativeGScore = gScore[current] + Vector2.Distance(current.Position, neighbor.Position);
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (tentativeGScore >= gScore[neighbor])
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Vector2.Distance(neighbor.Position, endNode.Position);
                }
            }

            return new List<Node>();
        }

        private List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
        {
            List<Node> totalPath = new List<Node> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Add(current);
            }
            totalPath.Reverse();
            return totalPath;
        }
    }
}
