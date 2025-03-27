using Verse;
using Verse.AI;

namespace RimWorld;

public class LordToil_DefendAndExpandHive : LordToil_HiveRelated
{
	public float distToHiveToAttack = 10f;

	public override void UpdateAllDuties()
	{
		FilterOutUnspawnedHives();
		for (int i = 0; i < lord.ownedPawns.Count; i++)
		{
			Hive hiveFor = GetHiveFor(lord.ownedPawns[i]);
			PawnDuty duty = new PawnDuty(DutyDefOf.DefendAndExpandHive, hiveFor, distToHiveToAttack);
			lord.ownedPawns[i].mindState.duty = duty;
		}
	}

	public override void Notify_PawnAcquiredTarget(Pawn detector, Thing newTarg)
	{
		detector.TryGetComp<CompCanBeDormant>()?.WakeUp();
	}
}
