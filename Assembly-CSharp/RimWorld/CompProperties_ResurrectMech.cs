using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class CompProperties_ResurrectMech : CompProperties_AbilityEffect
{
	public int maxCorpseAgeTicks = int.MaxValue;

	public List<MechChargeCosts> costs = new List<MechChargeCosts>();

	public EffecterDef appliedEffecterDef;

	public EffecterDef centerEffecterDef;

	public CompProperties_ResurrectMech()
	{
		compClass = typeof(CompAbilityEffect_ResurrectMech);
	}
}
