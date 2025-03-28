using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobDriver_Breastfeed : JobDriver_FeedBaby
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (base.TryMakePreToilReservations(errorOnFailed))
		{
			return pawn.Reserve(pawn, job, 1, -1, null, errorOnFailed);
		}
		return false;
	}

	public override void SetInitialPosture()
	{
	}

	public override bool CanBeginNowWhileLyingDown()
	{
		return pawn.Downed;
	}

	protected override IEnumerable<Toil> FeedBaby()
	{
		AddFailCondition(() => !ChildcareUtility.CanBreastfeed(pawn, out var _));
		yield return Breastfeed();
		yield return TuckMomInIfDowned();
	}

	private Toil Breastfeed()
	{
		Toil toil = ToilMaker.MakeToil("Breastfeed");
		toil.initAction = delegate
		{
			base.Baby.jobs.StartJob(ChildcareUtility.MakeBabySuckleJob(pawn), JobCondition.InterruptForced, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, null);
			initialFoodPercentage = base.Baby.needs.food.CurLevelPercentage;
		};
		toil.tickAction = delegate
		{
			bool num = ChildcareUtility.SuckleFromLactatingPawn(base.Baby, pawn);
			pawn.GainComfortFromCellIfPossible();
			if (!pawn.Downed && pawn.Rotation == Rot4.North)
			{
				pawn.Rotation = Rot4.East;
			}
			if (!num)
			{
				ReadyForNextToil();
			}
		};
		toil.AddFinishAction(delegate
		{
			if (base.Baby.needs.food.CurLevelPercentage - initialFoodPercentage > 0.6f)
			{
				base.Baby.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedMe, pawn);
				pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.BreastfedBaby, base.Baby);
			}
			if (base.Baby.CurJobDef == JobDefOf.BabySuckle)
			{
				base.Baby.jobs.EndCurrentJob(JobCondition.Succeeded);
			}
			if (pawn.Downed)
			{
				pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
			}
		});
		toil.AddFailCondition(() => !ChildcareUtility.CanBreastfeed(pawn, out var _));
		toil.handlingFacing = true;
		toil.WithProgressBar(TargetIndex.A, () => base.Baby.needs.food.CurLevelPercentage);
		toil.defaultCompleteMode = ToilCompleteMode.Never;
		toil.WithEffect(EffecterDefOf.Breastfeeding, TargetIndex.A, null);
		return toil;
	}

	private Toil TuckMomInIfDowned()
	{
		Toil toil = ToilMaker.MakeToil("TuckMomInIfDowned");
		toil.initAction = delegate
		{
			Building_Bed building_Bed;
			if (HealthAIUtility.ShouldSeekMedicalRest(pawn) && (building_Bed = pawn.CurrentBed()) != null && building_Bed == RestUtility.FindPatientBedFor(pawn))
			{
				pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.LayDown, building_Bed), JobCondition.Succeeded, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, JobTag.RestingForMedicalReasons, fromQueue: false, canReturnCurJobToPool: false, null);
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		return toil;
	}
}
