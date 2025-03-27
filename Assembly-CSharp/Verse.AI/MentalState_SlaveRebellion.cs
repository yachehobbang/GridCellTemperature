using RimWorld;

namespace Verse.AI;

public class MentalState_SlaveRebellion : MentalState
{
	private const int NoSlaveToRebelWithCheckInterval = 500;

	public override void MentalStateTick()
	{
		base.MentalStateTick();
		if (pawn.IsHashIntervalTick(500) && pawn.CurJobDef != JobDefOf.InduceSlaveToRebel && SlaveRebellionUtility.FindSlaveForRebellion(pawn) == null)
		{
			RecoverFromState();
		}
	}
}
