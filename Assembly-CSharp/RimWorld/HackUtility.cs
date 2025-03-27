using Verse;

namespace RimWorld;

public static class HackUtility
{
	public static bool IsHackable(this Thing thing)
	{
		return thing.TryGetComp<CompHackable>() != null;
	}

	public static bool IsHacked(this Thing thing)
	{
		return thing.TryGetComp<CompHackable>()?.IsHacked ?? false;
	}

	public static bool IsCapableOfHacking(Pawn pawn)
	{
		if (pawn.IsMutant)
		{
			return false;
		}
		if (StatDefOf.HackingSpeed.Worker.IsDisabledFor(pawn))
		{
			return false;
		}
		if (!pawn.WorkTagIsDisabled(WorkTags.Intellectual))
		{
			return pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
		}
		return false;
	}
}
