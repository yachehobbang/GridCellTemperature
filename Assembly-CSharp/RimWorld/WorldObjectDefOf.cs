namespace RimWorld;

[DefOf]
public static class WorldObjectDefOf
{
	public static WorldObjectDef Caravan;

	public static WorldObjectDef Settlement;

	public static WorldObjectDef AbandonedSettlement;

	public static WorldObjectDef EscapeShip;

	public static WorldObjectDef Ambush;

	public static WorldObjectDef DestroyedSettlement;

	public static WorldObjectDef AttackedNonPlayerCaravan;

	public static WorldObjectDef TravelingTransportPods;

	public static WorldObjectDef RoutePlannerWaypoint;

	public static WorldObjectDef Site;

	public static WorldObjectDef PocketMap;

	[MayRequireRoyalty]
	public static WorldObjectDef TravelingShuttle;

	public static WorldObjectDef Debug_Arena;

	[MayRequireIdeology]
	public static WorldObjectDef Settlement_SecondArchonexusCycle;

	[MayRequireIdeology]
	public static WorldObjectDef Settlement_ThirdArchonexusCycle;

	[MayRequireIdeology]
	public static WorldObjectDef AbandonedArchotechStructures;

	static WorldObjectDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjectDefOf));
	}
}
