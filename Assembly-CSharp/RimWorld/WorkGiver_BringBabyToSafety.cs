using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_BringBabyToSafety : WorkGiver
{
	public override Job NonScanJob(Pawn pawn)
	{
		Pawn pawn2;
		if ((pawn2 = ChildcareUtility.FindUnsafeBaby(pawn, AutofeedMode.Childcare)) == null)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.BringBabyToSafetyUnforced, pawn2);
		job.count = 1;
		return job;
	}
}
