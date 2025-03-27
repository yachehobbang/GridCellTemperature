using Verse;

namespace RimWorld;

public abstract class RoomContentsWorker
{
	public LayoutRoomDef RoomDef { get; private set; }

	public void Initialize(LayoutRoomDef roomDef)
	{
		RoomDef = roomDef;
	}

	public abstract void FillRoom(Map map, LayoutRoom room);
}
