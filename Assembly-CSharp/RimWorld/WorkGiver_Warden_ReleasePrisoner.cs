using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_Warden_ReleasePrisoner : WorkGiver_Warden
{
	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!ShouldTakeCareOfPrisoner(pawn, t))
		{
			return null;
		}
		Pawn pawn2 = (Pawn)t;
		if (pawn2.InMentalState)
		{
			JobFailReason.Is("PawnIsInMentalState".Translate(pawn2));
			return null;
		}
		if (pawn2.guest.IsInteractionEnabled(PrisonerInteractionModeDefOf.Release) && !pawn2.Downed && !pawn2.guest.Released)
		{
			if (!RCellFinder.TryFindPrisonerReleaseCell(pawn2, pawn, out var result))
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.ReleasePrisoner, pawn2, result);
			job.count = 1;
			return job;
		}
		return null;
	}
}
