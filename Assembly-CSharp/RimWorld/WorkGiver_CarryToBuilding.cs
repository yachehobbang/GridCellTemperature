using Verse;
using Verse.AI;

namespace RimWorld;

public abstract class WorkGiver_CarryToBuilding : WorkGiver_Scanner
{
	public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
		{
			return false;
		}
		if (!(t is Building_Enterable { SelectedPawn: var selectedPawn } building_Enterable))
		{
			return false;
		}
		if (selectedPawn == null || selectedPawn.Map != pawn.Map || !building_Enterable.CanAcceptPawn(selectedPawn))
		{
			return false;
		}
		if (selectedPawn.IsPrisonerOfColony || selectedPawn.Downed || !selectedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) || (def.workType != null && selectedPawn.WorkTypeIsDisabled(def.workType)))
		{
			if (!selectedPawn.IsForbidden(pawn))
			{
				return pawn.CanReserve(selectedPawn, 1, -1, null, forced);
			}
			return false;
		}
		return false;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Building_Enterable building_Enterable))
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.CarryToBuilding, building_Enterable, building_Enterable.SelectedPawn);
		job.count = 1;
		return job;
	}
}
