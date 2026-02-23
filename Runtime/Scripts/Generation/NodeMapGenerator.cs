using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public static class NodeMapGenerator
    {
        private static readonly Dictionary<Algorithm, IAlgorithm> algorithms = new Dictionary<Algorithm, IAlgorithm>
        {
            { Algorithm.PoissonDisk, new PoissonDiskAlgorithm() },
            { Algorithm.DiamondGrid, new DiamondGridAlgorithm() }
        };

        public static async Task<NodeMap> Generate(NodeMapSettingsAsset settings, int seed = -1)
        {
            settings.Validate(); // Always validate first

            if (!algorithms.TryGetValue(settings.Algorithm, out var algorithm))
            {
                throw new System.ArgumentException($"Unsupported algorithm: {settings.Algorithm}");
            }

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            NodeMap result = null;

            try
            {
                if (seed == -1)
                {
                    // Run multiple tasks asynchronously and in parallel for best performance
                    List<Task<NodeMap>> tasks = new List<Task<NodeMap>>();
                    for (int i = 0; i < System.Environment.ProcessorCount; i++)
                    {
                        seed = System.Environment.TickCount + i;
                        System.Random random = new System.Random(seed);
                        tasks.Add(Task.Run(() => GenerateMapUntilValid(settings, algorithm, token, random, seed), token));
                    }

                    // Wait synchronously for the first task to complete
                    result = await Task.WhenAny(tasks).Result;
                    cancellationTokenSource.Cancel(); // Cancel remaining tasks
                }
                else
                {
                    System.Random random = new System.Random(seed);
                    Random.InitState(seed);
                    result = GenerateMapUntilValid(settings, algorithm, token, random, seed);
                }

                if (result != null)
                {
                    System.Random random = new System.Random(result.Seed);
                    Random.InitState(result.Seed);

                    float randomNoise = settings.RandomNoise;
                    foreach (Node node in result.Nodes)
                    {
                        node.LocalPosition += Random.insideUnitCircle * randomNoise;
                    }

                    AssignNodeAssets(result, settings, random);
                }
            }
            catch (System.AggregateException ex) when (ex.InnerException is System.OperationCanceledException)
            {
                // Expected cancellation that is safe to ignore
            }

            return result;
        }

        // Generator function that runs until a valid node map is found
        private static NodeMap GenerateMapUntilValid(NodeMapSettingsAsset settings, IAlgorithm algorithm, CancellationToken token, System.Random random, int seed)
        {
            int minNodeCount = settings.MinNodeCount;
            int maxNodeCount = settings.MaxNodeCount;

            while (minNodeCount-- > 3)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (token.IsCancellationRequested) return null;

                    NodeMap nodeMap = algorithm.Generate(settings, random);
                    for (int j = 0; j < 10; j++)
                    {
                        if (token.IsCancellationRequested) return null;

                        NodeMap tempMap = Helper(nodeMap, settings, random);
                        if (tempMap.Nodes.Count > minNodeCount && tempMap.Nodes.Count < maxNodeCount)
                        {
                            tempMap.Seed = seed;
                            return tempMap; // Valid map found
                        }
                    }
                }
            }

            return null; // No valid map found
        }

        public static NodeMap Helper(NodeMap nodeMap, NodeMapSettingsAsset settings, System.Random random)
        {
            List<Node> path = new List<Node>();
            HashSet<Node> activePoints = new HashSet<Node>();
            NodeMap tempMap = nodeMap.Clone();

            Node start = tempMap.Start;
            Node end = tempMap.End;
            List<Node> tempNodes = tempMap.Nodes;
            List<Connection> tempConnections = tempMap.Connections;

            int minNodeCount = settings.MinNodeCount;
            int iterations = settings.Iterations;
            int removalsPerIteration = settings.RemovalsPerIteration;

            for (int i = 0; i < iterations; i++)
            {
                AStar.FindPath(start, end, tempConnections, path);

                if (path.Count == 0)
                {
                    break;
                }

                activePoints.AddRange(path);

                for (int j = 0; j < removalsPerIteration; j++)
                {
                    if (path.Count < 3)
                    {
                        break;
                    }

                    int randomIndex = random.Next(1, path.Count - 2);
                    Node randomNode = path[randomIndex];

                    if (randomNode != null)
                    {
                        path.Remove(randomNode);
                        tempNodes.Remove(randomNode);
                        tempConnections.RemoveAll(c => c.Source == randomNode || c.Destination == randomNode);
                    }
                }
            }

            List<Node> nodes = tempMap.Nodes;
            List<Connection> connections = tempMap.Connections;

            nodes.Clear();
            nodes.AddRange(activePoints);
            connections.Clear();
            connections.AddRange(nodeMap.Connections);
            connections.RemoveAll(c => !nodes.Contains(c.Source) || !nodes.Contains(c.Destination));
            connections.Shuffle(random);

            int connectionCount = connections.Count;
            float removalChance = settings.RemovalChance;

            for (int i = 0; i < connectionCount; i++)
            {
                Connection connection = connections[i];
                Node source = connection.Source;
                Node destination = connection.Destination;

                if (source == start || destination == start ||
                    source == end || destination == end)
                {
                    continue;
                }

                if (random.NextDouble() < removalChance)
                {
                    if (CountOutgoingConnections(tempMap, source) > 1 &&
                        CountIncomingConnections(tempMap, destination) > 1)
                    {
                        connections.RemoveAt(i--);
                        connectionCount--;
                    }
                }
            }

            return tempMap;
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

        private static void AssignNodeAssets(NodeMap nodeMap, NodeMapSettingsAsset settings, System.Random random)
        {
            if (settings.NodeAssets.Count == 0)
            {
                return;
            }

            int attempts = 100;
            Dictionary<NodeAsset, int> nodeAssetCounts = new Dictionary<NodeAsset, int>();

            do
            {
                // In case attempting to meet the min node count requirements
                nodeMap.Nodes.ForEach(node => node.NodeAsset = null);

                // Don't use ToDictionary since when add a new item to the list, it copies
                // the last element in the list, which causes a duplicate key exception
                foreach (NodeAsset nodeAsset in settings.NodeAssets)
                {
                    nodeAssetCounts[nodeAsset] = 0;
                }

                nodeMap.Nodes.Shuffle(random);
                
                foreach (Node node in nodeMap.Nodes)
                {
                    NodeAsset nodeAsset = settings.NodeAssets.Where((nodeAsset) => NodeMeetsNodeInfoRequirements(nodeMap, node, nodeAsset, nodeAssetCounts)).SelectByWeight(nodeInfo => nodeInfo.SelectionWeight, random);

                    if (nodeAsset != null)
                    {
                        node.NodeAsset = nodeAsset;
                        nodeAssetCounts[nodeAsset]++;
                    }
                    else if (Application.isPlaying)
                    {
                        throw new System.ArgumentException($"Failed to assign node asset to node: {node}");
                    }
                }

                attempts--;

            // We only need to check the min count requirement since we already
            // check the max count requirment in NodeMeetsNodeInfoRequirements
            } while (attempts > 0 && nodeAssetCounts.Any(n => n.Key.Count.x != -1 && n.Value < n.Key.Count.x));

            if (attempts == 0)
            {
                Debug.LogError("Failed to assign nodes");
            }
        }

        private static bool NodeMeetsNodeInfoRequirements(NodeMap nodeMap, Node node, NodeAsset nodeAsset, Dictionary<NodeAsset, int> nodeCounts)
        {
            if (nodeAsset.Count.y != -1 && nodeCounts[nodeAsset] >= nodeAsset.Count.y)
            {
                return false;
            }

            int distanceFromStart = GetDistanceFromStart(nodeMap, node);
            int distanceFromEnd = GetDistanceFromEnd(nodeMap, node);
            int distanceFromSimilar = GetDistanceFromSimilar(nodeMap, node, nodeAsset); // Pass nodeAsset since node.NodeAsset has not yet been assigned

            return (distanceFromStart == -1 || nodeAsset.DistanceFromStart.y == -1 || distanceFromStart <= nodeAsset.DistanceFromStart.y) &&
                   (distanceFromStart == -1 || nodeAsset.DistanceFromStart.x == -1 || distanceFromStart >= nodeAsset.DistanceFromStart.x) &&
                   (distanceFromEnd == -1 || nodeAsset.DistanceFromEnd.y == -1 || distanceFromEnd <= nodeAsset.DistanceFromEnd.y) &&
                   (distanceFromEnd == -1 || nodeAsset.DistanceFromEnd.x == -1 || distanceFromEnd >= nodeAsset.DistanceFromEnd.x) &&
                   (distanceFromSimilar == -1 || nodeAsset.DistanceFromSimilar.y == -1 || distanceFromSimilar <= nodeAsset.DistanceFromSimilar.y) &&
                   (distanceFromSimilar == -1 || nodeAsset.DistanceFromSimilar.x == -1 || distanceFromSimilar >= nodeAsset.DistanceFromSimilar.x);
        }
    }
}
