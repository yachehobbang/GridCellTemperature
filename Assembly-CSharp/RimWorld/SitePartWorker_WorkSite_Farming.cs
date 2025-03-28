using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class SitePartWorker_WorkSite_Farming : SitePartWorker_WorkSite
{
	public override IEnumerable<PreceptDef> DisallowedPrecepts => DefDatabase<PreceptDef>.AllDefs.Where((PreceptDef p) => p.disallowFarmingCamps);

	public override PawnGroupKindDef WorkerGroupKind => PawnGroupKindDefOf.Farmers;

	public override bool CanSpawnOn(int tile)
	{
		if (!Find.WorldGrid[tile].biome.allowFarmingCamps)
		{
			return false;
		}
		return LootThings(tile).Where((Func<CampLootThingStruct, bool>)delegate
		{
			float seasonalTemp = Find.World.tileTemperatures.GetSeasonalTemp(tile);
			return seasonalTemp >= 6f && seasonalTemp <= 42f;
		}).Any();
	}

	public override IEnumerable<CampLootThingStruct> LootThings(int tile)
	{
		IEnumerable<ThingDef> enumerable = from t in DefDatabase<ThingDef>.AllDefsListForReading.Where(IsFoodCrop)
			select t.plant.harvestedThingDef;
		float cropWeight = 1f / (float)enumerable.Count();
		foreach (ThingDef item in enumerable)
		{
			yield return new CampLootThingStruct
			{
				thing = item,
				weight = cropWeight
			};
		}
		static bool IsFoodCrop(ThingDef thingDef)
		{
			if (thingDef.plant == null)
			{
				return false;
			}
			if (!thingDef.plant.Sowable)
			{
				return false;
			}
			if (thingDef.plant.cavePlant)
			{
				return false;
			}
			if (thingDef.plant.harvestedThingDef == null)
			{
				return false;
			}
			if (!thingDef.plant.harvestedThingDef.IsIngestible)
			{
				return false;
			}
			if (!PawnKindDefOf.Colonist.RaceProps.CanEverEat(thingDef.plant.harvestedThingDef))
			{
				return false;
			}
			return true;
		}
	}
}
