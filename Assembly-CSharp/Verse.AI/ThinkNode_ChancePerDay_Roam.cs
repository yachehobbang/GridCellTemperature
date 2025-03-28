namespace Verse.AI;

public class ThinkNode_ChancePerDay_Roam : ThinkNode_ChancePerHour
{
	protected override float MtbHours(Pawn pawn)
	{
		return (pawn.RaceProps.roamMtbDays ?? 0f) * 24f;
	}
}
