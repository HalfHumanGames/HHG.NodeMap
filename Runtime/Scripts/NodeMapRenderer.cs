using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    public class NodeMapRenderer : MonoBehaviour
    {
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private LineRenderer linePrefab;
        [SerializeField] private NodeMapSettings settings = new NodeMapSettings();

        private NodeMap map;

        private void OnEnable()
        {
            GenerateMap();
            RenderMap(map);
        }

        private void GenerateMap()
        {
            settings.Validate();
            map = NodeMapGenerator.Generate(settings);
        }

        public void RenderMap(NodeMap map)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            foreach (Node node in map.Vertices)
            {
                Instantiate(nodePrefab, node.Position, Quaternion.identity, transform);
            }

            foreach (Path path in map.Paths)
            {
                LineRenderer lineRenderer = Instantiate(linePrefab, transform);
                lineRenderer.positionCount = 2;
                lineRenderer.SetPositions(new Vector3[] { path.Source.Position, path.Destination.Position });
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
            foreach (Node node in map.Vertices)
            {
                Gizmos.DrawSphere(node.Position, 0.2f);
            }

            Gizmos.color = Color.green;
            foreach (Path connection in map.Paths)
            {
                Gizmos.DrawLine(connection.Source.Position, connection.Destination.Position);
            }
        }
    }
}
