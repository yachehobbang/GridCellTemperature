using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Verse;

namespace RimWorld.BaseGen;

public static class BaseGenUtility
{
	private static List<TerrainDef> tmpFactionFloors = new List<TerrainDef>();

	private static List<IntVec3> bridgeCells = new List<IntVec3>();

	public static ThingDef CheapStuffFor(ThingDef thingDef, Faction faction)
	{
		ThingDef thingDef2 = DefDatabase<ThingDef>.AllDefs.Where((ThingDef stuff) => stuff.stuffProps != null && stuff.BaseMarketValue / stuff.VolumePerUnit < 5f && stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Stony) && stuff.stuffProps.CanMake(thingDef)).RandomElementWithFallback();
		if (thingDef2 != null)
		{
			return thingDef2;
		}
		if (ThingDefOf.WoodLog.stuffProps.CanMake(thingDef))
		{
			return ThingDefOf.WoodLog;
		}
		return GenStuff.RandomStuffInexpensiveFor(thingDef, faction);
	}

	public static ThingDef RandomCheapWallStuff(Faction faction, bool notVeryFlammable = false)
	{
		return RandomCheapWallStuff(faction?.def.techLevel ?? TechLevel.Spacer, notVeryFlammable);
	}

	public static ThingDef RandomCheapWallStuff(TechLevel techLevel, bool notVeryFlammable = false)
	{
		if (techLevel.IsNeolithicOrWorse())
		{
			return ThingDefOf.WoodLog;
		}
		ThingDef thingDef = DefDatabase<ThingDef>.AllDefs.Where((ThingDef stuff) => stuff.stuffProps != null && IsCheapWallStuff(stuff) && stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Stony)).RandomElementWithFallback();
		if (thingDef != null)
		{
			return thingDef;
		}
		return DefDatabase<ThingDef>.AllDefsListForReading.Where((ThingDef d) => IsCheapWallStuff(d) && (!notVeryFlammable || d.BaseFlammability < 0.5f)).RandomElement();
	}

	public static bool IsCheapWallStuff(ThingDef d)
	{
		if (d.IsStuff && d.stuffProps.CanMake(ThingDefOf.Wall))
		{
			return d.BaseMarketValue / d.VolumePerUnit < 5f;
		}
		return false;
	}

	public static ThingDef RandomHightechWallStuff()
	{
		if (Rand.Value < 0.15f)
		{
			return ThingDefOf.Plasteel;
		}
		return ThingDefOf.Steel;
	}

	public static TerrainDef RandomHightechFloorDef()
	{
		return Rand.Element(TerrainDefOf.Concrete, TerrainDefOf.Concrete, TerrainDefOf.PavedTile, TerrainDefOf.PavedTile, TerrainDefOf.MetalTile);
	}

	private static IEnumerable<TerrainDef> IdeoFloorTypes(Faction faction, bool allowCarpet = true)
	{
		if (faction == null || faction.ideos == null)
		{
			yield break;
		}
		foreach (Ideo allIdeo in faction.ideos.AllIdeos)
		{
			foreach (BuildableDef cachedPossibleBuildable in allIdeo.cachedPossibleBuildables)
			{
				if (cachedPossibleBuildable is TerrainDef terrainDef && (allowCarpet || !terrainDef.IsCarpet))
				{
					yield return terrainDef;
				}
			}
		}
	}

	public static TerrainDef RandomBasicFloorDef(Faction faction, bool allowCarpet = false)
	{
		bool flag = allowCarpet && (faction == null || !faction.def.techLevel.IsNeolithicOrWorse()) && Rand.Chance(0.1f);
		if (faction != null && faction.ideos != null && IdeoFloorTypes(faction, flag).TryRandomElement(out var result))
		{
			return result;
		}
		if (flag)
		{
			return DefDatabase<TerrainDef>.AllDefsListForReading.Where((TerrainDef x) => x.IsCarpet).RandomElement();
		}
		return Rand.Element(TerrainDefOf.MetalTile, TerrainDefOf.PavedTile, TerrainDefOf.WoodPlankFloor, TerrainDefOf.TileSandstone);
	}

	public static bool TryRandomInexpensiveFloor(out TerrainDef floor, Predicate<TerrainDef> validator = null)
	{
		Func<TerrainDef, float> costCalculator = delegate(TerrainDef x)
		{
			List<ThingDefCountClass> list = x.CostListAdjusted(null);
			float num = 0f;
			for (int i = 0; i < list.Count; i++)
			{
				num += (float)list[i].count * list[i].thingDef.BaseMarketValue;
			}
			return num;
		};
		IEnumerable<TerrainDef> enumerable = DefDatabase<TerrainDef>.AllDefs.Where((TerrainDef x) => x.BuildableByPlayer && x.terrainAffordanceNeeded != TerrainAffordanceDefOf.Bridgeable && !x.IsCarpet && (validator == null || validator(x)) && costCalculator(x) > 0f);
		float cheapest = -1f;
		foreach (TerrainDef item in enumerable)
		{
			float num2 = costCalculator(item);
			if (cheapest == -1f || num2 < cheapest)
			{
				cheapest = num2;
			}
		}
		return enumerable.Where((TerrainDef x) => costCalculator(x) <= cheapest * 4f).TryRandomElement(out floor);
	}

	public static TerrainDef CorrespondingTerrainDef(ThingDef stuffDef, bool beautiful, Faction faction = null)
	{
		tmpFactionFloors.Clear();
		if (faction != null && faction.ideos != null)
		{
			foreach (TerrainDef item in IdeoFloorTypes(faction))
			{
				if (item.CostList == null)
				{
					continue;
				}
				for (int i = 0; i < item.CostList.Count; i++)
				{
					if (item.CostList[i].thingDef == stuffDef)
					{
						tmpFactionFloors.Add(item);
						break;
					}
				}
			}
			if (tmpFactionFloors.Any() && tmpFactionFloors.TryRandomElementByWeight(delegate(TerrainDef x)
			{
				float statOffsetFromList = x.statBases.GetStatOffsetFromList(StatDefOf.Beauty);
				if (statOffsetFromList == 0f)
				{
					return 0f;
				}
				return (!beautiful) ? (1f / statOffsetFromList) : statOffsetFromList;
			}, out var result))
			{
				return result;
			}
		}
		TerrainDef terrainDef = null;
		List<TerrainDef> allDefsListForReading = DefDatabase<TerrainDef>.AllDefsListForReading;
		for (int j = 0; j < allDefsListForReading.Count; j++)
		{
			if (allDefsListForReading[j].CostList == null)
			{
				continue;
			}
			for (int k = 0; k < allDefsListForReading[j].CostList.Count; k++)
			{
				if (allDefsListForReading[j].CostList[k].thingDef == stuffDef && (terrainDef == null || (beautiful ? (terrainDef.statBases.GetStatOffsetFromList(StatDefOf.Beauty) < allDefsListForReading[j].statBases.GetStatOffsetFromList(StatDefOf.Beauty)) : (terrainDef.statBases.GetStatOffsetFromList(StatDefOf.Beauty) > allDefsListForReading[j].statBases.GetStatOffsetFromList(StatDefOf.Beauty)))))
				{
					terrainDef = allDefsListForReading[j];
				}
			}
		}
		if (terrainDef == null)
		{
			terrainDef = TerrainDefOf.Concrete;
		}
		return terrainDef;
	}

	public static TerrainDef RegionalRockTerrainDef(int tile, bool beautiful)
	{
		ThingDef thingDef = Find.World.NaturalRockTypesIn(tile).RandomElementWithFallback()?.building.mineableThing;
		return CorrespondingTerrainDef((thingDef != null && thingDef.butcherProducts != null && thingDef.butcherProducts.Count > 0) ? thingDef.butcherProducts[0].thingDef : null, beautiful);
	}

	public static bool AnyDoorAdjacentCardinalTo(IntVec3 cell, Map map)
	{
		for (int i = 0; i < 4; i++)
		{
			IntVec3 c = cell + GenAdj.CardinalDirections[i];
			if (c.InBounds(map) && c.GetDoor(map) != null)
			{
				return true;
			}
		}
		return false;
	}

	public static bool AnyDoorAdjacentCardinalTo(CellRect rect, Map map)
	{
		foreach (IntVec3 item in rect.AdjacentCellsCardinal)
		{
			if (item.InBounds(map) && item.GetDoor(map) != null)
			{
				return true;
			}
		}
		return false;
	}

	public static ThingDef WallStuffAt(IntVec3 c, Map map)
	{
		Building edifice = c.GetEdifice(map);
		if (edifice != null && edifice.def == ThingDefOf.Wall)
		{
			return edifice.Stuff;
		}
		return null;
	}

	public static void CheckSpawnBridgeUnder(ThingDef thingDef, IntVec3 c, Rot4 rot)
	{
		if (thingDef.category != ThingCategory.Building)
		{
			return;
		}
		Map map = BaseGen.globalSettings.map;
		CellRect cellRect = GenAdj.OccupiedRect(c, rot, thingDef.size);
		bridgeCells.Clear();
		foreach (IntVec3 item in cellRect)
		{
			if (!item.SupportsStructureType(map, thingDef.terrainAffordanceNeeded) && GenConstruct.CanBuildOnTerrain(TerrainDefOf.Bridge, item, map, Rot4.North))
			{
				bridgeCells.Add(item);
			}
		}
		if (!bridgeCells.Any())
		{
			return;
		}
		if (thingDef.size.x != 1 || thingDef.size.z != 1)
		{
			for (int num = bridgeCells.Count - 1; num >= 0; num--)
			{
				for (int i = 0; i < 8; i++)
				{
					IntVec3 intVec = bridgeCells[num] + GenAdj.AdjacentCells[i];
					if (!bridgeCells.Contains(intVec) && intVec.InBounds(map) && !intVec.SupportsStructureType(map, thingDef.terrainAffordanceNeeded) && GenConstruct.CanBuildOnTerrain(TerrainDefOf.Bridge, intVec, map, Rot4.North))
					{
						bridgeCells.Add(intVec);
					}
				}
			}
		}
		for (int j = 0; j < bridgeCells.Count; j++)
		{
			map.terrainGrid.SetTerrain(bridgeCells[j], TerrainDefOf.Bridge);
		}
	}

	[DebugOutput]
	private static void WallStuffs()
	{
		DebugTables.MakeTablesDialog(GenStuff.AllowedStuffsFor(ThingDefOf.Wall), new TableDataGetter<ThingDef>("defName", (ThingDef d) => d.defName), new TableDataGetter<ThingDef>("cheap", (ThingDef d) => IsCheapWallStuff(d).ToStringCheckBlank()), new TableDataGetter<ThingDef>("floor", (ThingDef d) => CorrespondingTerrainDef(d, beautiful: false).defName), new TableDataGetter<ThingDef>("floor (beautiful)", (ThingDef d) => CorrespondingTerrainDef(d, beautiful: true).defName));
	}

	public static void DoPathwayBetween(IntVec3 a, IntVec3 b, TerrainDef terrainDef, int size = 3)
	{
		foreach (IntVec3 item in GenSight.PointsOnLineOfSight(a, b))
		{
			foreach (IntVec3 item2 in CellRect.CenteredOn(item, size, size))
			{
				if (item2.InBounds(BaseGen.globalSettings.map))
				{
					BaseGen.globalSettings.map.terrainGrid.SetTerrain(item2, terrainDef);
				}
			}
		}
	}
}
