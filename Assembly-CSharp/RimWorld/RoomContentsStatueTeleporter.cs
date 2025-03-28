using Verse;

namespace RimWorld;

public class RoomContentsStatueTeleporter : RoomContentsWorker
{
	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (!room.TryGetRandomCellInRoom(map, out var cell, 4))
		{
			cell = room.rects[0].CenterCell;
		}
		GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.GrayStatueTeleporter), cell, map);
	}
}
