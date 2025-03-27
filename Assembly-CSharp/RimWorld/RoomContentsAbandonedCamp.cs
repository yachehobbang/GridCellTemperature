using Verse;

namespace RimWorld;

public class RoomContentsAbandonedCamp : RoomContentsWorker
{
	private static readonly IntRange SurvivalPacksCountRange = new IntRange(0, 1);

	private static readonly IntRange TorchesCountRange = new IntRange(0, 1);

	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (room.TryGetRandomCellInRoom(map, out var cell, 3, delegate(IntVec3 c)
		{
			foreach (IntVec3 item in GenAdjFast.AdjacentCells8Way(c))
			{
				if (item.InBounds(map) && item.GetFirstThing<Thing>(map) != null)
				{
					return false;
				}
			}
			return true;
		}))
		{
			Thing thing = ThingMaker.MakeThing(ThingDefOf.Bedroll, ThingDefOf.Cloth);
			GenSpawn.Spawn(thing, cell, map, Rot4.Random);
			DropSpawnNear(ThingDefOf.MealSurvivalPack, thing.Position + IntVec3.East, map, SurvivalPacksCountRange.RandomInRange);
			DropSpawnNear(ThingDefOf.TorchLamp, thing.Position, map, TorchesCountRange.RandomInRange);
		}
	}

	private void DropSpawnNear(ThingDef thing, IntVec3 cell, Map map, int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			GenDrop.TryDropSpawn(ThingMaker.MakeThing(thing), cell, map, ThingPlaceMode.Near, out var _);
		}
	}
}
