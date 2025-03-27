using System.Collections.Generic;
using System.Text;
using LudeonTK;
using RimWorld;

namespace Verse.AI;

public sealed class PathGrid
{
	private readonly Map map;

	private readonly bool fenceArePassable;

	public readonly int[] pathGrid;

	public const int ImpassableCost = 10000;

	public PathGrid(Map map, bool fenceArePassable)
	{
		this.map = map;
		this.fenceArePassable = fenceArePassable;
		pathGrid = new int[this.map.cellIndices.NumGridCells];
	}

	public bool Walkable(IntVec3 loc)
	{
		if (!loc.InBounds(map))
		{
			return false;
		}
		return pathGrid[map.cellIndices.CellToIndex(loc)] < 10000;
	}

	public bool WalkableFast(IntVec3 loc)
	{
		return pathGrid[map.cellIndices.CellToIndex(loc)] < 10000;
	}

	public bool WalkableFast(int x, int z)
	{
		return pathGrid[map.cellIndices.CellToIndex(x, z)] < 10000;
	}

	public bool WalkableFast(int index)
	{
		return pathGrid[index] < 10000;
	}

	public int PerceivedPathCostAt(IntVec3 loc)
	{
		return pathGrid[map.cellIndices.CellToIndex(loc)];
	}

	private void RecalculatePerceivedPathCostAt(IntVec3 c)
	{
		bool haveNotified = false;
		RecalculatePerceivedPathCostAt(c, ref haveNotified);
	}

	public void RecalculatePerceivedPathCostAt(IntVec3 c, ref bool haveNotified)
	{
		if (!c.InBounds(map))
		{
			return;
		}
		bool flag = WalkableFast(c);
		pathGrid[map.cellIndices.CellToIndex(c)] = CalculatedCostAt(c, perceivedStatic: true, IntVec3.Invalid);
		if (!haveNotified)
		{
			bool flag2 = WalkableFast(c);
			if (flag2 != flag)
			{
				map.reachability.ClearCache();
				map.regionDirtyer.Notify_WalkabilityChanged(c, flag2);
				haveNotified = true;
			}
		}
	}

	public void RecalculateAllPerceivedPathCosts()
	{
		foreach (IntVec3 allCell in map.AllCells)
		{
			RecalculatePerceivedPathCostAt(allCell);
		}
	}

	public int CalculatedCostAt(IntVec3 c, bool perceivedStatic, IntVec3 prevCell)
	{
		int num = 0;
		bool flag = false;
		TerrainDef terrainDef = map.terrainGrid.TerrainAt(c);
		if (terrainDef == null || terrainDef.passability == Traversability.Impassable)
		{
			return 10000;
		}
		num = terrainDef.pathCost;
		List<Thing> list = map.thingGrid.ThingsListAt(c);
		for (int i = 0; i < list.Count; i++)
		{
			Thing thing = list[i];
			if (thing.def.passability == Traversability.Impassable)
			{
				return 10000;
			}
			if (!fenceArePassable && thing.def.building != null && thing.def.building.isFence)
			{
				return 10000;
			}
			if (!IsPathCostIgnoreRepeater(thing.def) || !prevCell.IsValid || !ContainsPathCostIgnoreRepeater(prevCell))
			{
				int pathCost = thing.def.pathCost;
				if (pathCost > num)
				{
					num = pathCost;
				}
			}
			if (thing is Building_Door && prevCell.IsValid)
			{
				Building edifice = prevCell.GetEdifice(map);
				if (edifice != null && edifice is Building_Door)
				{
					flag = true;
				}
			}
		}
		int num2 = SnowUtility.MovementTicksAddOn(map.snowGrid.GetCategory(c));
		if (num2 > num)
		{
			num = num2;
		}
		if (flag)
		{
			num += 45;
		}
		if (perceivedStatic)
		{
			for (int j = 0; j < 9; j++)
			{
				IntVec3 intVec = GenAdj.AdjacentCellsAndInside[j];
				IntVec3 c2 = c + intVec;
				if (!c2.InBounds(map))
				{
					continue;
				}
				Fire fire = null;
				list = map.thingGrid.ThingsListAtFast(c2);
				for (int k = 0; k < list.Count; k++)
				{
					fire = list[k] as Fire;
					if (fire != null)
					{
						break;
					}
				}
				if (fire != null && fire.parent == null)
				{
					num = ((intVec.x != 0 || intVec.z != 0) ? (num + 150) : (num + 1000));
				}
			}
		}
		return num;
	}

	private bool ContainsPathCostIgnoreRepeater(IntVec3 c)
	{
		List<Thing> list = map.thingGrid.ThingsListAt(c);
		for (int i = 0; i < list.Count; i++)
		{
			if (IsPathCostIgnoreRepeater(list[i].def))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsPathCostIgnoreRepeater(ThingDef def)
	{
		if (def.pathCost >= 25)
		{
			return def.pathCostIgnoreRepeat;
		}
		return false;
	}

	[DebugOutput]
	public static void ThingPathCostsIgnoreRepeaters()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("===============PATH COST IGNORE REPEATERS==============");
		foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (IsPathCostIgnoreRepeater(allDef) && allDef.passability != Traversability.Impassable)
			{
				stringBuilder.AppendLine(allDef.defName + " " + allDef.pathCost);
			}
		}
		stringBuilder.AppendLine("===============NON-PATH COST IGNORE REPEATERS that are buildings with >0 pathCost ==============");
		foreach (ThingDef allDef2 in DefDatabase<ThingDef>.AllDefs)
		{
			if (!IsPathCostIgnoreRepeater(allDef2) && allDef2.passability != Traversability.Impassable && allDef2.category == ThingCategory.Building && allDef2.pathCost > 0)
			{
				stringBuilder.AppendLine(allDef2.defName + " " + allDef2.pathCost);
			}
		}
		Log.Message(stringBuilder.ToString());
	}
}
