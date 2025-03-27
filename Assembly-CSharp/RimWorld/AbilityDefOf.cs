namespace RimWorld;

[DefOf]
public static class AbilityDefOf
{
	[MayRequireRoyalty]
	public static AbilityDef Speech;

	[MayRequireBiotech]
	public static AbilityDef ReimplantXenogerm;

	[MayRequireBiotech]
	public static AbilityDef ResurrectionMech;

	[MayRequireAnomaly]
	public static AbilityDef EntitySkip;

	[MayRequireAnomaly]
	public static AbilityDef UnnaturalCorpseSkip;

	[MayRequireAnomaly]
	public static AbilityDef ConsumeLeap_Devourer;

	static AbilityDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(AbilityDefOf));
	}
}
