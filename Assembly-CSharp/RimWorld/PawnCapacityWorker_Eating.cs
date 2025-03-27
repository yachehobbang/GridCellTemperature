using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class PawnCapacityWorker_Eating : PawnCapacityWorker
{
	public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
	{
		return PawnCapacityUtility.CalculateTagEfficiency(diffSet, BodyPartTagDefOf.EatingSource, float.MaxValue, default(FloatRange), impactors) * PawnCapacityUtility.CalculateTagEfficiency(diffSet, BodyPartTagDefOf.EatingPathway, 1f, default(FloatRange), impactors) * PawnCapacityUtility.CalculateTagEfficiency(diffSet, BodyPartTagDefOf.Tongue, float.MaxValue, new FloatRange(0.5f, 1f), impactors) * CalculateCapacityAndRecord(diffSet, PawnCapacityDefOf.Consciousness, impactors);
	}

	public override bool CanHaveCapacity(BodyDef body)
	{
		return body.HasPartWithTag(BodyPartTagDefOf.EatingSource);
	}
}
