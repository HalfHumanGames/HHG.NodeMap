using UnityEngine;
using UnityEngine.UI;

namespace HHG.NodeMap.Runtime
{
    [RequireComponent(typeof(Button))]
    public class NodeRenderer : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Refresh(NodeAsset nodeAsset)
        {
            button.image.sprite = nodeAsset.Sprite;
        }
    }
}