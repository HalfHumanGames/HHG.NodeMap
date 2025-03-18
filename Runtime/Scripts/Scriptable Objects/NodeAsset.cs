using HHG.Common.Runtime;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [CreateAssetMenu(fileName = "Node", menuName = "HHG/Node Map System/Node")]
    public class NodeAsset : ScriptableObject
    {
        public Sprite Sprite => sprite;
        public ActionEvent OnClick => onClick;

        [SerializeField] private Sprite sprite;
        [SerializeField] private ActionEvent onClick;
    }
}