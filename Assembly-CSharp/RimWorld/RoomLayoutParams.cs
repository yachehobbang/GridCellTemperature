using Verse;

namespace RimWorld;

public struct RoomLayoutParams
{
	public CellRect container;

	public int minRoomWidth;

	public int minRoomHeight;

	public float areaPrunePercent;

	public IntRange mergeRoomsRange;

	public int entranceCount;

	public IntRange removeRoomsRange;
}
