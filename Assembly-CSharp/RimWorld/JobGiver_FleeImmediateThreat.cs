using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobGiver_FleeImmediateThreat : ThinkNode_JobGiver
{
	protected FloatRange fleeDistRange = new FloatRange(50f, 75f);

	protected int distToFireToFlee = 4;

	protected int distToDangerToFlee = 40;

	protected override Job TryGiveJob(Pawn pawn)
	{
		Job job = FleeUtility.FleeLargeFireJob(pawn, 1, distToFireToFlee, Mathf.CeilToInt(fleeDistRange.RandomInRange));
		if (job != null)
		{
			return job;
		}
		if (!GenAI.EnemyIsNear(pawn, distToDangerToFlee, out var threat))
		{
			return null;
		}
		return FleeUtility.FleeJob(pawn, threat, Mathf.CeilToInt(fleeDistRange.RandomInRange));
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_FleeImmediateThreat obj = (JobGiver_FleeImmediateThreat)base.DeepCopy(resolve);
		obj.fleeDistRange = fleeDistRange;
		obj.distToFireToFlee = distToFireToFlee;
		obj.distToDangerToFlee = distToDangerToFlee;
		return obj;
	}
}
