using Verse;

namespace RimWorld;

public class Thought_Situational_KillThirst : Thought_Situational
{
	private static readonly SimpleCurve MoodOffsetCurve = new SimpleCurve
	{
		new CurvePoint(0.301f, 0f),
		new CurvePoint(0.3f, -4f),
		new CurvePoint(0f, -18f)
	};

	public override float MoodOffset()
	{
		Need_KillThirst need_KillThirst = pawn.needs?.TryGetNeed<Need_KillThirst>();
		if (need_KillThirst == null)
		{
			return 0f;
		}
		return MoodOffsetCurve.Evaluate(need_KillThirst.CurLevel);
	}
}
