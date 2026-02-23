namespace HHG.NodeMap.Runtime
{
	public interface IAlgorithm
	{
		NodeMap Generate(NodeMapSettingsAsset settings, System.Random random);
	}
}