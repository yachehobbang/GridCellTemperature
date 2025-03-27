using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class RitualOutcomeEffectWorker_GiveMemoryBelievers : RitualOutcomeEffectWorker
{
	public RitualOutcomeEffectWorker_GiveMemoryBelievers()
	{
	}

	public RitualOutcomeEffectWorker_GiveMemoryBelievers(RitualOutcomeEffectDef def)
		: base(def)
	{
	}

	public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
	{
		foreach (Pawn allMapsCaravansAndTravelingTransportPods_Alive_Colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
		{
			if (allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Ideo == jobRitual.Ritual.ideo)
			{
				allMapsCaravansAndTravelingTransportPods_Alive_Colonist.needs.mood.thoughts.memories.TryGainMemory(MakeMemory(allMapsCaravansAndTravelingTransportPods_Alive_Colonist, jobRitual));
			}
		}
	}
}
