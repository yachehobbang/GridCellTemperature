namespace Verse;

public class HediffComp_GiveNeurocharge : HediffComp
{
	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		parent.pawn.health.lastReceivedNeuralSuperchargeTick = Find.TickManager.TicksGame;
	}
}
