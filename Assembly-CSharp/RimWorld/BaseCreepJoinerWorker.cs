using System.Collections.Generic;
using Verse;

namespace RimWorld;

public abstract class BaseCreepJoinerWorker : ICreepJoinerWorker
{
	public Pawn_CreepJoinerTracker Tracker { get; set; }

	public virtual bool CanOccurOnDeath => false;

	public Pawn Pawn => Tracker.Pawn;

	public virtual void OnCreated()
	{
	}

	public abstract void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs);
}
