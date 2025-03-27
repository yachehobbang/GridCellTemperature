using RimWorld;

namespace Verse.AI;

public class MentalState_Jailbreaker : MentalState
{
	private const int NoPrisonerToFreeCheckInterval = 500;

	public override void MentalStateTick()
	{
		base.MentalStateTick();
		if (pawn.IsHashIntervalTick(500) && pawn.CurJobDef != JobDefOf.InducePrisonerToEscape && JailbreakerMentalStateUtility.FindPrisoner(pawn) == null)
		{
			RecoverFromState();
		}
	}

	public void Notify_InducedPrisonerToEscape()
	{
		MentalStateUtility.TryTransitionToWanderOwnRoom(this);
	}
}
