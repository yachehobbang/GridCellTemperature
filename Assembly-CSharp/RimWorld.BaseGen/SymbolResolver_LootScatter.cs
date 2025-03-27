using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld.BaseGen;

public class SymbolResolver_LootScatter : SymbolResolver
{
	private static readonly IntRange DefaultLootCountRange = new IntRange(3, 10);

	public override void Resolve(ResolveParams rp)
	{
		if (rp.lootMarketValue.HasValue && rp.lootMarketValue.Value <= 0f)
		{
			return;
		}
		Map map = BaseGen.globalSettings.map;
		IList<Thing> list = rp.lootConcereteContents;
		if (list == null)
		{
			ThingSetMakerParams parms;
			if (rp.thingSetMakerParams.HasValue)
			{
				parms = rp.thingSetMakerParams.Value;
			}
			else
			{
				parms = default(ThingSetMakerParams);
				parms.countRange = DefaultLootCountRange;
				parms.techLevel = ((rp.faction != null) ? rp.faction.def.techLevel : TechLevel.Undefined);
			}
			parms.makingFaction = rp.faction;
			parms.totalMarketValueRange = new FloatRange(rp.lootMarketValue.Value, rp.lootMarketValue.Value);
			list = rp.thingSetMakerDef.root.Generate(parms);
		}
		List<IntVec3> list2 = rp.rect.Cells.Where((IntVec3 c) => CanPlace(c)).ToList();
		while (list2.Count > 0 && list.Count > 0)
		{
			int index = Rand.Range(0, list2.Count);
			IntVec3 loc = list2[index];
			list2.RemoveAt(index);
			index = Rand.Range(0, list.Count);
			Thing newThing = list[index];
			list.RemoveAt(index);
			GenSpawn.Spawn(newThing, loc, map);
		}
		if (list.Count <= 0)
		{
			return;
		}
		Log.Warning("Could not scatter loot things in rooms: " + string.Join(", ", list.Select((Thing t) => t.Label)));
		foreach (Thing item in list)
		{
			for (int num = 1000; num > 0; num--)
			{
				IntVec3 intVec = CellFinder.RandomCell(map);
				if (CanPlace(intVec))
				{
					GenSpawn.Spawn(item, intVec, map);
				}
			}
		}
		bool CanPlace(IntVec3 cell)
		{
			if (cell.GetFirstItem(map) != null)
			{
				return false;
			}
			if (!cell.Standable(map))
			{
				return false;
			}
			if (cell.GetRoom(map).PsychologicallyOutdoors)
			{
				return false;
			}
			return true;
		}
	}
}
