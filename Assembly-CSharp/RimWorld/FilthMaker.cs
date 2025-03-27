using System.Collections.Generic;
using Verse;

namespace RimWorld;

public static class FilthMaker
{
	private static List<Filth> toBeRemoved = new List<Filth>();

	public static bool CanMakeFilth(IntVec3 c, Map map, ThingDef filthDef, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
	{
		TerrainDef terrain = c.GetTerrain(map);
		if (!filthDef.filth.ignoreFilthMultiplierStat && (filthDef.filth.placementMask & FilthSourceFlags.Natural) == 0 && Rand.Value > terrain.GetStatValueAbstract(StatDefOf.FilthMultiplier))
		{
			return false;
		}
		FilthSourceFlags filthSourceFlags = filthDef.filth.placementMask | additionalFlags;
		if (terrain.filthAcceptanceMask != 0 && filthSourceFlags.HasFlag(FilthSourceFlags.Pawn))
		{
			if (c.GetRoof(map) != null)
			{
				return true;
			}
			Room room = c.GetRoom(map);
			if (room != null && !room.TouchesMapEdge && !room.UsesOutdoorTemperature)
			{
				return true;
			}
		}
		return TerrainAcceptsFilth(terrain, filthDef, additionalFlags);
	}

	public static bool TerrainAcceptsFilth(TerrainDef terrainDef, ThingDef filthDef, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
	{
		if (terrainDef.filthAcceptanceMask == FilthSourceFlags.None)
		{
			return false;
		}
		FilthSourceFlags filthSourceFlags = filthDef.filth.placementMask | additionalFlags;
		return (terrainDef.filthAcceptanceMask & filthSourceFlags) == filthSourceFlags;
	}

	public static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, int count = 1, FilthSourceFlags additionalFlags = FilthSourceFlags.None, bool shouldPropagate = true)
	{
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			flag |= TryMakeFilth(c, map, filthDef, null, shouldPropagate, out var _, additionalFlags);
		}
		return flag;
	}

	public static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, out Filth outFilth, int count = 1, FilthSourceFlags additionalFlags = FilthSourceFlags.None, bool shouldPropagate = true)
	{
		outFilth = null;
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			flag |= TryMakeFilth(c, map, filthDef, null, shouldPropagate, out outFilth, additionalFlags);
		}
		return flag;
	}

	public static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, string source, int count = 1, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
	{
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			flag |= TryMakeFilth(c, map, filthDef, Gen.YieldSingle(source), shouldPropagate: true, out var _, additionalFlags);
		}
		return flag;
	}

	public static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, IEnumerable<string> sources, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
	{
		Filth outFilth;
		return TryMakeFilth(c, map, filthDef, sources, shouldPropagate: true, out outFilth, additionalFlags);
	}

	public static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, out Filth outFilth, string source, FilthSourceFlags additionalFlags = FilthSourceFlags.None, bool shouldPropagate = true)
	{
		outFilth = null;
		return TryMakeFilth(c, map, filthDef, Gen.YieldSingle(source), shouldPropagate, out outFilth, additionalFlags);
	}

	private static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, IEnumerable<string> sources, bool shouldPropagate, out Filth outFilth, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
	{
		outFilth = (Filth)c.GetThingList(map).FirstOrDefault((Thing t) => t.def == filthDef);
		if (!c.WalkableByAny(map) || (outFilth != null && !outFilth.CanBeThickened))
		{
			if (shouldPropagate)
			{
				List<IntVec3> list = GenAdj.AdjacentCells8WayRandomized();
				for (int i = 0; i < 8; i++)
				{
					IntVec3 c2 = c + list[i];
					if (c2.InBounds(map) && TryMakeFilth(c2, map, filthDef, sources, shouldPropagate: false, out outFilth))
					{
						return true;
					}
				}
			}
			if (outFilth != null)
			{
				outFilth.AddSources(sources);
			}
			return false;
		}
		if (outFilth != null)
		{
			outFilth.ThickenFilth();
			outFilth.AddSources(sources);
		}
		else
		{
			if (!CanMakeFilth(c, map, filthDef, additionalFlags))
			{
				return false;
			}
			outFilth = (Filth)ThingMaker.MakeThing(filthDef);
			outFilth.AddSources(sources);
			GenSpawn.Spawn(outFilth, c, map);
		}
		FilthMonitor.Notify_FilthSpawned();
		return true;
	}

	public static void RemoveAllFilth(IntVec3 c, Map map)
	{
		toBeRemoved.Clear();
		List<Thing> thingList = c.GetThingList(map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (thingList[i] is Filth item)
			{
				toBeRemoved.Add(item);
			}
		}
		for (int j = 0; j < toBeRemoved.Count; j++)
		{
			toBeRemoved[j].Destroy();
		}
		toBeRemoved.Clear();
	}
}
