using Verse;

namespace RimWorld;

public struct PreceptThingChance
{
	public ThingDef def;

	public float chance;

	public static implicit operator PreceptThingChance(PreceptThingChanceClass c)
	{
		PreceptThingChance result = default(PreceptThingChance);
		result.chance = c.chance;
		result.def = c.def;
		return result;
	}
}
