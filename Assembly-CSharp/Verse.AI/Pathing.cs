namespace Verse.AI;

public class Pathing
{
	private readonly PathingContext normal;

	private readonly PathingContext fenceBlocked;

	public PathingContext Normal => normal;

	public PathingContext FenceBlocked => fenceBlocked;

	public Pathing(Map map)
	{
		normal = new PathingContext(map, new PathGrid(map, fenceArePassable: true));
		fenceBlocked = new PathingContext(map, new PathGrid(map, fenceArePassable: false));
	}

	public PathingContext For(TraverseParms parms)
	{
		if (parms.fenceBlocked && !parms.canBashFences)
		{
			return fenceBlocked;
		}
		return normal;
	}

	public PathingContext For(Pawn pawn)
	{
		if (pawn != null && pawn.ShouldAvoidFences && (pawn.CurJob == null || !pawn.CurJob.canBashFences))
		{
			return fenceBlocked;
		}
		return normal;
	}

	public void RecalculateAllPerceivedPathCosts()
	{
		normal.pathGrid.RecalculateAllPerceivedPathCosts();
		fenceBlocked.pathGrid.RecalculateAllPerceivedPathCosts();
	}

	public void RecalculatePerceivedPathCostUnderThing(Thing thing)
	{
		if (thing.def.size == IntVec2.One)
		{
			RecalculatePerceivedPathCostAt(thing.Position);
			return;
		}
		CellRect cellRect = thing.OccupiedRect();
		for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
		{
			for (int j = cellRect.minX; j <= cellRect.maxX; j++)
			{
				IntVec3 c = new IntVec3(j, 0, i);
				RecalculatePerceivedPathCostAt(c);
			}
		}
	}

	public void RecalculatePerceivedPathCostAt(IntVec3 c)
	{
		bool haveNotified = false;
		normal.pathGrid.RecalculatePerceivedPathCostAt(c, ref haveNotified);
		fenceBlocked.pathGrid.RecalculatePerceivedPathCostAt(c, ref haveNotified);
	}
}
