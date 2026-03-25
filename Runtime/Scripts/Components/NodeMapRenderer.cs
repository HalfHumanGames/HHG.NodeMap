using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class NodeMapRenderer : MonoBehaviour
    {
        [EditorButton(nameof(GenerateAndRenderMapAsync), "Generate Map", PositionType = ButtonPositionType.Above)]
        [SerializeField] private bool alwaysDrawGizmos;
        [SerializeField] private bool drawSettingGizmos;
        [SerializeField] private bool useSeed;
        [SerializeField] private int seed = -1;
        [SerializeField] private bool applyTransformation;
        [SerializeField] private Transform mapContainer;
        [SerializeField] private Transform transformationSource;
        [SerializeField] private NodeRenderer nodePrefab;
        [SerializeField] private ConnectionRenderer connectionPrefab;
        [SerializeField, InLineEditor] private NodeMapSettingsAsset nodeMapSettings;

        private NodeMap nodeMap;
        private Dictionary<Node, NodeRenderer> nodeRenderers = new Dictionary<Node, NodeRenderer>();
        private Dictionary<Connection, ConnectionRenderer> connectionRenderers = new Dictionary<Connection, ConnectionRenderer>();
        private Vector3 transformationCenter;
        private Matrix4x4 transformationMatrix;
        private bool invalidSettings = false;
        private RectTransform mapContainerRect;
        private Canvas canvas;

        private void Awake()
        {
            mapContainerRect = transform as RectTransform;
            canvas = mapContainerRect != null ? mapContainerRect.GetComponentInParent<Canvas>(true) : null;
        }

        private void UpdateConnectionRendererPoints()
        {
            foreach (var kv in connectionRenderers)
            {
                Connection connection = kv.Key;
                ConnectionRenderer connectionRenderer = kv.Value;
                NodeRenderer source = nodeRenderers[connection.Source];
                NodeRenderer destination = nodeRenderers[connection.Destination];
                Vector3 sourcePosition = NodeToWorldPosition(source.transform);
                Vector3 destinationPosition = NodeToWorldPosition(destination.transform);
                connectionRenderer.UpdatePositions(sourcePosition, destinationPosition);
            }
        }

        private Vector3 NodeToWorldPosition(Transform nodeTransform)
        {
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Vector3 screenPos = nodeTransform.position;
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(Camera.main.transform.position.z)));
                worldPos.z = nodeTransform.position.z;
                return worldPos;
            }
            return nodeTransform.position;
        }

        private async void OnEnable()
        {
            await GenerateAndRenderMapAsync();
        }

        private void LateUpdate()
        {
            if (mapContainer.transform.hasChanged)
            {
                mapContainer.transform.hasChanged = false;
                UpdateConnectionRendererPoints();
            }
        }

        private async Task GenerateAndRenderMapAsync()
        {
            await GenerateMapAsync();

            if (Application.isPlaying && nodeMap != null)
            {
                RenderMap();
                SetCurrentNode(nodeMap.Start);
            }
        }

        private async Task GenerateMapAsync()
        {
            if (nodeMapSettings != null)
            {
                nodeMap = await NodeMapGenerator.Generate(nodeMapSettings, useSeed ? seed : -1);

                if (nodeMap != null)
                {
                    seed = nodeMap.Seed;
                    ApplyMapTransformation();

#if UNITY_EDITOR
                    SceneView.RepaintAll();
#endif
                }
            }
        }

        private void ApplyMapTransformation()
        {
            transformationCenter = ComputeCenter(nodeMap.Nodes.Select(n => n.LocalPosition.ToVector3()));

            bool apply = applyTransformation && transformationSource != null;

            transformationMatrix = Matrix4x4.TRS(
                apply ? transformationSource.position : Vector3.zero,
                apply ? transformationSource.rotation : Quaternion.identity,
                apply ? transformationSource.localScale : Vector3.one
            );

            foreach (Node node in nodeMap.Nodes)
            {
                Vector3 local = node.LocalPosition.ToVector3() - transformationCenter;
                node.WorldPosition = transformationMatrix.MultiplyPoint3x4(local);
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

        private void RenderMap()
        {
            if (nodeMap != null)
            {
                mapContainer.gameObject.DestroyChildren();
                nodeRenderers.Clear();
                connectionRenderers.Clear();

                foreach (Node node in nodeMap.Nodes)
                {
                    NodeRenderer nodeRenderer = Instantiate(nodePrefab, node.WorldPosition, nodePrefab.transform.rotation, mapContainer);
                    nodeRenderer.Refresh(node);
                    nodeRenderers[node] = nodeRenderer;

                    RectTransform nodeRectTransform = nodeRenderer.transform as RectTransform;
                    if (mapContainerRect && nodeRectTransform)
                    {
                        nodeRectTransform.anchoredPosition = canvas.WorldToAnchoredPoint(mapContainerRect, node.WorldPosition);
                    }
                }

                foreach (Connection connection in nodeMap.Connections)
                {
                    ConnectionRenderer connectionRenderer = Instantiate(connectionPrefab, Vector3.zero, connectionPrefab.transform.rotation, mapContainer);
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

#if UNITY_EDITOR

        private async void OnValidate()
        {
            if (CanValidate())
            {
                await GenerateMapAsync();
            }
        }

        public static bool CanValidate()
        {
            return !Application.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !BuildPipeline.isBuildingPlayer;
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
                invalidSettings = false;
                nodeMapSettings.MarkClean();
                nodeMap = null; // Force regenerate
            }

            if (nodeMap == null && !invalidSettings)
            {
                await GenerateMapAsync();

                if (nodeMap == null)
                {
                    invalidSettings = true;
                }
            }

            if (nodeMap != null)
            {
                if (transformationSource.hasChanged)
                {
                    transformationSource.hasChanged = false;
                    ApplyMapTransformation();
                }

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

                if (drawSettingGizmos)
                {
                    Gizmos.color = Color.yellow;
                    Matrix4x4 matrix = Gizmos.matrix;
                    Gizmos.matrix = transformationMatrix;
                    float scaleFactor = Mathf.Max(transformationMatrix.lossyScale.x, transformationMatrix.lossyScale.y, transformationMatrix.lossyScale.z);
                    Gizmos.DrawWireSphere(nodeMapSettings.StartPoint, .2f / scaleFactor);
                    Gizmos.DrawWireSphere(nodeMapSettings.EndPoint, .2f / scaleFactor);
                    Gizmos.DrawWireCube(Vector3.zero, nodeMapSettings.SamplingArea);
                    Gizmos.matrix = matrix;
                }
            }
        }

        private string json = string.Empty;

        [ContextMenu("Test/Generate Map")] private void TestGenerateMap() => PerformanceUtil.MeasureDuration("Generation time", () => GenerateMapAsync().Wait());
        [ContextMenu("Test/Generate 100 Maps")] private void TestGenerate100Maps() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMapAsync().Wait(), 100);
        [ContextMenu("Test/Generate 1000 Maps")] private void TestGenerate1000Maps() => PerformanceUtil.MeasureAverageDuration("Average generation time", () => GenerateMapAsync().Wait(), 1000);
        [ContextMenu("Test/Save Map")] private void TestSaveMap() => json = nodeMap.ToJson();
        [ContextMenu("Test/Load Map")] private void TestLoadMap() => nodeMap.FromJsonOverwrite(json);

#endif
    }
}
