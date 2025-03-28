namespace Verse;

public class HediffComp_GiveHediff : HediffComp
{
	private HediffCompProperties_GiveHediff Props => (HediffCompProperties_GiveHediff)props;

	public override void CompPostPostAdd(DamageInfo? dinfo)
	{
		if (!Props.skipIfAlreadyExists || !parent.pawn.health.hediffSet.HasHediff(Props.hediffDef))
		{
			parent.pawn.health.AddHediff(Props.hediffDef, null, null);
		}
	}
}
