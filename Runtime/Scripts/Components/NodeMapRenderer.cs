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
        [SerializeField] private ConnectionRenderer connectionPrefab;
        [SerializeField] private NodeMapSettings settings = new NodeMapSettings();
        [SerializeField] private NodeSettings nodeSettings = new NodeSettings();

        private NodeMap map;
        private bool hasStarted;

        private void OnEnable()
        {
            if (hasStarted)
            {
                GenerateAndRenderMap();
            }
        }

        private void Start()
        {
            GenerateAndRenderMap();
            hasStarted = true;
        }

        private void GenerateAndRenderMap()
        {
            GenerateMap();
            RenderMap();
        }

        private void GenerateMap()
        {
            settings.Validate();
            map = NodeMapGenerator.Generate(settings, nodeSettings);
        }

        private void RenderMap()
        {
            nodeContainer.gameObject.DestroyChildren();
            connectionContainer.gameObject.DestroyChildren();

            RectTransform containerRectTransform = nodeContainer as RectTransform;
            Canvas canvas = containerRectTransform != null ? containerRectTransform.GetComponentInParent<Canvas>(true) : null;

            foreach (Node node in map.Nodes)
            {
                NodeRenderer nodeRenderer = Instantiate(nodePrefab, node.Position, Quaternion.identity, nodeContainer);
                nodeRenderer.Refresh(node);

                RectTransform nodeRectTransform = nodeRenderer.transform as RectTransform;
                if (containerRectTransform && nodeRectTransform)
                {
                    nodeRectTransform.anchoredPosition = canvas.WorldToAnchoredPoint(containerRectTransform, node.Position);
                }
            }

            foreach (Connection connection in map.Connections)
            {
                ConnectionRenderer connectionRenderer = Instantiate(connectionPrefab, connectionContainer);
                connectionRenderer.Refresh(connection);
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

        [ContextMenu("Generate Map Test")] private void GenerateMapTest() => PerformanceUtil.MeasureDuration("Generation time", () => GenerateMap());
        [ContextMenu("Generate 100 Maps Test")] private void Generate100MapsTest() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMap(), 100);
        [ContextMenu("Generate 1000 Maps Test")] private void Generate1000MapsTest() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMap(), 1000);
    }
}
