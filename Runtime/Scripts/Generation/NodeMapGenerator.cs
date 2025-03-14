using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public static class NodeMapGenerator
    {
        private const int retryCount = 100;

        private static readonly Dictionary<Algorithm, IAlgorithm> algorithms = new Dictionary<Algorithm, IAlgorithm>
        {
            { Algorithm.PoissonDisk, new PoissonDiskAlgorithm() },
            { Algorithm.DiamondGrid, new DiamondGridAlgorithm() }
        };

        public static NodeMap Generate(NodeMapSettings settings, NodeSettings nodeSettings)
        {
            if (algorithms.TryGetValue(settings.Algorithm, out var algorithm))
            {
                int minNodeCount = settings.MinNodeCount;
                int retries = retryCount;
                NodeMap nodeMap;
                do
                {
                    nodeMap = Helper(algorithm.Generate(settings), settings);

                    if (retries-- < 0)
                    {
                        minNodeCount--;
                        retries = retryCount;
                    }

                } while (minNodeCount >= 3 && (nodeMap.Nodes.Count < minNodeCount || nodeMap.Nodes.Count > settings.MaxNodeCount));

                if (minNodeCount < 3)
                {
                    throw new System.ArgumentException($"Settings failed to generate a valid node map: {settings.Algorithm}");
                }

                foreach (Node node in nodeMap.Nodes)
                {
                    node.Position += Random.insideUnitCircle * settings.RandomNoise;
                }

                AssignNodeAssets(nodeMap, nodeSettings);

                return nodeMap;
            }

            throw new System.ArgumentException($"Unsupported algorithm: {settings.Algorithm}");
        }

        public static NodeMap Helper(NodeMap nodeMap, NodeMapSettings settings)
        {
            List<Node> path = new List<Node>();
            HashSet<Node> activePoints = new HashSet<Node>();
            NodeMap tempMap = nodeMap.Clone();
            for (int i = 0; i < settings.Iterations; i++)
            {

                AStar.FindPath(tempMap.Start, tempMap.End, tempMap.Connections, path);

                if (path.Count == 0)
                {
                    break;
                }

                activePoints.AddRange(path);

                for (int j = 0; j < settings.Removals; j++)
                {
                    if (path.Count <= 2)
                    {
                        break;
                    }

                    int randomIndex = Random.Range(1, path.Count - 2);
                    Node randomNode = path[randomIndex];

                    if (randomNode != null)
                    {
                        path.Remove(randomNode);
                        tempMap.Nodes.Remove(randomNode);
                        tempMap.Connections.RemoveAll(c => c.Source == randomNode || c.Destination == randomNode);
                    }
                }
            }

            nodeMap.Nodes.Clear();
            nodeMap.Nodes.AddRange(activePoints);
            nodeMap.Connections.RemoveAll(c => !nodeMap.Nodes.Contains(c.Source) || !nodeMap.Nodes.Contains(c.Destination));
            nodeMap.Connections.Shuffle();

            for (int i = 0; i < nodeMap.Connections.Count; i++)
            {
                Connection connection = nodeMap.Connections[i];

                if (connection.Source == nodeMap.Start || connection.Destination == nodeMap.Start ||
                    connection.Source == nodeMap.End || connection.Destination == nodeMap.End)
                {
                    continue;
                }

                if (Random.value < settings.RemovalChance)
                {
                    if (CountOutgoingConnections(nodeMap, connection.Source) > 1 &&
                        CountIncomingConnections(nodeMap, connection.Destination) > 1)
                    {
                        nodeMap.Connections.RemoveAt(i--);
                    }
                }
            }

            return nodeMap;
        }

        private static int CountIncomingConnections(NodeMap nodeMap, Node node)
        {
            return nodeMap.Connections.Count(c => c.Destination == node);
        }

        private static int CountOutgoingConnections(NodeMap nodeMap, Node node)
        {
            return nodeMap.Connections.Count(c => c.Source == node);
        }

        public static int GetDistanceFromStart(NodeMap nodeMap, Node node)
        {
            return GetDistance(nodeMap, nodeMap.Start, node);
        }

        public static int GetDistanceFromEnd(NodeMap nodeMap, Node node)
        {
            return GetDistance(nodeMap, nodeMap.End, node);
        }

        // Pass NodeAsset as a parameter since node.NodeAsset has not yet been assigned
        public static int GetDistanceFromSimilar(NodeMap nodeMap, Node node, NodeAsset nodeAsset)
        {
            int minDistance = -1;

            foreach (Node other in nodeMap.Nodes)
            {
                if (other != node && other.NodeAsset == nodeAsset)
                {
                    int distance = GetDistance(nodeMap, node, other);

                    if (minDistance == -1 || distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }

            return minDistance;
        }

        private static int GetDistance(NodeMap nodeMap, Node start, Node end)
        {
            if (start == end)
            {
                return 0;
            }

            Dictionary<Node, int> distances = new Dictionary<Node, int>();
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(start);
            distances[start] = 0;

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                int currentDistance = distances[current];

                foreach (Connection connection in nodeMap.Connections)
                {
                    Node neighbor = null;
                    if (connection.Source == current)
                    {
                        neighbor = connection.Destination;
                    }
                    else if (connection.Destination == current)
                    {
                        neighbor = connection.Source;
                    }

                    if (neighbor != null && !distances.ContainsKey(neighbor))
                    {
                        distances[neighbor] = currentDistance + 1;
                        queue.Enqueue(neighbor);

                        if (neighbor == end)
                        {
                            return distances[neighbor];
                        }
                    }
                }
            }

            return -1;
        }

        // TODO: This does not yet check the NodeInfo.MinCount property, so need to retry if don't meet those requirements
        private static void AssignNodeAssets(NodeMap nodeMap, NodeSettings nodeSettings)
        {
            if (nodeSettings.NodeInfos.Count == 0)
            {
                return;
            }

            int attempts = 100;
            Dictionary<NodeInfo, int> nodeInfoCounts = new Dictionary<NodeInfo, int>();

            do
            {
                // In case attempting to meet the min node count requirements
                nodeMap.Nodes.ForEach(node => node.NodeAsset = null);

                // Don't use ToDictionary since when add a new item to the list, it copies
                // the last element in the list, which causes a duplicate key exception

                foreach (NodeInfo nodeInfo in nodeSettings.NodeInfos)
                {
                    nodeInfoCounts[nodeInfo] = 0;
                }

                foreach (Node node in nodeMap.Nodes)
                {
                    NodeInfo nodeInfo = nodeSettings.NodeInfos.Where((nodeInfo) => NodeMeetsNodeInfoRequirements(nodeMap, node, nodeInfo, nodeInfoCounts)).SelectByWeight(nodeInfo => nodeInfo.SelectionWeight);

                    if (nodeInfo != null)
                    {
                        node.NodeAsset = nodeInfo.NodeAsset;
                        nodeInfoCounts[nodeInfo]++;
                    }
                    else if (Application.isPlaying)
                    {
                        throw new System.ArgumentException($"Failed to assign node asset to node: {node}");
                    }
                }

                attempts--;

            // We only need to check the min count requirement since we already
            // check the max count requirment in NodeMeetsNodeInfoRequirements
            } while (attempts > 0 && nodeInfoCounts.Any(n => n.Key.MinCount != -1 && n.Value < n.Key.MinCount));
        }

        private static bool NodeMeetsNodeInfoRequirements(NodeMap nodeMap, Node node, NodeInfo nodeInfo, Dictionary<NodeInfo, int> nodeCounts)
        {
            if (nodeInfo.MaxCount != -1 && nodeCounts[nodeInfo] >= nodeInfo.MaxCount)
            {
                return false;
            }

            int distanceFromStart = GetDistanceFromStart(nodeMap, node);
            int distanceFromEnd = GetDistanceFromEnd(nodeMap, node);
            int distanceFromSimilar = GetDistanceFromSimilar(nodeMap, node, nodeInfo.NodeAsset);

            return (distanceFromStart == -1 || nodeInfo.MaxDistanceFromStart == -1 || distanceFromStart <= nodeInfo.MaxDistanceFromStart) &&
                   (distanceFromStart == -1 || nodeInfo.MinDistanceFromStart == -1 || distanceFromStart >= nodeInfo.MinDistanceFromStart) &&
                   (distanceFromEnd == -1 || nodeInfo.MaxDistanceFromEnd == -1 || distanceFromEnd <= nodeInfo.MaxDistanceFromEnd) &&
                   (distanceFromEnd == -1 || nodeInfo.MinDistanceFromEnd == -1 || distanceFromEnd >= nodeInfo.MinDistanceFromEnd) &&
                   (distanceFromSimilar == -1 || nodeInfo.MaxDistanceFromSimilar == -1 || distanceFromSimilar <= nodeInfo.MaxDistanceFromSimilar) &&
                   (distanceFromSimilar == -1 || nodeInfo.MinDistanceFromSimilar == -1 || distanceFromSimilar >= nodeInfo.MinDistanceFromSimilar);
        }
    }
}
