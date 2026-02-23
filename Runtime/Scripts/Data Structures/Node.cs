using HHG.Common.Runtime;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [System.Serializable]
    public class Node
    {
        public Vector2 LocalPosition;
        public Vector3 WorldPosition;
        public SerializedAsset<NodeAsset> NodeAsset;
    }
}