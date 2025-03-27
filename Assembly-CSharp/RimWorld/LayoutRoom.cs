using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class LayoutRoom : IExposable
{
	public List<CellRect> rects;

	public List<LayoutRoomDef> defs;

	public List<IntVec3> entryCells;

	public LayoutRoomDef requiredDef;

	public int id;

	public readonly List<LayoutRoom> connections = new List<LayoutRoom>();

	private static readonly HashSet<IntVec3> cells = new HashSet<IntVec3>();

	public int Area => rects.Sum((CellRect r) => r.Area);

	public IEnumerable<IntVec3> Corners => rects.SelectMany((CellRect r) => r.Corners);

	public IEnumerable<IntVec3> Cells
	{
		get
		{
			for (int i = 0; i < rects.Count; i++)
			{
				foreach (IntVec3 cell in rects[i].Cells)
				{
					yield return cell;
				}
			}
		}
	}

	public LayoutRoom()
	{
	}

	public LayoutRoom(List<CellRect> rects, LayoutRoomDef requiredDef = null)
	{
		this.rects = rects;
	}

	public bool TryGetRectOfSize(int minWidth, int minHeight, out CellRect rect)
	{
		foreach (CellRect rect2 in rects)
		{
			if (rect2.AreSidesEqualOrGreater(minWidth, minHeight))
			{
				rect = rect2;
				return true;
			}
		}
		rect = default(CellRect);
		return false;
	}

	public bool IsCorner(IntVec3 position)
	{
		for (int i = 0; i < rects.Count; i++)
		{
			if (rects[i].IsCorner(position))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasLayoutDef(LayoutRoomDef def)
	{
		return defs.Contains(def);
	}

	public bool IsAdjacentTo(LayoutRoom room, int minAdjacencyScore = 1)
	{
		foreach (CellRect rect in rects)
		{
			foreach (CellRect rect2 in room.rects)
			{
				if (rect.GetAdjacencyScore(rect2) >= minAdjacencyScore)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool Contains(IntVec3 position, int contractedBy = 1)
	{
		foreach (CellRect rect in rects)
		{
			if (rect.ContractedBy(contractedBy).Contains(position))
			{
				return true;
			}
		}
		return false;
	}

	public void SpawnRectTriggersForAction(SignalAction action, Map map)
	{
		foreach (CellRect rect in rects)
		{
			RectTrigger obj = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger);
			obj.signalTag = action.signalTag;
			obj.Rect = rect;
			GenSpawn.Spawn(obj, rect.CenterCell, map);
		}
	}

	public bool TryGetRandomCellInRoom(Map map, out IntVec3 cell, int contractedBy = 1, Func<IntVec3, bool> validator = null)
	{
		for (int i = 0; i < rects.Count; i++)
		{
			foreach (IntVec3 cell2 in rects[i].ContractedBy(contractedBy).Cells)
			{
				if (cell2.GetFirstBuilding(map) == null && (validator == null || validator(cell2)))
				{
					cells.Add(cell2);
				}
			}
		}
		if (cells.Count == 0)
		{
			cell = IntVec3.Invalid;
			return false;
		}
		cell = cells.RandomElement();
		cells.Clear();
		return true;
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref requiredDef, "requiredDef");
		Scribe_Collections.Look(ref defs, "defs", LookMode.Def);
		Scribe_Collections.Look(ref rects, "rects", LookMode.Value);
	}
}
