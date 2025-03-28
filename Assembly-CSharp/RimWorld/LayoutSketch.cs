using Verse;

namespace RimWorld;

public class LayoutSketch : Sketch
{
	public StructureLayout layout;

	public ThingDef wall;

	public ThingDef wallStuff;

	public ThingDef door;

	public ThingDef doorStuff;

	public TerrainDef floor;

	protected virtual ThingDef GetWallStuff(int roomId)
	{
		return wallStuff;
	}

	protected virtual ThingDef GetDoorStuff(int roomId)
	{
		return doorStuff;
	}

	public LayoutSketch()
	{
		wall = ThingDefOf.Wall;
		door = ThingDefOf.Door;
		floor = TerrainDefOf.PavedTile;
	}

	public void FlushLayoutToSketch()
	{
		FlushLayoutToSketch(IntVec3.Zero);
	}

	public void FlushLayoutToSketch(IntVec3 at)
	{
		layout.offset = at;
		for (int i = layout.container.minX; i <= layout.container.maxX; i++)
		{
			for (int j = layout.container.minZ; j <= layout.container.maxZ; j++)
			{
				IntVec3 intVec = new IntVec3(i, 0, j);
				int roomIdAt = layout.GetRoomIdAt(intVec);
				if (layout.IsWallAt(intVec))
				{
					AddThing(wall, at + intVec, Rot4.North, GetWallStuff(roomIdAt), 1, null, null);
				}
				if (layout.IsFloorAt(intVec) || layout.IsDoorAt(intVec))
				{
					AddTerrain(floor, at + intVec);
				}
				if (layout.IsDoorAt(intVec))
				{
					AddThing(door, at + intVec, Rot4.North, GetDoorStuff(roomIdAt), 1, null, null);
				}
			}
		}
		foreach (LayoutRoom room in layout.Rooms)
		{
			for (int k = 0; k < room.rects.Count; k++)
			{
				room.rects[k] = room.rects[k].MovedBy(at);
			}
		}
	}
}
