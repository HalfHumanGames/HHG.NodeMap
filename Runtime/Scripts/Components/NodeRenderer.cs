using HHG.Common.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HHG.NodeMap.Runtime
{
    [RequireComponent(typeof(Button))]
    public class NodeRenderer : MonoBehaviour
    {
        private Button button;
        private Node node;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Refresh(Node node, bool interactable = false)
        {
            this.node = node;
            button.image.sprite = node.NodeAsset.Asset.Sprite;
            button.interactable = interactable;
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);

            if (button.TryGetComponentInChildren(out TMP_Text label))
            {
                label.text = node.NodeAsset.Asset.name;
            }
        }

        private void OnClick()
        {
            if (node != null && node.NodeAsset.HasAsset)
            {
                node.NodeAsset.Asset.OnClick.Invoke(this);
            }
        }
    }
}