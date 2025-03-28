using Verse;

namespace RimWorld;

public class JobDriver_FillIn : JobDriver_RemoveBuilding
{
	protected override DesignationDef Designation => DesignationDefOf.FillIn;

	protected override float TotalNeededWork => 2500f;

	protected override EffecterDef WorkEffecter => EffecterDefOf.FillIn;

	protected override void FinishedRemoving()
	{
		(base.Target as PitBurrow)?.FillIn();
	}
}
