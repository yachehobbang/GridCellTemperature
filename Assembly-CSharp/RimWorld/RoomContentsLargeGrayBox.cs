using Verse;

namespace RimWorld;

public class RoomContentsLargeGrayBox : RoomContentsGrayBox
{
	private const int NormalCrates = 1;

	private const float ChanceEmpty = 0.5f;

	private int placedCrates;

	protected virtual int MaxCrates { get; } = 20;

	public override void FillRoom(Map map, LayoutRoom room)
	{
		for (int i = 0; i < MaxCrates; i++)
		{
			if (room.TryGetRandomCellInRoom(map, out var cell, 1, (IntVec3 c) => IsValidCell(c, map)))
			{
				SpawnBox(cell, map);
			}
		}
	}

	protected virtual void SpawnBox(IntVec3 cell, Map map)
	{
		ThingSetMakerDef rewardMaker = ThingSetMakerDefOf.Reward_GrayBoxLowReward;
		bool addRewards = true;
		if (placedCrates < 1)
		{
			rewardMaker = ThingSetMakerDefOf.Reward_GrayBox;
		}
		else if (Rand.Chance(0.5f))
		{
			addRewards = false;
		}
		placedCrates++;
		RoomContentsGrayBox.SpawnBoxInRoom(cell, map, rewardMaker, addRewards);
	}

	private bool IsValidCell(IntVec3 cell, Map map)
	{
		if (cell.GetFirstBuilding(map) != null)
		{
			return false;
		}
		foreach (IntVec3 item in GenAdjFast.AdjacentCells8Way(cell))
		{
			if (item.GetFirstBuilding(map) != null)
			{
				return false;
			}
		}
		return true;
	}
}
