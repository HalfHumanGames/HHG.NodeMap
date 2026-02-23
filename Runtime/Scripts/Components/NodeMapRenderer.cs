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
        [SerializeField] private bool useSeed;
        [SerializeField] private int seed = -1;
        [SerializeField] private Transform transformationSource;
        [SerializeField] private NodeRenderer nodePrefab;
        [SerializeField] private ConnectionRenderer connectionPrefab;
        [SerializeField] private NodeMapSettingsAsset nodeMapSettings;

        private NodeMap nodeMap;
        private Dictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();
        private Dictionary<Connection, ConnectionRenderer> connectionRenderers = new Dictionary<Connection, ConnectionRenderer>();

        private async void OnEnable()
        {
            await GenerateAndRenderMapAsync();
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
                nodeMap = await NodeMapGenerator.Generate(nodeMapSettings, useSeed ? seed : -1);
                seed = nodeMap.Seed;
                ApplyMapTransformations();
            }
        }

        private void ApplyMapTransformations()
        {
            if (transformationSource != null)
            {
                Vector3 center = ComputeCenter(nodeMap.Nodes.Select(n => n.LocalPosition.ToVector3()));

                Matrix4x4 matrix = Matrix4x4.TRS(
                    transformationSource.position,
                    transformationSource.rotation,
                    transformationSource.localScale
                );

                foreach (Node node in nodeMap.Nodes)
                {
                    Vector3 local = node.LocalPosition.ToVector3() - center;
                    node.WorldPosition = matrix.MultiplyPoint3x4(local);
                }
            }
        }

        public static Vector3 ComputeCenter(IEnumerable<Vector3> points)
        {
            int count = 0;
            Vector3 sum = Vector3.zero;

            foreach (Vector3 point in points)
            {
                sum += point;
                count++;
            }

            return sum / count;
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

            if (transformationSource.hasChanged)
            {
                transformationSource.hasChanged = false;
                ApplyMapTransformations();
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

        private string json = string.Empty;

        [ContextMenu("Test/Generate Map")] private void TestGenerateMap() => PerformanceUtil.MeasureDuration("Generation time", () => GenerateMapAsync().Wait());
        [ContextMenu("Test/Generate 100 Maps")] private void TestGenerate100Maps() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMapAsync().Wait(), 100);
        [ContextMenu("Test/Generate 1000 Maps")] private void TestGenerate1000Maps() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMapAsync().Wait(), 1000);
        [ContextMenu("Test/Save Map")] private void TestSaveMap() => json = nodeMap.ToJson();
        [ContextMenu("Test/Load Map")] private void TestLoadMap() => nodeMap.FromJsonOverwrite(json);
    }
}
