namespace HHG.NodeMap.Runtime
{
	public interface INodeMapGenerator
	{
		NodeMap Generate(NodeMapSettings settings);
	}
}