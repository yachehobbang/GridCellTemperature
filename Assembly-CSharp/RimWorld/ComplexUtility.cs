using System.Collections.Generic;
using Verse;

namespace RimWorld;

public static class ComplexUtility
{
	public static bool TryFindRandomSpawnCell(ThingDef def, IEnumerable<IntVec3> cells, Map map, out IntVec3 spawnPosition, int gap = 1, Rot4? rot = null)
	{
		foreach (IntVec3 item in cells.InRandomOrder())
		{
			CellRect cellRect = GenAdj.OccupiedRect(item, rot ?? Rot4.North, def.Size).ExpandedBy(gap);
			bool flag = false;
			foreach (IntVec3 item2 in cellRect)
			{
				if (!item2.InBounds(map) || item2.GetEdifice(map) != null || item2.GetFirstPawn(map) != null)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				spawnPosition = item;
				return true;
			}
		}
		spawnPosition = IntVec3.Invalid;
		return false;
	}

	public static string SpawnRoomEnteredTrigger(List<CellRect> room, Map map)
	{
		string text = "RoomEntered" + Find.UniqueIDsManager.GetNextSignalTagID();
		foreach (CellRect item in room)
		{
			RectTrigger obj = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger);
			obj.signalTag = text;
			obj.Rect = item;
			GenSpawn.Spawn(obj, item.CenterCell, map);
		}
		return text;
	}

	public static string SpawnRadialDistanceTrigger(IEnumerable<Thing> things, Map map, int radius)
	{
		string text = "RandomTrigger" + Find.UniqueIDsManager.GetNextSignalTagID();
		foreach (Thing thing in things)
		{
			RadialTrigger obj = (RadialTrigger)ThingMaker.MakeThing(ThingDefOf.RadialTrigger);
			obj.signalTag = text;
			obj.maxRadius = radius;
			obj.lineOfSight = true;
			GenSpawn.Spawn(obj, thing.Position, map);
		}
		return text;
	}
}
