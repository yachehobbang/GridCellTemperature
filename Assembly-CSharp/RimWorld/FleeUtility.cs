using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public static class FleeUtility
{
	private static int MinFiresNearbyRadius = 20;

	private static int MinFiresNearbyRegionsToScan = 18;

	private static List<Thing> tmpThings = new List<Thing>();

	public static Job FleeJob(Pawn pawn, Thing danger, int fleeDistance)
	{
		IntVec3 intVec;
		if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Flee)
		{
			intVec = pawn.CurJob.targetA.Cell;
		}
		else
		{
			tmpThings.Clear();
			tmpThings.Add(danger);
			intVec = CellFinderLoose.GetFleeDest(pawn, tmpThings, fleeDistance);
			tmpThings.Clear();
		}
		if (intVec != pawn.Position)
		{
			return JobMaker.MakeJob(JobDefOf.Flee, intVec, danger);
		}
		return null;
	}

	public static Job FleeLargeFireJob(Pawn pawn, int minFiresNearbyToFlee, int distToFireToFlee, int fleeDistance)
	{
		if (pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Fire).Count < minFiresNearbyToFlee)
		{
			return null;
		}
		TraverseParms tp = TraverseParms.For(pawn);
		Fire closestFire = null;
		float closestDistSq = -1f;
		int firesCount = 0;
		RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (Region from, Region to) => to.Allows(tp, isDestination: false), delegate(Region x)
		{
			List<Thing> list = x.ListerThings.ThingsInGroup(ThingRequestGroup.Fire);
			for (int i = 0; i < list.Count; i++)
			{
				float num = pawn.Position.DistanceToSquared(list[i].Position);
				if (!(num > (float)(MinFiresNearbyRadius * MinFiresNearbyRadius)))
				{
					if (closestFire == null || num < closestDistSq)
					{
						closestDistSq = num;
						closestFire = (Fire)list[i];
					}
					firesCount++;
				}
			}
			return closestDistSq <= (float)(distToFireToFlee * distToFireToFlee) && firesCount >= minFiresNearbyToFlee;
		}, MinFiresNearbyRegionsToScan);
		if (closestDistSq <= (float)(distToFireToFlee * distToFireToFlee) && firesCount >= minFiresNearbyToFlee)
		{
			Job job = FleeJob(pawn, closestFire, fleeDistance);
			if (job != null)
			{
				return job;
			}
		}
		return null;
	}
}
