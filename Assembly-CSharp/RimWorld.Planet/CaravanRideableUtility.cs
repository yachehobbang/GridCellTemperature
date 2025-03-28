using System.Text;
using Verse;

namespace RimWorld.Planet;

public static class CaravanRideableUtility
{
	public static bool IsCaravanRideable(this Pawn pawn)
	{
		if (!pawn.Downed && pawn.ageTracker.CurLifeStage.caravanRideable)
		{
			return pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor) > 1f;
		}
		return false;
	}

	public static bool IsCaravanRideable(this ThingDef def)
	{
		return def.StatBaseDefined(StatDefOf.CaravanRidingSpeedFactor);
	}

	public static string RideableLifeStagesDesc(RaceProperties raceProps)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (LifeStageAge lifeStageAge in raceProps.lifeStageAges)
		{
			if (lifeStageAge.def.caravanRideable)
			{
				stringBuilder.AppendWithComma(lifeStageAge.def.label);
			}
		}
		return stringBuilder.ToString().CapitalizeFirst();
	}

	public static string GetIconTooltipText(Pawn pawn)
	{
		float statValue = pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor);
		return "RideableAnimalTip".Translate() + "\n\n" + StatDefOf.CaravanRidingSpeedFactor.LabelCap + ": " + statValue.ToStringPercent();
	}
}
