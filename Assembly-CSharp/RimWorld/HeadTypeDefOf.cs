using Verse;

namespace RimWorld;

[DefOf]
public static class HeadTypeDefOf
{
	public static HeadTypeDef Skull;

	static HeadTypeDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(HeadTypeDefOf));
	}
}
