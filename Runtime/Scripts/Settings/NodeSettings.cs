using System.Collections.Generic;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [System.Serializable]
    public class NodeSettings
    {
        public IReadOnlyList<NodeInfo> NodeInfos => nodeInfos;

        [SerializeField] private List<NodeInfo> nodeInfos = new List<NodeInfo>();
    }

    [System.Serializable]
    public class NodeInfo
    {
        public NodeAsset NodeAsset => nodeAsset;
        public int SelectionWeight => selectionWeight;
        public int MinCount => minCount;
        public int MaxCount => maxCount;
        public int MinDistanceFromStart => minDistanceFromStart;
        public int MaxDistanceFromStart => maxDistanceFromStart;
        public int MinDistanceFromEnd => minDistanceFromEnd;
        public int MaxDistanceFromEnd => maxDistanceFromEnd;
        public int MinDistanceFromSimilar => minDistanceFromSimilar;
        public int MaxDistanceFromSimilar => maxDistanceFromSimilar;

        [SerializeField] private NodeAsset nodeAsset;
        [SerializeField] private int selectionWeight;
        [SerializeField] private int minCount;
        [SerializeField] private int maxCount;
        [SerializeField] private int minDistanceFromStart;
        [SerializeField] private int maxDistanceFromStart = int.MaxValue;
        [SerializeField] private int minDistanceFromEnd;
        [SerializeField] private int maxDistanceFromEnd = int.MaxValue;
        [SerializeField] private int minDistanceFromSimilar;
        [SerializeField] private int maxDistanceFromSimilar = int.MaxValue;
    }
}