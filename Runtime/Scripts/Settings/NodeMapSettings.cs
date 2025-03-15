using UnityEngine;

namespace HHG.NodeMap.Runtime
{
    [System.Serializable]
    public class NodeMapSettings
    {
        public Algorithm Algorithm => algorithm;
        public Vector2 StartPoint => startPoint;
        public Vector2 EndPoint => endPoint;
        public Vector2 SamplingAreaMin => samplingAreaMin;
        public Vector2 SamplingAreaMax => samplingAreaMax;
        public int Size => size;
        public int Iterations => iterations;
        public int RemovalsPerIteration => removalsPerIteration;
        public int MinNodeCount => minNodeCount;
        public int MaxNodeCount => maxNodeCount;
        public Vector2 Spacing => spacing;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public float AngleFilter => angleFilter;
        public float FilterDistance => filterDistance;
        public float RemovalChance => removalChance;
        public float RandomNoise => randomNoise;

        [SerializeField] private Algorithm algorithm;
        [SerializeField] private int iterations = 8;
        [SerializeField] private int removalsPerIteration = 1;
        [SerializeField] private int minNodeCount = 25;
        [SerializeField] private int maxNodeCount = 100;
        [SerializeField] private Vector2 startPoint = new Vector2(0, -10f);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private Vector2 endPoint = new Vector2(0, 10f);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private Vector2 samplingAreaMin = new Vector2(-10f, -10f);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private Vector2 samplingAreaMax = new Vector2(10f, 10f);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.DiamondGrid)] private int size = 8;
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.DiamondGrid)] private Vector2 spacing = new Vector2(1, 1);
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private float minDistance = 1f;
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private float maxDistance = 1f;
        [SerializeField, ShowIf(nameof(algorithm), Algorithm.PoissonDisk)] private float angleFilter = 15f;
        [SerializeField] private float filterDistance = 10f;
        [SerializeField] private float removalChance = .3f;
        [SerializeField] private float randomNoise = 0f;


        // TODO: Possible to get weird errors, so need proper validation
        public void Validate()
        {
            //if (Vector2.Distance(startPoint, endPoint) < minDistance)
            //{
            //    endPoint = startPoint + Vector2.up * minDistance;
            //}

            minNodeCount = Mathf.Max(minNodeCount, 3);
            maxNodeCount = Mathf.Max(maxNodeCount, minNodeCount + 1);

            samplingAreaMax = new Vector2(
                Mathf.Max(samplingAreaMax.x, samplingAreaMin.x + 1f),
                Mathf.Max(samplingAreaMax.y, samplingAreaMin.y + 1f)
            );

            minDistance = Mathf.Max(minDistance, .1f);
            filterDistance = Mathf.Max(filterDistance, minDistance);
        }
    }
}