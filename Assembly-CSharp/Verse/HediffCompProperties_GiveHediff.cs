namespace Verse;

public class HediffCompProperties_GiveHediff : HediffCompProperties
{
	public HediffDef hediffDef;

	public bool skipIfAlreadyExists;

	protected HediffCompProperties_GiveHediff()
	{
		compClass = typeof(HediffComp_GiveHediff);
	}
}
