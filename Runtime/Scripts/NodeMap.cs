using System.Collections.Generic;

namespace HHG.NodeMapSystem.Runtime
{
    [System.Serializable]
    public class NodeMap
    {
        public List<Node> Vertices = new List<Node>();
        public List<Path> Connections = new List<Path>();
    }
}