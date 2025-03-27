namespace RimWorld;

[DefOf]
public static class LayoutDefOf
{
	[MayRequireIdeology]
	public static LayoutDef AncientComplex;

	[MayRequireIdeology]
	public static LayoutDef AncientComplex_Loot;

	[MayRequireBiotech]
	public static LayoutDef AncientComplex_Mechanitor_Loot;

	[MayRequireAnomaly]
	public static LayoutDef Labyrinth;

	static LayoutDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(LayoutDefOf));
	}
}
