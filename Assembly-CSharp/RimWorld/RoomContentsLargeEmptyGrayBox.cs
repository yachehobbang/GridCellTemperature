using Verse;

namespace RimWorld;

public class RoomContentsLargeEmptyGrayBox : RoomContentsLargeGrayBox
{
	protected override int MaxCrates => 5;

	protected override void SpawnBox(IntVec3 cell, Map map)
	{
		RoomContentsGrayBox.SpawnBoxInRoom(cell, map, null, addRewards: false);
	}
}
