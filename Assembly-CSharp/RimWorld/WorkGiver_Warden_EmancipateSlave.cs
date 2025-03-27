using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_Warden_EmancipateSlave : WorkGiver_Warden
{
	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!ModLister.CheckIdeology("Slave imprisonment"))
		{
			return null;
		}
		Pawn pawn2 = t as Pawn;
		if (!ShouldTakeCareOfSlave(pawn, pawn2))
		{
			return null;
		}
		if (pawn2.guest.slaveInteractionMode != SlaveInteractionModeDefOf.Emancipate || pawn2.Downed || !pawn2.Awake())
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.SlaveEmancipation, pawn2);
		job.count = 1;
		return job;
	}
}
