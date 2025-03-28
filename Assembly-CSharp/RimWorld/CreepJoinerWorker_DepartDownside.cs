using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class CreepJoinerWorker_DepartDownside : BaseCreepJoinerDownsideWorker
{
	public override void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs)
	{
		base.Tracker.DoLeave();
	}
}
