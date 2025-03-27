using Verse;

namespace RimWorld;

[DefOf]
public static class WeatherDefOf
{
	public static WeatherDef Clear;

	[MayRequireAnomaly]
	public static WeatherDef GrayPall;

	[MayRequireAnomaly]
	public static WeatherDef UnnaturalDarkness_Stage1;

	[MayRequireAnomaly]
	public static WeatherDef UnnaturalDarkness_Stage2;

	static WeatherDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(WeatherDefOf));
	}
}
