using UnityEngine;
namespace HHG.NodeMap.Runtime
{
    [RequireComponent(typeof(LineRenderer))]
    public class ConnectionRenderer : MonoBehaviour
    {
        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        public void Refresh(Connection connection, bool interactable = false)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startColor = interactable ? Color.white : Color.black;
            lineRenderer.endColor = interactable ? Color.white : Color.black;
            lineRenderer.SetPositions(new Vector3[] { connection.Source.Position, connection.Destination.Position });

            if (connection.Source.Position.x < connection.Destination.Position.x)
            {
                lineRenderer.textureScale = new Vector2(lineRenderer.textureScale.x, -lineRenderer.textureScale.y);
            }
        }
    }
}