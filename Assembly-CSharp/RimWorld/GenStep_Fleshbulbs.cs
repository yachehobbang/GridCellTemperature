using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class GenStep_Fleshbulbs : GenStep
{
	private IntRange numBulbsRange = new IntRange(5, 10);

	private static readonly IntRange SpawnClusterSize = new IntRange(1, 3);

	private const int SpawnClusterRadius = 3;

	public override int SeedPart => 71423789;

	public override void Generate(Map map, GenStepParams parms)
	{
		int num = 0;
		int randomInRange = SpawnClusterSize.RandomInRange;
		CellFinder.TryFindRandomCell(map, (IntVec3 c) => c.Standable(map) && c.GetTerrain(map) == TerrainDefOf.Flesh, out var result);
		int num2 = 0;
		int randomInRange2 = numBulbsRange.RandomInRange;
		int num3 = 0;
		List<IntVec3> usedCells = new List<IntVec3>();
		while (num3 < randomInRange2 && num2 < 1000)
		{
			if (CellFinder.TryFindRandomCellNear(result, map, 3, ValidateCell, out var result2))
			{
				GenSpawn.Spawn(ThingDefOf.Fleshbulb, result2, map).SetFaction(Faction.OfEntities);
				num3++;
				num++;
				usedCells.Add(result2);
			}
			else
			{
				result2 = IntVec3.Invalid;
			}
			if (num >= randomInRange || !result2.IsValid)
			{
				num = 0;
				randomInRange = SpawnClusterSize.RandomInRange;
				CellFinder.TryFindRandomCell(map, (IntVec3 c) => c.Standable(map), out result);
			}
			num2++;
		}
		bool ValidateCell(IntVec3 c)
		{
			if (!c.InBounds(map))
			{
				return false;
			}
			if (!c.GetThingList(map).NullOrEmpty() || c.GetTerrain(map) != TerrainDefOf.Flesh)
			{
				return false;
			}
			IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
			foreach (IntVec3 intVec in cardinalDirections)
			{
				if (usedCells.Contains(c + intVec))
				{
					return false;
				}
			}
			return true;
		}
	}
}
