using Verse;

namespace RimWorld;

public class StageFailTrigger_PawnAlive : StageFailTrigger
{
	[NoTranslate]
	public string pawnId;

	public override bool Failed(LordJob_Ritual ritual, TargetInfo spot, TargetInfo focus)
	{
		return !ritual.PawnWithRole(pawnId).Dead;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref pawnId, "pawnId");
	}
}
