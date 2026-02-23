using HHG.Common.Runtime;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [CreateAssetMenu(fileName = "Node", menuName = "HHG/Node Map System/Node")]
    public class NodeAsset : ScriptableObject
    {
        public Sprite Sprite => sprite;
        public int SelectionWeight => selectionWeight;
        public Vector2Int Count => count;
        public Vector2Int DistanceFromStart => distanceFromStart;
        public Vector2Int DistanceFromEnd => distanceFromEnd;
        public Vector2Int DistanceFromSimilar => distanceFromSimilar;
        public ActionEvent OnClick => onClick;

        [SerializeField] private Sprite sprite;
        [SerializeField] private int selectionWeight;
        [SerializeField, MinMaxSlider(1, 100)] private Vector2Int count;
        [SerializeField, MinMaxSlider(1, 100)] private Vector2Int distanceFromStart;
        [SerializeField, MinMaxSlider(1, 100)] private Vector2Int distanceFromEnd;
        [SerializeField, MinMaxSlider(1, 100)] private Vector2Int distanceFromSimilar;
        [SerializeField] private ActionEvent onClick = new ActionEvent();
    }
}