using System.Collections.Generic;
using Verse;

namespace RimWorld;

public interface ICreepJoinerWorker
{
	void OnCreated();

	void DoResponse(List<TargetInfo> looktargets, List<NamedArgument> namedArgs);
}
