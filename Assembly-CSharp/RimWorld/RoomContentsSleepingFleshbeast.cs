using Verse;

namespace RimWorld;

public class RoomContentsSleepingFleshbeast : RoomContentsWorker
{
	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (room.TryGetRandomCellInRoom(map, out var cell))
		{
			if (GenSpawn.Spawn(PawnGenerator.GeneratePawn(PawnKindDefOf.Fingerspike, Faction.OfEntities), cell, map).TryGetComp(out CompCanBeDormant comp))
			{
				comp.ToSleep();
			}
			for (int i = 0; i < 5; i++)
			{
				FilthMaker.TryMakeFilth(room.Cells.RandomElement(), map, ThingDefOf.Filth_Blood);
			}
		}
	}
}
