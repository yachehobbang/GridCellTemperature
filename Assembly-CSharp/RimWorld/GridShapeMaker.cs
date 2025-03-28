using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public static class GridShapeMaker
{
	public static List<IntVec3> IrregularLump(IntVec3 center, Map map, int numCells, Predicate<IntVec3> validator = null)
	{
		HashSet<IntVec3> lumpCells = new HashSet<IntVec3>();
		for (int i = 0; i < numCells * 2; i++)
		{
			IntVec3 intVec = center + GenRadial.RadialPattern[i];
			if (intVec.InBounds(map) && (validator == null || validator(intVec)))
			{
				lumpCells.Add(intVec);
			}
		}
		List<IntVec3> list = new List<IntVec3>();
		while (lumpCells.Count > numCells)
		{
			int num = 99;
			foreach (IntVec3 item in lumpCells)
			{
				int num2 = CountNeighbours(item);
				if (num2 < num)
				{
					num = num2;
				}
			}
			list.Clear();
			foreach (IntVec3 item2 in lumpCells)
			{
				if (CountNeighbours(item2) == num)
				{
					list.Add(item2);
				}
			}
			lumpCells.Remove(list.RandomElement());
		}
		return lumpCells.ToList();
		int CountNeighbours(IntVec3 sq)
		{
			int num3 = 0;
			IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
			foreach (IntVec3 intVec2 in cardinalDirections)
			{
				if (lumpCells.Contains(sq + intVec2))
				{
					num3++;
				}
			}
			return num3;
		}
	}
}
