using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace RimWorld;

public class RitualCancellationTrigger_CannotScarify : RitualCancellationTrigger
{
	[NoTranslate]
	public string roleId;

	public override IEnumerable<Trigger> CancellationTriggers(RitualRoleAssignments assignments)
	{
		yield return new Trigger_Custom((TriggerSignal signal) => (signal.type == TriggerSignalType.Tick && GenTicks.TicksGame % 60 == 0 && !JobDriver_Scarify.AvailableOnNow(assignments.FirstAssignedPawn(roleId))) ? true : false);
	}
}
