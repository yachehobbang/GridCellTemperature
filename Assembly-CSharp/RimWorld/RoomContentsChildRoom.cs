using UnityEngine;
using Verse;

namespace RimWorld;

public class RoomContentsChildRoom : RoomContentsDeadBodyLabyrinth
{
	private static readonly FloatRange YearsAgeChild = new FloatRange(2f, 3f);

	private static readonly FloatRange YearsAgeAdult = new FloatRange(20f, 40f);

	private static readonly FloatRange CorpseAgeDaysRange = new FloatRange(30f, 60f);

	private static readonly IntRange BloodFilthRange = new IntRange(3, 6);

	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (!room.TryGetRandomCellInRoom(map, out var cell))
		{
			return;
		}
		int deadTicks = Mathf.RoundToInt(CorpseAgeDaysRange.RandomInRange * 60000f);
		SpawnCorpse(cell, PawnKindDefOf.Villager, deadTicks, map, YearsAgeChild.RandomInRange, forceNoGear: true);
		foreach (IntVec3 item in GenAdjFast.AdjacentCellsCardinal(cell))
		{
			if (item.Standable(map))
			{
				deadTicks = Mathf.RoundToInt(CorpseAgeDaysRange.RandomInRange * 60000f);
				SpawnCorpse(item, PawnKindDefOf.Villager, deadTicks, map, YearsAgeAdult.RandomInRange);
				break;
			}
		}
		int randomInRange = BloodFilthRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			if (!room.TryGetRandomCellInRoom(map, out cell))
			{
				break;
			}
			FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Blood);
		}
		if (ModsConfig.BiotechActive && room.TryGetRandomCellInRoom(map, out cell))
		{
			FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Floordrawing);
		}
	}
}
