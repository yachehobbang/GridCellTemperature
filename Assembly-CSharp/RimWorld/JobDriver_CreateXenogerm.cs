using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobDriver_CreateXenogerm : JobDriver
{
	private const int JobEndInterval = 4000;

	private Building_GeneAssembler Xenogerminator => (Building_GeneAssembler)base.TargetThingA;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (pawn.Reserve(Xenogerminator, job, 1, -1, null, errorOnFailed))
		{
			return pawn.ReserveSittableOrSpot(Xenogerminator.InteractionCell, job, errorOnFailed);
		}
		return false;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		this.FailOn(() => !Xenogerminator.CanBeWorkedOnNow.Accepted);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.tickAction = delegate
		{
			float workAmount = pawn.GetStatValue(StatDefOf.ResearchSpeed) * Xenogerminator.GetStatValue(StatDefOf.AssemblySpeedFactor);
			Xenogerminator.DoWork(workAmount);
			pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f);
			pawn.GainComfortFromCellIfPossible(chairsOnly: true);
			if (Xenogerminator.ProgressPercent >= 1f)
			{
				Xenogerminator.Finish();
				pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
			}
		};
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
		toil.WithEffect(EffecterDefOf.GeneAssembler_Working, TargetIndex.A, null);
		toil.WithProgressBar(TargetIndex.A, () => Xenogerminator.ProgressPercent, interpolateBetweenActorAndTarget: false, 0f);
		toil.defaultCompleteMode = ToilCompleteMode.Never;
		toil.defaultDuration = 4000;
		toil.activeSkill = () => SkillDefOf.Intellectual;
		yield return toil;
	}
}
