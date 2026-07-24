using HHG.Common.Runtime;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [RequireComponent(typeof(UILineRenderer))]
    public class ConnectionRenderer : MonoBehaviour
    {
        private UILineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<UILineRenderer>();
        }

        public void Refresh(Connection connection, bool interactable = false)
        {
            //lineRenderer.color = interactable ? Color.white : Color.black;
        }

        public void UpdatePositions(Vector2 sourcePosition, Vector2 destinationPosition)
        {
            lineRenderer.SetPositions(new Vector2[] { sourcePosition, destinationPosition });
        }
    }
}