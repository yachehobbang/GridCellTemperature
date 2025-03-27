using RimWorld;
using UnityEngine;

namespace Verse;

public sealed class MapDrawer
{
	private readonly Map map;

	private Section[,] sections;

	private IntVec2 SectionCount
	{
		get
		{
			IntVec2 result = default(IntVec2);
			result.x = Mathf.CeilToInt((float)map.Size.x / 17f);
			result.z = Mathf.CeilToInt((float)map.Size.z / 17f);
			return result;
		}
	}

	private CellRect VisibleSections
	{
		get
		{
			CellRect viewRect = ViewRect;
			IntVec2 intVec = SectionCoordsAt(viewRect.Min);
			IntVec2 intVec2 = SectionCoordsAt(viewRect.Max);
			if (intVec2.x < intVec.x || intVec2.z < intVec.z)
			{
				return CellRect.Empty;
			}
			return CellRect.FromLimits(intVec.x, intVec.z, intVec2.x, intVec2.z);
		}
	}

	private CellRect ViewRect => Find.CameraDriver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(map);

	public MapDrawer(Map map)
	{
		this.map = map;
	}

	public void MapMeshDirty(IntVec3 loc, ulong dirtyFlags)
	{
		bool regenAdjacentCells = (dirtyFlags & ((ulong)MapMeshFlagDefOf.FogOfWar | (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.Roofs)) != 0;
		MapMeshDirty(loc, dirtyFlags, regenAdjacentCells, regenAdjacentSections: false);
	}

	public void MapMeshDirty(IntVec3 loc, ulong dirtyFlags, bool regenAdjacentCells, bool regenAdjacentSections)
	{
		if (Current.ProgramState != ProgramState.Playing || sections == null)
		{
			return;
		}
		SectionAt(loc).dirtyFlags |= dirtyFlags;
		if (regenAdjacentCells)
		{
			for (int i = 0; i < 8; i++)
			{
				IntVec3 intVec = loc + GenAdj.AdjacentCells[i];
				if (intVec.InBounds(map))
				{
					SectionAt(intVec).dirtyFlags |= dirtyFlags;
				}
			}
		}
		if (!regenAdjacentSections)
		{
			return;
		}
		IntVec2 intVec2 = SectionCoordsAt(loc);
		for (int j = 0; j < 8; j++)
		{
			IntVec3 intVec3 = GenAdj.AdjacentCells[j];
			IntVec2 intVec4 = intVec2 + new IntVec2(intVec3.x, intVec3.z);
			IntVec2 sectionCount = SectionCount;
			if (intVec4.x >= 0 && intVec4.z >= 0 && intVec4.x <= sectionCount.x - 1 && intVec4.z <= sectionCount.z - 1)
			{
				sections[intVec4.x, intVec4.z].dirtyFlags |= dirtyFlags;
			}
		}
	}

	public void MapMeshDrawerUpdate_First()
	{
		CellRect viewRect = ViewRect;
		bool flag = false;
		Section[,] array = sections;
		int upperBound = array.GetUpperBound(0);
		int upperBound2 = array.GetUpperBound(1);
		for (int i = array.GetLowerBound(0); i <= upperBound; i++)
		{
			for (int j = array.GetLowerBound(1); j <= upperBound2; j++)
			{
				if (array[i, j].TryUpdate(viewRect))
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			return;
		}
		for (int k = 0; k < SectionCount.x; k++)
		{
			for (int l = 0; l < SectionCount.z; l++)
			{
				if (sections[k, l].TryUpdate(viewRect))
				{
					return;
				}
			}
		}
	}

	public void DrawMapMesh()
	{
		CellRect viewRect = ViewRect;
		for (int i = 0; i < SectionCount.x; i++)
		{
			for (int j = 0; j < SectionCount.z; j++)
			{
				Section section = sections[i, j];
				if (viewRect.Overlaps(section.Bounds))
				{
					section.DrawSection();
				}
				else
				{
					section.DrawDynamicSections(viewRect);
				}
			}
		}
	}

	private IntVec2 SectionCoordsAt(IntVec3 loc)
	{
		return new IntVec2(Mathf.FloorToInt(loc.x / 17), Mathf.FloorToInt(loc.z / 17));
	}

	public Section SectionAt(IntVec3 loc)
	{
		IntVec2 intVec = SectionCoordsAt(loc);
		return sections[intVec.x, intVec.z];
	}

	public void RegenerateEverythingNow()
	{
		if (sections == null)
		{
			sections = new Section[SectionCount.x, SectionCount.z];
		}
		for (int i = 0; i < SectionCount.x; i++)
		{
			for (int j = 0; j < SectionCount.z; j++)
			{
				if (sections[i, j] == null)
				{
					sections[i, j] = new Section(new IntVec3(i, 0, j), map);
				}
				sections[i, j].RegenerateAllLayers();
			}
		}
	}

	public void WholeMapChanged(ulong change)
	{
		for (int i = 0; i < SectionCount.x; i++)
		{
			for (int j = 0; j < SectionCount.z; j++)
			{
				sections[i, j].dirtyFlags |= change;
			}
		}
	}
}
