using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld;

public class JobDriver_Hack : JobDriver
{
	private Thing HackTarget => base.TargetThingA;

	private CompHackable CompHacking => HackTarget.TryGetComp<CompHackable>();

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(HackTarget, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (!ModLister.CheckIdeology("Hack"))
		{
			yield break;
		}
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.handlingFacing = true;
		toil.tickAction = delegate
		{
			float statValue = pawn.GetStatValue(StatDefOf.HackingSpeed);
			CompHacking.Hack(statValue, pawn);
			pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f);
			pawn.rotationTracker.FaceTarget(HackTarget);
		};
		toil.WithEffect(EffecterDefOf.Hacking, TargetIndex.A, null);
		if (CompHacking.Props.effectHacking != null)
		{
			toil.WithEffect(() => CompHacking.Props.effectHacking, () => HackTarget.OccupiedRect().ClosestCellTo(pawn.Position), null);
		}
		toil.WithProgressBar(TargetIndex.A, () => CompHacking.ProgressPercent, interpolateBetweenActorAndTarget: false, -0.5f, alwaysShow: true);
		toil.PlaySoundAtStart(SoundDefOf.Hacking_Started);
		toil.PlaySustainerOrSound(SoundDefOf.Hacking_InProgress);
		toil.AddFinishAction(delegate
		{
			if (CompHacking.IsHacked)
			{
				SoundDefOf.Hacking_Completed.PlayOneShot(HackTarget);
				if (CompHacking.Props.hackingCompletedSound != null)
				{
					CompHacking.Props.hackingCompletedSound.PlayOneShot(HackTarget);
				}
			}
			else
			{
				SoundDefOf.Hacking_Suspended.PlayOneShot(HackTarget);
			}
		});
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
		toil.FailOn(() => CompHacking.IsHacked);
		toil.defaultCompleteMode = ToilCompleteMode.Never;
		toil.activeSkill = () => SkillDefOf.Intellectual;
		yield return toil;
	}
}
