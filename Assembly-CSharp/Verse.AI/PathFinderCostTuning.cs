namespace Verse.AI;

public class PathFinderCostTuning
{
	public interface ICustomizer
	{
		int CostOffset(IntVec3 from, IntVec3 to);
	}

	private const int Cost_BlockedWallBase = 70;

	private const float Cost_BlockedWallExtraPerHitPoint = 0.2f;

	private const int Cost_BlockedWallExtraForNaturalWalls = 0;

	private const int Cost_BlockedDoor = 50;

	private const float Cost_BlockedDoorPerHitPoint = 0.2f;

	private const int Cost_OffLordWalkGrid = 70;

	public int costBlockedWallBase = 70;

	public float costBlockedWallExtraPerHitPoint = 0.2f;

	public int costBlockedWallExtraForNaturalWalls;

	public int costBlockedDoor = 50;

	public float costBlockedDoorPerHitPoint = 0.2f;

	public int costOffLordWalkGrid = 70;

	public ICustomizer custom;

	public static readonly PathFinderCostTuning DefaultTuning = new PathFinderCostTuning();
}
