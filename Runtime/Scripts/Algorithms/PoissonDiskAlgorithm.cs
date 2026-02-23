using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class PoissonDiskAlgorithm : IAlgorithm
    {
        public NodeMap Generate(NodeMapSettingsAsset settings, System.Random random)
        {
            NodeMap nodeMap = new NodeMap();

            Vector2 startPoint = settings.StartPoint;
            Vector2 endPoint = settings.EndPoint;
            Vector2 samplingAreaMin = settings.SamplingAreaMin;
            Vector2 samplingAreaMax = settings.SamplingAreaMax;

            float minDistance = settings.MinDistance;
            float maxDistance = settings.MaxDistance;
            float filterDistance = settings.FilterDistance;
            float angleFilter = settings.AngleFilter;

            List<Vector2> points = PoissonDiskSampling.Sampling(samplingAreaMin, samplingAreaMax, minDistance, random);
            Vector2 center = (samplingAreaMin + samplingAreaMax) / 2;

            points.RemoveAll(p =>
                Vector2.Distance(p, center) > filterDistance ||
                Vector2.Distance(p, startPoint) <= minDistance ||
                Vector2.Distance(p, endPoint) <= minDistance);
           
            points.Add(startPoint);
            points.Add(endPoint);

            Dictionary<Vector2, Node> pointToNode = points.ToDictionary(point => point, point => new Node { LocalPosition = point });
            nodeMap.Start = pointToNode[startPoint];
            nodeMap.End = pointToNode[endPoint];
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

                if (angle < angleFilter || 180f - angle < angleFilter || Vector2.Distance(edge.Item1, edge.Item2) > maxDistance)
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
