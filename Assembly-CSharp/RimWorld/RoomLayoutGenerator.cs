using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class RoomLayoutGenerator
{
	private static readonly IntRange DefaultMaxMergedRoomsRange = new IntRange(1, 3);

	private static readonly List<CellRect> tmpRoomRects = new List<CellRect>();

	private static readonly List<List<CellRect>> tmpMergedRoomRects = new List<List<CellRect>>();

	private static readonly List<IntVec3> tmpCells = new List<IntVec3>();

	public static StructureLayout GenerateRandomLayout(CellRect container, int minRoomWidth = 6, int minRoomHeight = 6, float areaPrunePercent = 0.2f, IntRange? mergeRoomsRange = null, IntRange? removeRoomsRange = null, int entranceCount = 1)
	{
		RoomLayoutParams parms = default(RoomLayoutParams);
		parms.container = container;
		parms.areaPrunePercent = areaPrunePercent;
		parms.minRoomHeight = Mathf.Max(minRoomWidth, 3);
		parms.minRoomWidth = Mathf.Max(minRoomHeight, 3);
		parms.mergeRoomsRange = mergeRoomsRange ?? DefaultMaxMergedRoomsRange;
		parms.removeRoomsRange = removeRoomsRange ?? IntRange.zero;
		parms.entranceCount = entranceCount;
		return GenerateRoomLayout(parms);
	}

	private static StructureLayout GenerateRoomLayout(RoomLayoutParams parms)
	{
		StructureLayout structureLayout = new StructureLayout();
		structureLayout.Init(parms.container);
		SplitRandom(parms.container, tmpRoomRects, parms.minRoomWidth, parms.minRoomHeight);
		MergeRandom(tmpRoomRects, tmpMergedRoomRects, DefaultMaxMergedRoomsRange);
		foreach (List<CellRect> tmpMergedRoomRect in tmpMergedRoomRects)
		{
			structureLayout.AddRoom(tmpMergedRoomRect);
		}
		float num = (float)structureLayout.Area - parms.areaPrunePercent * (float)structureLayout.Area;
		int num2 = 100;
		while ((float)structureLayout.Area > num && num2-- > 0)
		{
			structureLayout.TryMinimizeLayoutWithoutDisconnection();
		}
		structureLayout.FinalizeRooms();
		CreateDoors(structureLayout, parms.entranceCount);
		return structureLayout;
	}

	private static void CreateDoors(StructureLayout layout, int entranceCount)
	{
		HashSet<int> hashSet = new HashSet<int>();
		tmpCells.Clear();
		tmpCells.AddRange(layout.container.Cells.InRandomOrder());
		for (int i = 0; i < tmpCells.Count; i++)
		{
			IntVec3 intVec = tmpCells[i];
			if (!layout.IsWallAt(intVec))
			{
				continue;
			}
			if (layout.IsGoodForHorizontalDoor(intVec))
			{
				int roomIdAt = layout.GetRoomIdAt(intVec + IntVec3.North);
				int roomIdAt2 = layout.GetRoomIdAt(intVec + IntVec3.South);
				bool flag = layout.IsOutside(intVec + IntVec3.North) || layout.IsOutside(intVec + IntVec3.South);
				int item = Gen.HashOrderless(roomIdAt, roomIdAt2);
				if (hashSet.Contains(item) || (flag && entranceCount <= 0))
				{
					continue;
				}
				layout.Add(intVec, RoomLayoutCellType.Door);
				hashSet.Add(item);
				if (flag)
				{
					entranceCount--;
				}
			}
			if (!layout.IsGoodForVerticalDoor(intVec))
			{
				continue;
			}
			int roomIdAt3 = layout.GetRoomIdAt(intVec + IntVec3.East);
			int roomIdAt4 = layout.GetRoomIdAt(intVec + IntVec3.West);
			bool flag2 = layout.IsOutside(intVec + IntVec3.East) || layout.IsOutside(intVec + IntVec3.West);
			int item2 = Gen.HashOrderless(roomIdAt3, roomIdAt4);
			if (!hashSet.Contains(item2) && (!flag2 || entranceCount > 0))
			{
				layout.Add(intVec, RoomLayoutCellType.Door);
				hashSet.Add(item2);
				if (flag2)
				{
					entranceCount--;
				}
			}
		}
		tmpCells.Clear();
	}

	private static void SplitRandom(CellRect rectToSplit, List<CellRect> rooms, int minWidth, int minHeight)
	{
		rooms.Clear();
		Queue<CellRect> queue = new Queue<CellRect>();
		queue.Enqueue(rectToSplit);
		while (queue.Count > 0)
		{
			CellRect cellRect = queue.Dequeue();
			if (!CanSplit(cellRect))
			{
				rooms.Add(cellRect);
			}
			else if (cellRect.Width > cellRect.Height)
			{
				int num = Rand.Range(minWidth, cellRect.Width - minWidth);
				CellRect item = new CellRect(cellRect.minX, cellRect.minZ, num, cellRect.Height);
				CellRect item2 = new CellRect(cellRect.minX + num, cellRect.minZ, cellRect.Width - num, cellRect.Height);
				queue.Enqueue(item);
				queue.Enqueue(item2);
			}
			else
			{
				int num2 = Rand.Range(minHeight, cellRect.Height - minHeight);
				CellRect item3 = new CellRect(cellRect.minX, cellRect.minZ + num2, cellRect.Width, cellRect.Height - num2);
				CellRect item4 = new CellRect(cellRect.minX, cellRect.minZ, cellRect.Width, num2);
				queue.Enqueue(item3);
				queue.Enqueue(item4);
			}
		}
		bool CanSplit(CellRect r)
		{
			if (r.Height <= 2 * minHeight)
			{
				return r.Width > 2 * minWidth;
			}
			return true;
		}
	}

	private static void MergeRandom(List<CellRect> rects, List<List<CellRect>> mergedRects, IntRange maxMergedRooms, int minAdjacenyScore = 5)
	{
		mergedRects.Clear();
		rects.Shuffle();
		for (int i = 0; i < rects.Count; i++)
		{
			CellRect cellRect = rects[i];
			if (UsedInMerge(mergedRects, cellRect))
			{
				continue;
			}
			List<CellRect> list = new List<CellRect> { cellRect };
			int num = Math.Max(1, maxMergedRooms.RandomInRange);
			for (int j = 0; j < rects.Count; j++)
			{
				if (list.Count >= num)
				{
					break;
				}
				CellRect cellRect2 = rects[j];
				if (!(cellRect == cellRect2) && !UsedInMerge(mergedRects, cellRect2) && cellRect.GetAdjacencyScore(cellRect2) >= minAdjacenyScore)
				{
					list.Add(cellRect2);
				}
			}
			mergedRects.Add(list);
		}
	}

	private static bool UsedInMerge(List<List<CellRect>> mergedRects, CellRect rect)
	{
		for (int i = 0; i < mergedRects.Count; i++)
		{
			for (int j = 0; j < mergedRects[i].Count; j++)
			{
				if (mergedRects[i][j] == rect)
				{
					return true;
				}
			}
		}
		return false;
	}
}
