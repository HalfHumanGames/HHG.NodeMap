using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class DiamondGridAlgorithm : IAlgorithm
    {
        public NodeMap Generate(NodeMapSettingsAsset settings)
        {
            NodeMap nodeMap = new NodeMap();

            int size = settings.Size;
            float cos45 = Mathf.Cos(Mathf.PI / 4f);
            float sin45 = Mathf.Sin(Mathf.PI / 4f);
            float spacingX = settings.Spacing.x;
            float spacingY = settings.Spacing.y;
            float filterDistance = settings.FilterDistance;

            List<Vector2> points = new List<Vector2>();
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    points.Add(new Vector2(x, y));
                }
            }

            List<Vector2> rotatedPoints = new List<Vector2>();
            foreach (Vector2 point in points)
            {
                float rotatedX = (point.x - point.y) * cos45;
                float rotatedY = (point.x + point.y) * sin45;

                Vector2 rotatedAndSpaced = new Vector2(rotatedX * spacingX, rotatedY * spacingY);

                if (Mathf.Abs(rotatedAndSpaced.x) < filterDistance)
                {
                    rotatedPoints.Add(rotatedAndSpaced);
                }
            }

            Vector2 bottommostPoint = rotatedPoints.OrderBy(p => p.y).First();
            Vector2 translation = settings.StartPoint - bottommostPoint;
            List<Vector2> translatedPoints = rotatedPoints.Select(p => p + translation).ToList();

            foreach (Vector2 point in translatedPoints)
            {
                Node node = new Node { LocalPosition = point };
                nodeMap.Nodes.Add(node);
            }

            float dist = Mathf.Sqrt(Mathf.Pow(spacingY, 2f) + Mathf.Pow(spacingX, 2f));

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
