using System.Collections.Generic;
using UnityEngine;

namespace HHG.NodeMapSystem.Runtime
{
    // TODO: Add min node count param and regen if nodes <= min node count
    public class NodeMapRenderer : MonoBehaviour
    {
        public GameObject nodePrefab;
        public LineRenderer linePrefab;

        [Header("Structured")]
        public Vector2 startPoint2 = new Vector2(0, -10f);
        public int size = 8;
        public int iterations = 8;
        public Vector2 spacing = new Vector2(1, 1);
        public float filterDistanceX;

        [Header("Organic")]
        public Vector2 startPoint = new Vector2(0, -10f);
        public Vector2 endPoint = new Vector2(0, 10f);
        public Vector2 samplingAreaMin = new Vector2(-10f, -10f);
        public Vector2 samplingAreaMax = new Vector2(10f, 10f);
        public float minDistance = 1f;
        public float filterDistance = 10f;

        private NodeMapGenerator generator = new NodeMapGenerator();
        private NodeMap map;

        void Start()
        {
            GenerateAndRender();
        }

        private void OnEnable()
        {
            GenerateAndRender();
        }

        private void GenerateAndRender()
        {
            GenerateMap();
            Render(map);
        }

        private void GenerateMap()
        {
            //map = generator.GenerateOrganic(startPoint, endPoint, samplingAreaMin, samplingAreaMax, minDistance, filterDistance);
            map = generator.GenerateStructured(startPoint2, size, spacing, filterDistanceX, iterations);
        }

        public void Render(NodeMap map)
        {
            // Clear existing nodes and connections
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            Dictionary<Node, GameObject> nodeObjects = new();

            foreach (Node node in map.Vertices)
            {
                GameObject nodeObj = Instantiate(nodePrefab, node.Position, Quaternion.identity, transform);
                nodeObjects[node] = nodeObj;
            }

            foreach (Path connection in map.Connections)
            {
                LineRenderer line = Instantiate(linePrefab, transform);
                line.positionCount = 2;
                line.SetPositions(new Vector3[] { connection.Source.Position, connection.Destination.Position });
            }
        }

        private void OnValidate()
        {
            if (Vector2.Distance(startPoint, endPoint) < minDistance)
            {
                endPoint = startPoint + Vector2.up * minDistance;
            }

            samplingAreaMax.x = Mathf.Max(samplingAreaMax.x, samplingAreaMin.x + 1f);
            samplingAreaMax.y = Mathf.Max(samplingAreaMax.y, samplingAreaMin.y + 1f);
            minDistance = Mathf.Max(minDistance, .1f);
            filterDistance = Mathf.Max(filterDistance, minDistance);

            GenerateMap();
        }

        void OnDrawGizmos()
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
            foreach (Path connection in map.Connections)
            {
                Gizmos.DrawLine(connection.Source.Position, connection.Destination.Position);
            }
        }
    }
}
