using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobDriver_MechCharge : JobDriver
{
	private const TargetIndex ChargerInd = TargetIndex.A;

	public Building_MechCharger Charger => (Building_MechCharger)job.targetA.Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (!pawn.Reserve(Charger, job, 1, -1, null, errorOnFailed))
		{
			return false;
		}
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(TargetIndex.A);
		this.FailOn(() => !Charger.CanPawnChargeCurrently(pawn));
		yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.InteractionCell).FailOnForbidden(TargetIndex.A);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.defaultCompleteMode = ToilCompleteMode.Never;
		toil.initAction = delegate
		{
			Charger.StartCharging(pawn);
		};
		toil.AddFinishAction(delegate
		{
			Charger.StopCharging();
		});
		toil.handlingFacing = true;
		toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
		{
			pawn.rotationTracker.FaceTarget(Charger.Position);
			if (pawn.needs.energy.CurLevel >= JobGiver_GetEnergy.GetMaxRechargeLimit(pawn))
			{
				ReadyForNextToil();
			}
		});
		yield return toil;
	}
}
