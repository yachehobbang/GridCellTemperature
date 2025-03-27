namespace RimWorld;

[DefOf]
public static class LayoutRoomDefOf
{
	[MayRequireAnomaly]
	public static LayoutRoomDef LabyrinthObelisk;

	static LayoutRoomDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(LayoutRoomDefOf));
	}
}
