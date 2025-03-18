using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private NodeMapSettingsAsset nodeMapSettings;

        private NodeMap nodeMap;
        private Dictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();
        private Dictionary<Connection, ConnectionRenderer> connectionRenderers = new Dictionary<Connection, ConnectionRenderer>();
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
            SetCurrentNode(nodeMap.Start);
        }

        private void GenerateMap()
        {
            if (nodeMapSettings != null)
            {
                nodeMap = NodeMapGenerator.Generate(nodeMapSettings);
            }
        }

        private void RenderMap()
        {
            if (nodeMap != null)
            {
                nodeContainer.gameObject.DestroyChildren();
                connectionContainer.gameObject.DestroyChildren();

                RectTransform containerRectTransform = nodeContainer as RectTransform;
                Canvas canvas = containerRectTransform != null ? containerRectTransform.GetComponentInParent<Canvas>(true) : null;

                foreach (Node node in nodeMap.Nodes)
                {
                    NodeRenderer nodeRenderer = Instantiate(nodePrefab, node.Position, Quaternion.identity, nodeContainer);
                    nodeRenderer.Refresh(node);

                    RectTransform nodeRectTransform = nodeRenderer.transform as RectTransform;
                    if (containerRectTransform && nodeRectTransform)
                    {
                        nodeRectTransform.anchoredPosition = canvas.WorldToAnchoredPoint(containerRectTransform, node.Position);
                    }

                    nodeRenderers[node] = nodeRenderer;
                }

                foreach (Connection connection in nodeMap.Connections)
                {
                    ConnectionRenderer connectionRenderer = Instantiate(connectionPrefab, connectionContainer);
                    connectionRenderer.Refresh(connection);

                    connectionRenderers[connection] = connectionRenderer;
                }
            }
        }

        private void SetCurrentNode(Node currentNode)
        {
            foreach (Node node in nodeMap.Nodes)
            {
                nodeRenderers[node].Refresh(node);
            }

            foreach (Connection connection in nodeMap.Connections)
            {
                connectionRenderers[connection].Refresh(connection);
            }

            nodeRenderers[currentNode].Refresh(currentNode, true);

            foreach (Connection connection in nodeMap.Connections.Where(c => c.Source == currentNode))
            {
                Node node = connection.Destination;
                nodeRenderers[node].Refresh(node, true);
                connectionRenderers[connection].Refresh(connection, true);
            }          
        }

        private void OnValidate()
        {
            GenerateMap();
        }

        private void OnDrawGizmosSelected()
        {
            if (nodeMapSettings != null && nodeMapSettings.IsDirty())
            {
                nodeMapSettings.MarkClean();
                nodeMap = null; // Force regenerate
            }

            if (nodeMap == null)
            {
                GenerateMap();
            }

            if (nodeMap != null)
            {
                Gizmos.color = Color.red;
                foreach (Node node in nodeMap.Nodes)
                {
                    Gizmos.DrawWireSphere(node.Position, 0.2f);
                    Handles.Label(node.Position + Vector2.right * .25f, node.NodeAsset != null ? node.NodeAsset.Asset.name : string.Empty);
                }

                Gizmos.color = Color.green;
                foreach (Connection connection in nodeMap.Connections)
                {
                    Gizmos.DrawLine(connection.Source.Position, connection.Destination.Position);
                }
            }
        }

        [ContextMenu("Generate Map Test")] private void GenerateMapTest() => PerformanceUtil.MeasureDuration("Generation time", () => GenerateMap());
        [ContextMenu("Generate 100 Maps Test")] private void Generate100MapsTest() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMap(), 100);
        [ContextMenu("Generate 1000 Maps Test")] private void Generate1000MapsTest() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMap(), 1000);

        private string json = string.Empty;

        [ContextMenu("Save Map Test")] private void SaveMapTest() => json = nodeMap.ToJson();
        [ContextMenu("Load Map Test")] private void LoadMapTest() => nodeMap.FromJsonOverwrite(json);
    }
}
