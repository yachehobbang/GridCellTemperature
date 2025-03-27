using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;

namespace Verse;

public static class CaravanPollutionUtility
{
	private const float ModeratePollutionToxicDamageFactor = 0.5f;

	public static void CheckDamageFromPollution(Caravan caravan)
	{
		if (Find.TickManager.TicksGame % 3451 == 0 && Find.WorldGrid[caravan.Tile].PollutionLevel() >= PollutionLevel.Moderate)
		{
			float extraFactor = ToxicDamagePollutionFactor(caravan.Tile);
			List<Pawn> pawnsListForReading = caravan.PawnsListForReading;
			for (int i = 0; i < pawnsListForReading.Count; i++)
			{
				GameCondition_ToxicFallout.DoPawnToxicDamage(pawnsListForReading[i], protectedByRoof: false, extraFactor);
			}
		}
	}

	public static float ToxicDamagePollutionFactor(int tile)
	{
		PollutionLevel pollutionLevel = Find.WorldGrid[tile].PollutionLevel();
		if (pollutionLevel == PollutionLevel.Moderate)
		{
			return 0.5f;
		}
		return 1f;
	}
}
