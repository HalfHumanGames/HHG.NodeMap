using HHG.Common.Runtime;
using System.Collections.Generic;

namespace HHG.NodeMap.Runtime
{
    [System.Serializable]
    public class NodeMap : ICloneable<NodeMap>
    {
        public Node Start;
        public Node End;
        public List<Node> Vertices = new List<Node>();
        public List<Path> Paths = new List<Path>();

        public NodeMap Clone()
        {
            NodeMap clone = (NodeMap)MemberwiseClone();
            clone.Vertices = new List<Node>(Vertices);
            clone.Paths = new List<Path>(Paths);
            return clone;
        }
    }
}