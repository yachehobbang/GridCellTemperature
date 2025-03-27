using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class WorkGiver_TendOther_Humanlike : WorkGiver_TendOther
{
	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.mapPawns.SpawnedHumanlikesWithAnyHediff;
	}
}
