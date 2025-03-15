using HHG.Common.Runtime;
using UnityEditor;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class NodeMapRenderer : MonoBehaviour
    {
        [SerializeField] private Transform nodeContainer;
        [SerializeField] private Transform connectionContainer;
        [SerializeField] private NodeRenderer nodePrefab;
        [SerializeField] private LineRenderer connectionPrefab;
        [SerializeField] private NodeMapSettings settings = new NodeMapSettings();
        [SerializeField] private NodeSettings nodeSettings = new NodeSettings();

        private NodeMap map;

        private void OnEnable()
        {
            GenerateMap();
            RenderMap(map);
        }

        [ContextMenu("Generate Map")]
        private void GenerateMap()
        {
            settings.Validate();
            map = NodeMapGenerator.Generate(settings, nodeSettings);
        }

        [ContextMenu("Performance Test")]
        private void PerformanceTest()
        {
            settings.Validate();

            PerformanceUtil.MeasureAverageDuration("Average generation time", () =>
            {
                NodeMapGenerator.Generate(settings, nodeSettings);
            }, 100);
        }

        public void RenderMap(NodeMap map)
        {
            nodeContainer.gameObject.DestroyChildren();
            connectionContainer.gameObject.DestroyChildren();

            RectTransform containerRectTransform = nodeContainer as RectTransform;
            Canvas canvas = containerRectTransform != null ? containerRectTransform.GetComponentInParent<Canvas>(true) : null;

            foreach (Node node in map.Nodes)
            {
                NodeRenderer nodeRenderer = Instantiate(nodePrefab, node.Position, Quaternion.identity, nodeContainer);

                nodeRenderer.Refresh(node.NodeAsset);

                RectTransform nodeRectTransform = nodeRenderer.transform as RectTransform;

                if (containerRectTransform && nodeRectTransform)
                {
                    nodeRectTransform.anchoredPosition = canvas.WorldToAnchoredPoint(containerRectTransform, node.Position);
                }
            }

            foreach (Connection connection in map.Connections)
            {
                LineRenderer lineRenderer = Instantiate(connectionPrefab, connectionContainer);
                lineRenderer.positionCount = 2;
                lineRenderer.SetPositions(new Vector3[] { connection.Source.Position, connection.Destination.Position });
            }
        }

        private void OnValidate()
        {
            GenerateMap();
        }

        private void OnDrawGizmosSelected()
        {
            if (map == null)
            {
                GenerateMap();
            }

            Gizmos.color = Color.red;
            foreach (Node node in map.Nodes)
            {
                Gizmos.DrawWireSphere(node.Position, 0.2f);
                Handles.Label(node.Position + Vector2.right * .25f, node.NodeAsset != null ? node.NodeAsset.name : string.Empty);
            }

            Gizmos.color = Color.green;
            foreach (Connection connection in map.Connections)
            {
                Gizmos.DrawLine(connection.Source.Position, connection.Destination.Position);
            }
        }
    }
}
