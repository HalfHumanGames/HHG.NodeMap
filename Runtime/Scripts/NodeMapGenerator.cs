using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public static class NodeMapGenerator
    {
        private const int retryCount = 100;

        private static readonly Dictionary<Algorithm, INodeMapGenerator> generators = new Dictionary<Algorithm, INodeMapGenerator>
        {
            { Algorithm.PoissonDisk, new PoissonDiskGenerator() },
            { Algorithm.DiamondGrid, new DiamondGridGenerator() }
        };

        public static NodeMap Generate(NodeMapSettings settings)
        {
            if (generators.TryGetValue(settings.Mode, out var strategy))
            {
                int minNodeCount = settings.MinNodeCount;
                int attempt = retryCount;
                NodeMap nodeMap;
                do
                {
                    nodeMap = Helper(strategy.Generate(settings), settings);

                    if (attempt-- < 0)
                    {
                        minNodeCount--;
                        attempt = retryCount;
                    }

                } while (minNodeCount >= 3 && (nodeMap.Vertices.Count < minNodeCount || nodeMap.Vertices.Count > settings.MaxNodeCount));

                foreach (Node node in nodeMap.Vertices)
                {
                    node.Position += Random.insideUnitCircle * settings.RandomNoise;
                }

                return nodeMap;
            }

            throw new System.ArgumentException($"Unsupported algorithm: {settings.Mode}");
        }

        public static NodeMap Helper(NodeMap nodeMap, NodeMapSettings settings)
        {
            // HashSet do don't add duplicates
            HashSet<Node> activePoints = new HashSet<Node>();
            NodeMap tempMap = nodeMap.Clone();
            for (int i = 0; i < settings.Iterations; i++)
            {
                List<Node> path = AStar.FindPath(tempMap.Start, tempMap.End, tempMap.Paths);
                if (path.Count == 0)
                {
                    break;
                }

                activePoints.AddRange(path);

                if (path.Count > 2)
                {
                    //for (int j = 0; j < 5; j++)
                    {
                        if (path.Count > 2)
                        {
                            int randomIndex = Random.Range(1, path.Count - 2);
                            Node randomNode = path[randomIndex];

                            if (randomNode != null)
                            {
                                path.Remove(randomNode);
                                tempMap.Vertices.Remove(randomNode);
                                tempMap.Paths.RemoveAll(c => c.Source == randomNode || c.Destination == randomNode);
                            }
                        }

                    }
                }
            }

            nodeMap.Vertices.Clear();
            nodeMap.Vertices.AddRange(activePoints);
            nodeMap.Paths.RemoveAll(c => !nodeMap.Vertices.Contains(c.Source) || !nodeMap.Vertices.Contains(c.Destination));
            nodeMap.Paths.Shuffle();

            for (int i = 0; i < nodeMap.Paths.Count; i++)
            {
                Path path = nodeMap.Paths[i];

                if (path.Source == nodeMap.Start || path.Destination == nodeMap.Start ||
                    path.Source == nodeMap.End || path.Destination == nodeMap.End)
                {
                    continue;
                }

                if (Random.Range(0f, 1f) < settings.RemovalChance)
                {
                    if (CountOutgoingPaths(nodeMap, path.Source) > 1 &&
                        CountIncomingPaths(nodeMap, path.Destination) > 1)
                    {
                        nodeMap.Paths.RemoveAt(i--);
                    }
                }
            }

            return nodeMap;
        }

        private static int CountIncomingPaths(NodeMap nodeMap, Node node)
        {
            return nodeMap.Paths.Count(c => c.Destination == node);
        }

        private static int CountOutgoingPaths(NodeMap nodeMap, Node node)
        {
            return nodeMap.Paths.Count(c => c.Source == node);
        }
    }
}
