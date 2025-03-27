using RimWorld;

namespace Verse;

public class HediffComp_SeverityFromHemogen : HediffComp
{
	private Gene_Hemogen cachedHemogenGene;

	public HediffCompProperties_SeverityFromHemogen Props => (HediffCompProperties_SeverityFromHemogen)props;

	public override bool CompShouldRemove => base.Pawn.genes?.GetFirstGeneOfType<Gene_Hemogen>() == null;

	private Gene_Hemogen Hemogen
	{
		get
		{
			if (cachedHemogenGene == null)
			{
				cachedHemogenGene = base.Pawn.genes.GetFirstGeneOfType<Gene_Hemogen>();
			}
			return cachedHemogenGene;
		}
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (Hemogen != null)
		{
			severityAdjustment += ((Hemogen.Value > 0f) ? Props.severityPerHourHemogen : Props.severityPerHourEmpty) / 2500f;
		}
	}
}
