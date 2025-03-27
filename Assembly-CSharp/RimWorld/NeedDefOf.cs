namespace RimWorld;

[DefOf]
public static class NeedDefOf
{
	public static NeedDef Food;

	public static NeedDef Rest;

	[MayRequireBiotech]
	public static NeedDef Learning;

	static NeedDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(NeedDefOf));
	}
}
