using Verse;

namespace RimWorld;

public class StatPart_SightPsychicSensitivityOffset : StatPart
{
	public float startsAt = 0.5f;

	public float minBonus;

	public float maxBonus = 0.5f;

	public float endsAt;

	public override void TransformValue(StatRequest req, ref float val)
	{
		if (TryGetPsychicOffset(req.Thing, out var offset))
		{
			val += offset;
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		if (req.HasThing && TryGetPsychicOffset(req.Thing, out var offset))
		{
			return "StatsReport_SightPsychicSensitivityOffset".Translate() + (": +" + offset.ToStringPercent());
		}
		return null;
	}

	private bool TryGetPsychicOffset(Thing t, out float offset)
	{
		if (t != null && t is Pawn pawn)
		{
			float num = PawnCapacityUtility.CalculateTagEfficiency(pawn.health.hediffSet, BodyPartTagDefOf.SightSource, float.MaxValue, default(FloatRange), null, PawnCapacityWorker_Sight.PartEfficiencySpecialWeight);
			if (num <= startsAt)
			{
				offset = GenMath.LerpDoubleClamped(startsAt, endsAt, minBonus, maxBonus, num);
				return offset >= 0.01f;
			}
		}
		offset = 0f;
		return false;
	}
}
