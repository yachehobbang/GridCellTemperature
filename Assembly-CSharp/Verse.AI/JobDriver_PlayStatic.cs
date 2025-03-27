using System.Collections.Generic;
using RimWorld;

namespace Verse.AI;

public class JobDriver_PlayStatic : JobDriver_BabyPlay
{
	private const int InteractionIntervalTicks = 1250;

	protected override StartingConditions StartingCondition => StartingConditions.GotoBaby;

	protected override IEnumerable<Toil> Play()
	{
		Toil toil = ToilMaker.MakeToil("Play");
		toil.WithEffect(EffecterDefOf.PlayStatic, TargetIndex.A, null);
		toil.handlingFacing = true;
		toil.tickAction = delegate
		{
			pawn.rotationTracker.FaceTarget(base.Baby);
			if (Find.TickManager.TicksGame % 1250 == 0)
			{
				pawn.interactions.TryInteractWith(base.Baby, InteractionDefOf.BabyPlay);
			}
			if (roomPlayGainFactor < 0f)
			{
				roomPlayGainFactor = BabyPlayUtility.GetRoomPlayGainFactors(base.Baby);
			}
			if (BabyPlayUtility.PlayTickCheckEnd(base.Baby, pawn, roomPlayGainFactor))
			{
				pawn.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Never;
		ChildcareUtility.MakeBabyPlayAsLongAsToilIsActive(toil, TargetIndex.A);
		yield return toil;
	}
}
