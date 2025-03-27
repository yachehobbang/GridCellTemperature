using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class BuildingGroundSpawner : GroundSpawner
{
	protected Thing thingToSpawn;

	public IntRange? emergeDelay;

	public List<string> questTagsToForward;

	protected override IntRange ResultSpawnDelay => emergeDelay ?? def.building.groundSpawnerSpawnDelay;

	protected override SoundDef SustainerSound => def.building.groundSpawnerSustainerSound ?? SoundDefOf.Tunnel;

	protected virtual ThingDef ThingDefToSpawn => def.building.groundSpawnerThingToSpawn;

	public Thing ThingToSpawn => thingToSpawn;

	public override void PostMake()
	{
		base.PostMake();
		PostMakeInt();
	}

	protected virtual void PostMakeInt()
	{
		thingToSpawn = ThingMaker.MakeThing(ThingDefToSpawn);
	}

	protected override void Spawn(Map map, IntVec3 pos)
	{
		TerrainDef newTerr = map.Biome.terrainsByFertility.Find((TerrainThreshold t) => t.terrain.affordances.Contains(ThingDefToSpawn.terrainAffordanceNeeded))?.terrain ?? TerrainDefOf.Soil;
		foreach (IntVec3 item in GenAdj.OccupiedRect(pos, Rot4.North, ThingDefToSpawn.Size))
		{
			map.terrainGrid.RemoveTopLayer(item, doLeavings: false);
			if (!item.GetTerrain(map).affordances.Contains(ThingDefToSpawn.terrainAffordanceNeeded))
			{
				map.terrainGrid.SetTerrain(item, newTerr);
			}
		}
		GenSpawn.Spawn(thingToSpawn, pos, map, Rot4.North, WipeMode.FullRefund, respawningAfterLoad: false, forbidLeavings: true);
		thingToSpawn.questTags = questTagsToForward;
		BuildingProperties building = def.building;
		if (building != null && building.groundSpawnerDestroyAdjacent)
		{
			foreach (IntVec3 item2 in GenAdj.CellsAdjacentCardinal(thingToSpawn))
			{
				item2.GetEdifice(map)?.Destroy(DestroyMode.Refund);
			}
		}
		Find.TickManager.slower.SignalForceNormalSpeedShort();
		if (def.building?.groundSpawnerLetterLabel != null && def.building?.groundSpawnerLetterText != null)
		{
			Find.LetterStack.ReceiveLetter(def.building.groundSpawnerLetterLabel, def.building.groundSpawnerLetterText, LetterDefOf.NegativeEvent, new TargetInfo(thingToSpawn));
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref emergeDelay, "emergeDelay", null);
		Scribe_Deep.Look(ref thingToSpawn, "thingToSpawn");
		Scribe_Collections.Look(ref questTagsToForward, "questTagsToForward", LookMode.Value);
	}
}
