using Verse;

namespace RimWorld;

public class RoomContentsCorpseRoom : RoomContentsDeadBodyLabyrinth
{
	private static readonly IntRange CorpseRange = new IntRange(5, 10);

	private static readonly IntRange BloodFilthRange = new IntRange(4, 10);

	public override void FillRoom(Map map, LayoutRoom room)
	{
		int randomInRange = CorpseRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			SpawnCorpse(map, room);
		}
		int randomInRange2 = BloodFilthRange.RandomInRange;
		for (int j = 0; j < randomInRange2; j++)
		{
			if (room.TryGetRandomCellInRoom(map, out var cell))
			{
				FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Blood);
			}
		}
	}
}
