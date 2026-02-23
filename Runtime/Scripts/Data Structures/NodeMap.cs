using HHG.Common.Runtime;
using System.Collections.Generic;

namespace HHG.NodeMap.Runtime
{
    [System.Serializable]
    public class NodeMap : ICloneable<NodeMap>
    {
        public int Seed;
        public Node Start;
        public Node End;
        public List<Node> Nodes = new List<Node>();
        public List<Connection> Connections = new List<Connection>();

        public NodeMap Clone()
        {
            NodeMap clone = (NodeMap)MemberwiseClone();
            clone.Nodes = new List<Node>(Nodes);
            clone.Connections = new List<Connection>(Connections);
            return clone;
        }
    }
}