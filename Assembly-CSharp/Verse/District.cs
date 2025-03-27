using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Verse;

public sealed class District
{
	public sbyte mapIndex = -1;

	private Room roomInt;

	private List<Region> regions = new List<Region>();

	public int ID = -16161616;

	public int lastChangeTick = -1;

	private int numRegionsTouchingMapEdge;

	private int cachedOpenRoofCount = -1;

	private IEnumerator<IntVec3> cachedOpenRoofState;

	private int cachedCellCount = -1;

	public int newOrReusedRoomIndex = -1;

	private static int nextDistrictID;

	private HashSet<District> uniqueNeighborsSet = new HashSet<District>();

	private List<District> uniqueNeighbors = new List<District>();

	public Map Map
	{
		get
		{
			if (mapIndex >= 0)
			{
				return Find.Maps[mapIndex];
			}
			return null;
		}
	}

	public RegionType RegionType
	{
		get
		{
			if (!regions.Any())
			{
				return RegionType.None;
			}
			return regions[0].type;
		}
	}

	public List<Region> Regions => regions;

	public int RegionCount => regions.Count;

	public bool TouchesMapEdge => numRegionsTouchingMapEdge > 0;

	public bool Passable => RegionType.Passable();

	public bool IsDoorway
	{
		get
		{
			if (regions.Count == 1)
			{
				return regions[0].IsDoorway;
			}
			return false;
		}
	}

	public Room Room
	{
		get
		{
			return roomInt;
		}
		set
		{
			if (value != roomInt)
			{
				if (roomInt != null)
				{
					roomInt.RemoveDistrict(this);
				}
				roomInt = value;
				if (roomInt != null)
				{
					roomInt.AddDistrict(this);
				}
			}
		}
	}

	public int CellCount
	{
		get
		{
			if (cachedCellCount == -1)
			{
				cachedCellCount = 0;
				for (int i = 0; i < regions.Count; i++)
				{
					cachedCellCount += regions[i].CellCount;
				}
			}
			return cachedCellCount;
		}
	}

	public List<District> Neighbors
	{
		get
		{
			uniqueNeighborsSet.Clear();
			uniqueNeighbors.Clear();
			for (int i = 0; i < regions.Count; i++)
			{
				foreach (Region neighbor in regions[i].Neighbors)
				{
					if (uniqueNeighborsSet.Add(neighbor.District) && neighbor.District != this)
					{
						uniqueNeighbors.Add(neighbor.District);
					}
				}
			}
			uniqueNeighborsSet.Clear();
			return uniqueNeighbors;
		}
	}

	public IEnumerable<IntVec3> Cells
	{
		get
		{
			for (int i = 0; i < regions.Count; i++)
			{
				foreach (IntVec3 cell in regions[i].Cells)
				{
					yield return cell;
				}
			}
		}
	}

	public static District MakeNew(Map map)
	{
		District result = new District
		{
			mapIndex = (sbyte)map.Index,
			ID = nextDistrictID
		};
		nextDistrictID++;
		return result;
	}

	public void AddRegion(Region r)
	{
		if (regions.Contains(r))
		{
			Log.Error(string.Concat("Tried to add the same region twice to District. region=", r, ", district=", this));
			return;
		}
		regions.Add(r);
		if (r.touchesMapEdge)
		{
			numRegionsTouchingMapEdge++;
		}
		if (regions.Count == 1)
		{
			Map.regionGrid.allDistricts.Add(this);
		}
	}

	public void RemoveRegion(Region r)
	{
		if (!regions.Contains(r))
		{
			Log.Error(string.Concat("Tried to remove region from District but this region is not here. region=", r, ", district=", this));
			return;
		}
		regions.Remove(r);
		if (r.touchesMapEdge)
		{
			numRegionsTouchingMapEdge--;
		}
		if (regions.Count == 0)
		{
			Room = null;
			cachedOpenRoofCount = -1;
			cachedOpenRoofState = null;
			Map.regionGrid.allDistricts.Remove(this);
		}
	}

	public void Notify_MyMapRemoved()
	{
		mapIndex = -1;
	}

	public void Notify_RoofChanged()
	{
		cachedOpenRoofCount = -1;
		cachedOpenRoofState = null;
		Room.Notify_RoofChanged();
	}

	public void Notify_RoomShapeOrContainedBedsChanged()
	{
		cachedCellCount = -1;
		cachedOpenRoofCount = -1;
		cachedOpenRoofState = null;
		AnimalPenConnectedDistrictsCalculator.InvalidateDistrictCache(this);
		lastChangeTick = Find.TickManager.TicksGame;
		FacilitiesUtility.NotifyFacilitiesAboutChangedLOSBlockers(regions);
	}

	public void DecrementMapIndex()
	{
		if (mapIndex <= 0)
		{
			Log.Warning("Tried to decrement map index for district " + ID + ", but mapIndex=" + mapIndex);
		}
		else
		{
			mapIndex--;
		}
	}

	public int OpenRoofCountStopAt(int threshold)
	{
		if (cachedOpenRoofCount == -1 && cachedOpenRoofState == null)
		{
			cachedOpenRoofCount = 0;
			cachedOpenRoofState = Cells.GetEnumerator();
		}
		if (cachedOpenRoofCount < threshold && cachedOpenRoofState != null)
		{
			RoofGrid roofGrid = Map.roofGrid;
			while (cachedOpenRoofCount < threshold && cachedOpenRoofState.MoveNext())
			{
				if (!roofGrid.Roofed(cachedOpenRoofState.Current))
				{
					cachedOpenRoofCount++;
				}
			}
			if (cachedOpenRoofCount < threshold)
			{
				cachedOpenRoofState = null;
			}
		}
		return cachedOpenRoofCount;
	}

	internal void DebugDraw()
	{
		int hashCode = GetHashCode();
		foreach (IntVec3 cell in Cells)
		{
			CellRenderer.RenderCell(cell, (float)hashCode * 0.01f);
		}
	}

	internal string DebugString()
	{
		return string.Concat("District ID=", ID, "\n  first cell=", Cells.FirstOrDefault(), "\n  RegionCount=", RegionCount, "\n  RegionType=", RegionType, "\n  CellCount=", CellCount, "\n  OpenRoofCount=", OpenRoofCountStopAt(int.MaxValue), "\n  numRegionsTouchingMapEdge=", numRegionsTouchingMapEdge, "\n  lastChangeTick=", lastChangeTick, "\n  Room=", (Room != null) ? Room.ID.ToString() : "null");
	}

	public override string ToString()
	{
		return "District(districtID=" + ID + ", first=" + Cells.FirstOrDefault().ToString() + ", RegionsCount=" + RegionCount.ToString() + ", lastChangeTick=" + lastChangeTick + ")";
	}

	public override int GetHashCode()
	{
		return Gen.HashCombineInt(ID, 1538478890);
	}
}
