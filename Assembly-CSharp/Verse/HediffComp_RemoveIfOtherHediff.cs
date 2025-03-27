namespace Verse;

public class HediffComp_RemoveIfOtherHediff : HediffComp_MessageBase
{
	private const int MtbRemovalCheckInterval = 1000;

	protected HediffCompProperties_RemoveIfOtherHediff Props => (HediffCompProperties_RemoveIfOtherHediff)props;

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (ShouldRemove())
		{
			Message();
			parent.pawn.health.RemoveHediff(parent);
		}
	}

	private bool ShouldRemove()
	{
		if (base.CompShouldRemove)
		{
			return true;
		}
		foreach (HediffDef hediff in Props.hediffs)
		{
			Hediff firstHediffOfDef = base.Pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
			if (firstHediffOfDef != null && (!Props.stages.HasValue || Props.stages.Value.Includes(firstHediffOfDef.CurStageIndex)) && (Props.mtbHours <= 0 || (base.Pawn.IsHashIntervalTick(1000) && Rand.MTBEventOccurs(Props.mtbHours, 2500f, 1000f))))
			{
				return true;
			}
		}
		return false;
	}
}
