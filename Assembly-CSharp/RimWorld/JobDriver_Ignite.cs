using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobDriver_Ignite : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnBurningImmobile(TargetIndex.A);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			pawn.natives.TryStartIgnite(base.TargetThingA);
		};
		yield return toil;
	}
}
