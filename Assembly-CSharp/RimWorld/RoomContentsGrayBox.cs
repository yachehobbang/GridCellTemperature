using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class RoomContentsGrayBox : RoomContentsWorker
{
	public override void FillRoom(Map map, LayoutRoom room)
	{
		TrySpawnBoxInRoom(map, room, out var _);
	}

	public static bool TrySpawnBoxInRoom(Map map, LayoutRoom room, out Building_Crate spawned)
	{
		spawned = null;
		if (!room.TryGetRandomCellInRoom(map, out var cell, 2, (IntVec3 c) => (c + Rot4.South.FacingCell).GetFirstBuilding(map) == null))
		{
			return false;
		}
		spawned = SpawnBoxInRoom(cell, map);
		return true;
	}

	public static Building_Crate SpawnBoxInRoom(IntVec3 cell, Map map, ThingSetMakerDef rewardMaker = null, bool addRewards = true)
	{
		Building_Crate building_Crate = (Building_Crate)GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.GrayBox), cell, map, Rot4.South);
		if (addRewards)
		{
			List<Thing> list = (rewardMaker ?? ThingSetMakerDefOf.Reward_GrayBox).root.Generate(default(ThingSetMakerParams));
			for (int num = list.Count - 1; num >= 0; num--)
			{
				Thing thing = list[num];
				if (!building_Crate.TryAcceptThing(thing, allowSpecialEffects: false))
				{
					thing.Destroy();
				}
			}
		}
		else
		{
			building_Crate.Open();
		}
		return building_Crate;
	}
}
