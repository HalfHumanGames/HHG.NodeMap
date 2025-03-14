using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class PoissonDiskAlgorithm : IAlgorithm
    {
        public NodeMap Generate(NodeMapSettings settings)
        {
            NodeMap nodeMap = new NodeMap();
            List<Vector2> points = PoissonDiskSampling.Sampling(settings.SamplingAreaMin, settings.SamplingAreaMax, settings.MinDistance);

            Vector2 center = (settings.SamplingAreaMin + settings.SamplingAreaMax) / 2;
            points = points.Where(p => Vector2.Distance(p, center) <= settings.FilterDistance && Vector2.Distance(p, settings.StartPoint) > settings.MinDistance && Vector2.Distance(p, settings.EndPoint) > settings.MinDistance).ToList();
            points.Add(settings.StartPoint);
            points.Add(settings.EndPoint);

            Dictionary<Vector2, Node> pointToNode = points.ToDictionary(point => point, point => new Node { Position = point });
            nodeMap.Start = pointToNode[settings.StartPoint];
            nodeMap.End = pointToNode[settings.EndPoint];
            nodeMap.Nodes.AddRange(pointToNode.Values);

            Delaunator.Point[] delaunayPoints = points.ConvertAll(p => new Delaunator.Point(p.x, p.y)).ToArray();

            if (delaunayPoints.Length < 3)
            {
                Debug.LogError("Parameters resulted in less than 3 points.");
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
                if (edge.Item1.y >= edge.Item2.y)
                {
                    continue;
                }

                Vector2 direction = edge.Item2 - edge.Item1;
                float angle = Vector2.Angle(Vector2.right, direction);

                if (angle < settings.AngleFilter || 
                    180f - angle < settings.AngleFilter || 
                    Vector2.Distance(edge.Item1, edge.Item2) > settings.MaxDistance)
                {
                    continue;
                }

                Node fromNode = pointToNode[edge.Item1];
                Node toNode = pointToNode[edge.Item2];
                nodeMap.Connections.Add(new Connection { Source = fromNode, Destination = toNode });
            }

            return nodeMap;
        }
    }
}
