namespace RimWorld;

[DefOf]
public static class BiomeDefOf
{
	public static BiomeDef IceSheet;

	public static BiomeDef Tundra;

	public static BiomeDef BorealForest;

	public static BiomeDef TemperateForest;

	public static BiomeDef Desert;

	public static BiomeDef SeaIce;

	public static BiomeDef Ocean;

	public static BiomeDef Lake;

	[MayRequireAnomaly]
	public static BiomeDef Undercave;

	static BiomeDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(BiomeDefOf));
	}
}
