using HHG.Common.Runtime;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [System.Serializable]
    public class Node
    {
        public Vector2 Position;
        public SerializedAsset<NodeAsset> NodeAsset;
    }
}