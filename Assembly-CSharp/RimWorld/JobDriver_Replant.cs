using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobDriver_Replant : JobDriver_HaulToContainer
{
	protected override int Duration => (int)(((MinifiedTree)base.ThingToCarry).InnerTree.def.plant.harvestWork / JobDriver_PlantWork.WorkDonePerTick(pawn, ((MinifiedTree)base.ThingToCarry).InnerTree));

	protected override EffecterDef WorkEffecter => EffecterDefOf.Sow;

	protected override SoundDef WorkSustainer => SoundDefOf.Interact_ConstructDirt;

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOn(() => job.plantDefToSow.plant.interferesWithRoof && base.Container.Position.Roofed(base.Map));
		foreach (Toil item in base.MakeNewToils())
		{
			yield return item;
		}
	}

	protected override void ModifyPrepareToil(Toil toil)
	{
		toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
		{
			if (pawn.skills != null)
			{
				pawn.skills.Learn(SkillDefOf.Plants, 0.085f);
			}
		});
	}
}
