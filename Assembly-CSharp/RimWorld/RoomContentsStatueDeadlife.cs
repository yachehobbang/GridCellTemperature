using Verse;

namespace RimWorld;

public class RoomContentsStatueDeadlife : RoomContentsDeadBodyLabyrinth
{
	private static readonly IntRange CorpseRange = new IntRange(1, 3);

	private static readonly IntRange BloodFilthRange = new IntRange(2, 4);

	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (!room.TryGetRandomCellInRoom(map, out var cell, 4))
		{
			cell = room.rects[0].CenterCell;
		}
		GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.GrayStatueDeadlifeDust), cell, map);
		int randomInRange = CorpseRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			if (RCellFinder.TryFindRandomCellNearWith(cell, (IntVec3 c) => ValidCell(c, map), map, out var result, 2, 6))
			{
				SpawnCorpse(result, map).InnerPawn.inventory.DestroyAll();
			}
		}
		int randomInRange2 = BloodFilthRange.RandomInRange;
		for (int j = 0; j < randomInRange2; j++)
		{
			if (room.TryGetRandomCellInRoom(map, out var cell2))
			{
				FilthMaker.TryMakeFilth(cell2, map, ThingDefOf.Filth_Blood);
			}
		}
	}

	private bool ValidCell(IntVec3 cell, Map map)
	{
		return cell.GetFirstThing<Thing>(map) == null;
	}
}
