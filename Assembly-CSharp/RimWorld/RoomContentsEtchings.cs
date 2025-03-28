using Verse;

namespace RimWorld;

public class RoomContentsEtchings : RoomContentsWorker
{
	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (room.TryGetRandomCellInRoom(map, out var cell))
		{
			GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.FloorEtching), cell, map);
		}
	}
}
