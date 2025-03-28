using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class StockGenerator_ReinforcedBarrels : StockGenerator
{
	public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
	{
		if (Find.Storyteller.difficulty.classicMortars)
		{
			yield break;
		}
		foreach (Thing item in StockGeneratorUtility.TryMakeForStock(ThingDefOf.ReinforcedBarrel, RandomCountOf(ThingDefOf.ReinforcedBarrel), faction))
		{
			yield return item;
		}
	}

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		return thingDef == ThingDefOf.ReinforcedBarrel;
	}

	public override IEnumerable<string> ConfigErrors(TraderKindDef parentDef)
	{
		return Enumerable.Empty<string>();
	}
}
