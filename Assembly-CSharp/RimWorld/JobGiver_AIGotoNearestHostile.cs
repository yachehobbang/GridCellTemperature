using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobGiver_AIGotoNearestHostile : ThinkNode_JobGiver
{
	private bool ignoreNonCombatants;

	private bool humanlikesOnly;

	private int overrideExpiryInterval = -1;

	private int overrideInstancedExpiryInterval = -1;

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		JobGiver_AIGotoNearestHostile obj = (JobGiver_AIGotoNearestHostile)base.DeepCopy(resolve);
		obj.ignoreNonCombatants = ignoreNonCombatants;
		obj.humanlikesOnly = humanlikesOnly;
		obj.overrideExpiryInterval = overrideExpiryInterval;
		obj.overrideInstancedExpiryInterval = overrideInstancedExpiryInterval;
		return obj;
	}

	protected override Job TryGiveJob(Pawn pawn)
	{
		float num = float.MaxValue;
		Thing thing = null;
		List<IAttackTarget> potentialTargetsFor = pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn);
		for (int i = 0; i < potentialTargetsFor.Count; i++)
		{
			IAttackTarget attackTarget = potentialTargetsFor[i];
			if (!attackTarget.ThreatDisabled(pawn) && AttackTargetFinder.IsAutoTargetable(attackTarget) && (!humanlikesOnly || !(attackTarget is Pawn pawn2) || pawn2.RaceProps.Humanlike) && (!(attackTarget.Thing is Pawn pawn3) || pawn3.IsCombatant() || (!ignoreNonCombatants && GenSight.LineOfSightToThing(pawn.Position, pawn3, pawn.Map))))
			{
				Thing thing2 = (Thing)attackTarget;
				int num2 = thing2.Position.DistanceToSquared(pawn.Position);
				if ((float)num2 < num && pawn.CanReach(thing2, PathEndMode.OnCell, Danger.Deadly))
				{
					num = num2;
					thing = thing2;
				}
			}
		}
		if (thing != null)
		{
			Job job = JobMaker.MakeJob(JobDefOf.Goto, thing);
			job.checkOverrideOnExpire = true;
			if (overrideInstancedExpiryInterval > 0)
			{
				job.instancedExpiryInterval = overrideInstancedExpiryInterval;
			}
			else
			{
				job.expiryInterval = ((overrideExpiryInterval > 0) ? overrideExpiryInterval : 500);
			}
			job.collideWithPawns = true;
			return job;
		}
		return null;
	}
}
