using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class RoomRoleWorker_Laboratory : RoomRoleWorker
{
	public override float GetScore(Room room)
	{
		int num = 0;
		List<Thing> containedAndAdjacentThings = room.ContainedAndAdjacentThings;
		for (int i = 0; i < containedAndAdjacentThings.Count; i++)
		{
			Thing thing = containedAndAdjacentThings[i];
			if (thing is Building_ResearchBench || thing is Building_GeneAssembler || (thing is Building_WorkTable && thing.def == ThingDefOf.SubcoreEncoder) || thing is Building_SubcoreScanner || thing is Building_MechGestator)
			{
				num++;
			}
		}
		return 30f * (float)num;
	}
}
