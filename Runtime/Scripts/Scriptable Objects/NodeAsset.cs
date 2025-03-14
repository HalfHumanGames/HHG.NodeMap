using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [CreateAssetMenu(fileName = "Node", menuName = "HHG/Node Map System/Node")]
    public class NodeAsset : ScriptableObject
    {
        public Sprite Sprite => sprite;

        [SerializeField] private Sprite sprite;
    }
}