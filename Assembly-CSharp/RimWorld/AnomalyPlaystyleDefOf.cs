namespace RimWorld;

[DefOf]
public static class AnomalyPlaystyleDefOf
{
	[MayRequireAnomaly]
	public static AnomalyPlaystyleDef Standard;

	static AnomalyPlaystyleDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(AnomalyPlaystyleDefOf));
	}
}
