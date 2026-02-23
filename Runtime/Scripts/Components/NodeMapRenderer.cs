using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class NodeMapRenderer : MonoBehaviour
    {
        [SerializeField] private bool alwaysDrawGizmos;
        [SerializeField] private Transform transformSource;
        [SerializeField] private NodeRenderer nodePrefab;
        [SerializeField] private ConnectionRenderer connectionPrefab;
        [SerializeField] private NodeMapSettingsAsset nodeMapSettings;

        private NodeMap nodeMap;
        private Dictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();
        private Dictionary<Connection, ConnectionRenderer> connectionRenderers = new Dictionary<Connection, ConnectionRenderer>();
        private Vector3 sourcePosition;
        private Quaternion sourceRotation;
        private Vector3 sourceScale;
        private bool hasStarted;

        private async void OnEnable()
        {
            if (hasStarted)
            {
                await GenerateAndRenderMapAsync();
            }
        }

        private async void Start()
        {
            await GenerateAndRenderMapAsync();
            hasStarted = true;
        }

        private async Task GenerateAndRenderMapAsync()
        {
            await GenerateMapAsync();
            RenderMap();
            SetCurrentNode(nodeMap.Start);
        }

        [ContextMenu("Generate Map")]
        private async Task GenerateMapAsync()
        {
            if (nodeMapSettings != null)
            {
                nodeMap = await NodeMapGenerator.Generate(nodeMapSettings);
                RealignMap();
                
            }
        }

        private void RealignMap()
        {
            if (transformSource != null)
            {
                Vector3 center = ComputeCenter(nodeMap.Nodes.Select(n => n.LocalPosition.ToVector3()).ToArray());

                Matrix4x4 matrix = Matrix4x4.TRS(
                    transformSource.position,
                    transformSource.rotation,
                    transformSource.localScale
                );

                foreach (Node node in nodeMap.Nodes)
                {
                    Vector3 local = node.LocalPosition.ToVector3() - center;
                    node.WorldPosition = matrix.MultiplyPoint3x4(local);
                }
            }
        }

        public static Vector3 ComputeCenter(Vector3[] points)
        {
            Vector3 sum = Vector3.zero;
            foreach (var p in points)
                sum += p;

            return sum / points.Length;
        }


        [ContextMenu("Render Map")]
        private void RenderMap()
        {
            if (nodeMap != null)
            {
                gameObject.DestroyChildren();

                foreach (Node node in nodeMap.Nodes)
                {
                    NodeRenderer nodeRenderer = Instantiate(nodePrefab, node.WorldPosition, nodePrefab.transform.rotation, transform);
                    nodeRenderer.Refresh(node);
                    nodeRenderers[node] = nodeRenderer;
                }

                foreach (Connection connection in nodeMap.Connections)
                {
                    ConnectionRenderer connectionRenderer = Instantiate(connectionPrefab, Vector3.zero, connectionPrefab.transform.rotation, transform);
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

        private async void OnValidate()
        {
            await GenerateMapAsync();
        }

        private async void OnDrawGizmos()
        {
            if (alwaysDrawGizmos)
            {
                await DrawGizmos();
            }
        }

        private async void OnDrawGizmosSelected()
        {
            await DrawGizmos();
        }

        private async Task DrawGizmos()
        {
            if (nodeMapSettings != null && nodeMapSettings.IsDirty())
            {
                nodeMapSettings.MarkClean();
                nodeMap = null; // Force regenerate
            }

            if (nodeMap == null)
            {
                await GenerateMapAsync();
            }

            if (transformSource.position != sourcePosition ||
                transformSource.rotation != sourceRotation||
                transformSource.lossyScale != sourceScale)
            {
                sourcePosition = transformSource.position;
                sourceRotation = transformSource.rotation;
                sourceScale = transformSource.lossyScale;
                RealignMap();
            }

            if (nodeMap != null)
            {
                Gizmos.color = Color.red;
                foreach (Node node in nodeMap.Nodes)
                {
                    Gizmos.DrawWireSphere(node.WorldPosition, 0.2f);
                    Handles.Label(node.WorldPosition + Vector3.right * .25f, node.NodeAsset != null ? node.NodeAsset.Asset.name : string.Empty);
                }

                Gizmos.color = Color.green;
                foreach (Connection connection in nodeMap.Connections)
                {
                    Gizmos.DrawLine(connection.Source.WorldPosition, connection.Destination.WorldPosition);
                }
            }
        }

        [ContextMenu("Generate Map Test")] private void GenerateMapTest() => PerformanceUtil.MeasureDuration("Generation time", () => GenerateMapAsync().Wait());
        [ContextMenu("Generate 100 Maps Test")] private void Generate100MapsTest() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMapAsync().Wait(), 100);
        [ContextMenu("Generate 1000 Maps Test")] private void Generate1000MapsTest() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMapAsync().Wait(), 1000);

        private string json = string.Empty;

        [ContextMenu("Save Map Test")] private void SaveMapTest() => json = nodeMap.ToJson();
        [ContextMenu("Load Map Test")] private void LoadMapTest() => nodeMap.FromJsonOverwrite(json);
    }
}
