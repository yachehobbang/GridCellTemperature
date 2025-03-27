using Verse;

namespace RimWorld;

[DefOf]
public static class GenStepDefOf
{
	public static GenStepDef PreciousLump;

	[MayRequireAnomaly]
	public static GenStepDef HarbingerTrees;

	static GenStepDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(GenStepDefOf));
	}
}
