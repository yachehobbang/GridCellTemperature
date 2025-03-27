using Verse;

namespace RimWorld;

[DefOf]
public static class MapGeneratorDefOf
{
	public static MapGeneratorDef Encounter;

	public static MapGeneratorDef Base_Player;

	public static MapGeneratorDef Base_Faction;

	[MayRequireAnomaly]
	public static MapGeneratorDef Undercave;

	[MayRequireAnomaly]
	public static MapGeneratorDef MetalHell;

	[MayRequireAnomaly]
	public static MapGeneratorDef Labyrinth;

	static MapGeneratorDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(MapGeneratorDefOf));
	}
}
