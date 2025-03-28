using System;
using RimWorld;
using RimWorld.Planet;

namespace Verse;

public static class GetOrGenerateMapUtility
{
	public static Map GetOrGenerateMap(int tile, IntVec3 size, WorldObjectDef suggestedMapParentDef)
	{
		Map map = Current.Game.FindMap(tile);
		if (map == null)
		{
			MapParent mapParent = Find.WorldObjects.MapParentAt(tile);
			if (mapParent == null)
			{
				if (suggestedMapParentDef == null)
				{
					Log.Error("Tried to get or generate map at " + tile + ", but there isn't any MapParent world object here and map parent def argument is null.");
					return null;
				}
				mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(suggestedMapParentDef);
				mapParent.Tile = tile;
				Find.WorldObjects.Add(mapParent);
			}
			map = MapGenerator.GenerateMap(size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs);
		}
		return map;
	}

	public static Map GetOrGenerateMap(int tile, WorldObjectDef suggestedMapParentDef)
	{
		return GetOrGenerateMap(tile, Find.World.info.initialMapSize, suggestedMapParentDef);
	}

	public static void UnfogMapFromEdge(Map map)
	{
		Predicate<IntVec3> validator = delegate(IntVec3 c)
		{
			if (!c.Standable(map))
			{
				return false;
			}
			if (c.Roofed(map))
			{
				return false;
			}
			return map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater)) ? true : false;
		};
		if (CellFinder.TryFindRandomCellNear(map.Center, map, 30, validator, out var result) || CellFinder.TryFindRandomEdgeCellWith(validator, map, 0f, out result) || CellFinder.TryFindRandomCell(map, validator, out result))
		{
			FloodFillerFog.FloodUnfog(result, map);
		}
	}
}
