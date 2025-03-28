namespace Verse.AI;

public class PathingContext
{
	public readonly Map map;

	public readonly PathGrid pathGrid;

	public PathingContext(Map map, PathGrid pathGrid)
	{
		this.map = map;
		this.pathGrid = pathGrid;
	}
}
