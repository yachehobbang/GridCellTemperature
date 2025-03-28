using Verse;
using Verse.AI.Group;

namespace RimWorld;

public class LordJob_ShamblerSwarm : LordJob_EntitySwarm
{
	public LordJob_ShamblerSwarm()
	{
	}

	public LordJob_ShamblerSwarm(IntVec3 startPos, IntVec3 destPos)
		: base(startPos, destPos)
	{
	}

	protected override LordToil CreateTravelingToil(IntVec3 start, IntVec3 dest)
	{
		return new LordToil_ShamblerSwarm(start, dest);
	}
}
