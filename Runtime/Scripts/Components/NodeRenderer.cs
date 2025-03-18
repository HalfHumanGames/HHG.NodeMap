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

        public void Refresh(Node node, bool interactable = false)
        {
            button.image.sprite = node.NodeAsset.Asset.Sprite;
            button.interactable = interactable;
        }
    }
}