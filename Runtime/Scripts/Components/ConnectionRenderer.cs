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
            lineRenderer.startColor = interactable ? Color.white : Color.black;
            lineRenderer.endColor = interactable ? Color.white : Color.black;
            UpdatePositions(connection.Source.WorldPosition, connection.Destination.WorldPosition);
        }

        public void UpdatePositions(Vector3 sourcePosition, Vector3 destinationPosition)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { sourcePosition, destinationPosition });
        }
    }
}