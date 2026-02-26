using System.Collections.Generic;
using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [CreateAssetMenu(fileName = "Node Map Settings", menuName = "HHG/Node Map System/Node Map Settings")]
    public class NodeMapSettingsAsset : ScriptableObject
    {
        public Algorithm Algorithm => algorithm;
        public Vector2 StartPoint => startPoint;
        public Vector2 EndPoint => endPoint;
        public Vector2 SamplingArea => samplingArea;
        public Vector2Int NodeCount => nodeCount;
        public Vector2 Distance => distance;
        public Vector2 Spacing => spacing;
        public int Iterations => iterations;
        public int RemovalsPerIteration => removalsPerIteration;
        public float RemovalChance => removalChance;
        public float AngleFilter => angleFilter;
        public float DistanceFilter => distanceFilter;
        public float RandomNoise => randomNoise;

        public int Size => size;

        public IReadOnlyList<NodeAsset> NodeAssets => nodeAssets;

        [SerializeField] private Algorithm algorithm;
        [SerializeField] private Vector2 startPoint = new Vector2(0, -10f);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private Vector2 endPoint = new Vector2(0, 10f);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private Vector2 samplingArea = new Vector2(20, 20);
        [SerializeField, MinMaxSlider(3, 100)] private Vector2Int nodeCount = new Vector2Int(10, 100);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk), MinMaxSlider(.1f, 100f)] private Vector2 distance = new Vector2(1f, 1f);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.DiamondGrid)] private Vector2 spacing = new Vector2(1, 1);
        [SerializeField, Range(0, 100)] private int iterations = 8;
        [SerializeField, Range(0, 100)] private int removalsPerIteration = 1;
        [SerializeField, Range(0f, 1f)] private float removalChance = .3f;
        [SerializeField, Range(1f, 100f)] private float distanceFilter = 10f;
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk), Range(5f, 85f)] private float angleFilter = 15f;
        [SerializeField, Range(0f, 100f)] private float randomNoise = 0f;
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.DiamondGrid)] private int size = 8;
        [SerializeField] private List<NodeAsset> nodeAssets = new List<NodeAsset>();

        private bool isDirty;

        public bool IsDirty() => isDirty;
        public void MarkClean() => isDirty = false;

        public void Validate()
        {
            //if (Vector2.Distance(startPoint, endPoint) < minDistance)
            //{
            //    endPoint = startPoint + Vector2.up * minDistance;
            //}

            nodeCount = new Vector2Int(
                Mathf.Max(nodeCount.x, 3),
                Mathf.Max(nodeCount.y, nodeCount.x + 1)
                );

            samplingArea = new Vector2(
                Mathf.Max(samplingArea.x, 1f),
                Mathf.Max(samplingArea.y, 1f)
            );

            distance = new Vector2(
                Mathf.Max(distance.x, .01f),
                Mathf.Max(distance.x, distance.y)
            );

            distanceFilter = Mathf.Max(distanceFilter, distance.x);
        }

        private void OnValidate()
        {
            Validate();
            isDirty = true;
        }
    }
}