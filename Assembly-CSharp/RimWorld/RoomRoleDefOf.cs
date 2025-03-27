using Verse;

namespace RimWorld;

[DefOf]
public static class RoomRoleDefOf
{
	public static RoomRoleDef None;

	public static RoomRoleDef Bedroom;

	public static RoomRoleDef Barracks;

	public static RoomRoleDef PrisonCell;

	public static RoomRoleDef PrisonBarracks;

	public static RoomRoleDef Hospital;

	[MayRequireRoyalty]
	public static RoomRoleDef ThroneRoom;

	[MayRequireIdeology]
	public static RoomRoleDef WorshipRoom;

	static RoomRoleDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(RoomRoleDefOf));
	}
}
