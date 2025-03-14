using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class DiamondGridAlgorithm : IAlgorithm
    {
        public NodeMap Generate(NodeMapSettings settings)
        {
            NodeMap nodeMap = new NodeMap();

            List<Vector2> points = new List<Vector2>();
            for (int y = 0; y < settings.Size; y++)
            {
                for (int x = 0; x < settings.Size; x++)
                {
                    points.Add(new Vector2(x, y) * 1.41f);
                }
            }

            List<Vector2> rotatedPoints = new List<Vector2>();
            foreach (Vector2 point in points)
            {
                float rotatedX = (point.x - point.y) * Mathf.Cos(Mathf.PI / 4f);
                float rotatedY = (point.x + point.y) * Mathf.Sin(Mathf.PI / 4f);

                Vector2 rotatedAndSpaced = new Vector2(rotatedX * settings.Spacing.x, rotatedY * settings.Spacing.y);

                if (Mathf.Abs(rotatedAndSpaced.x) < settings.FilterDistance)
                {
                    rotatedPoints.Add(rotatedAndSpaced);
                }
            }

            Vector2 bottommostPoint = rotatedPoints.OrderBy(p => p.y).First();
            Vector2 translation = settings.StartPoint - bottommostPoint;
            List<Vector2> translatedPoints = rotatedPoints.Select(p => p + translation).ToList();

            foreach (Vector2 point in translatedPoints)
            {
                Node node = new Node { Position = point };
                nodeMap.Nodes.Add(node);
            }

            float dist = Mathf.Sqrt(Mathf.Pow(settings.Spacing.y, 2f) + Mathf.Pow(settings.Spacing.x, 2f));

            for (int i = 0; i < translatedPoints.Count; i++)
            {
                for (int j = i + 1; j < translatedPoints.Count; j++)
                {
                    if (Vector2.Distance(translatedPoints[i], translatedPoints[j]) <= dist &&
                        !Mathf.Approximately(translatedPoints[i].x, translatedPoints[j].x) &&
                        !Mathf.Approximately(translatedPoints[i].y, translatedPoints[j].y))
                    {
                        nodeMap.Connections.Add(new Connection { Source = nodeMap.Nodes[i], Destination = nodeMap.Nodes[j] });
                    }
                }
            }

            nodeMap.Start = nodeMap.Nodes.First();
            nodeMap.End = nodeMap.Nodes.Last();

            return nodeMap;
        }
    }
}
