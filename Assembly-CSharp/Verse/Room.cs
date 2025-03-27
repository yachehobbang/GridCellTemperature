using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;

namespace Verse;

public class Room
{
	public int ID = -1;

	private List<District> districts = new List<District>();

	private RoomTempTracker tempTracker;

	private int cachedOpenRoofCount = -1;

	private int cachedCellCount = -1;

	private bool isPrisonCell;

	private bool statsAndRoleDirty = true;

	private DefMap<RoomStatDef, float> stats = new DefMap<RoomStatDef, float>();

	private RoomRoleDef role;

	private static int nextRoomID;

	private const int RegionCountHuge = 60;

	private const float UseOutdoorTemperatureUnroofedFraction = 0.25f;

	private const int MaxRegionsToAssignRoomRole = 36;

	private static readonly Color PrisonFieldColor = new Color(1f, 0.7f, 0.2f);

	private static readonly Color NonPrisonFieldColor = new Color(0.3f, 0.3f, 1f);

	private List<Region> tmpRegions = new List<Region>();

	private HashSet<Thing> uniqueContainedThingsSet = new HashSet<Thing>();

	private List<Thing> uniqueContainedThings = new List<Thing>();

	private readonly HashSet<Thing> uniqueContainedThingsOfDef = new HashSet<Thing>();

	private static List<IntVec3> fields = new List<IntVec3>();

	public List<District> Districts => districts;

	public Map Map
	{
		get
		{
			if (!districts.Any())
			{
				return null;
			}
			return districts[0].Map;
		}
	}

	public int DistrictCount => districts.Count;

	public RoomTempTracker TempTracker => tempTracker;

	public float Temperature
	{
		get
		{
			return tempTracker.Temperature;
		}
		set
		{
			tempTracker.Temperature = value;
		}
	}

	public bool UsesOutdoorTemperature
	{
		get
		{
			if (!TouchesMapEdge)
			{
				return OpenRoofCount >= Mathf.CeilToInt((float)CellCount * 0.25f);
			}
			return true;
		}
	}

	public bool Dereferenced => RegionCount == 0;

	public bool IsHuge => RegionCount > 60;

	public bool IsPrisonCell => isPrisonCell;

	public IEnumerable<IntVec3> Cells
	{
		get
		{
			for (int i = 0; i < districts.Count; i++)
			{
				foreach (IntVec3 cell in districts[i].Cells)
				{
					yield return cell;
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
				for (int i = 0; i < districts.Count; i++)
				{
					cachedCellCount += districts[i].CellCount;
				}
			}
			return cachedCellCount;
		}
	}

	public Region FirstRegion
	{
		get
		{
			for (int i = 0; i < districts.Count; i++)
			{
				List<Region> regions = districts[i].Regions;
				if (regions.Count > 0)
				{
					return regions[0];
				}
			}
			return null;
		}
	}

	public List<Region> Regions
	{
		get
		{
			tmpRegions.Clear();
			for (int i = 0; i < districts.Count; i++)
			{
				List<Region> regions = districts[i].Regions;
				for (int j = 0; j < regions.Count; j++)
				{
					tmpRegions.Add(regions[j]);
				}
			}
			return tmpRegions;
		}
	}

	public int RegionCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < districts.Count; i++)
			{
				num += districts[i].RegionCount;
			}
			return num;
		}
	}

	public CellRect ExtentsClose
	{
		get
		{
			CellRect result = new CellRect(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
			foreach (Region region in Regions)
			{
				if (region.extentsClose.minX < result.minX)
				{
					result.minX = region.extentsClose.minX;
				}
				if (region.extentsClose.minZ < result.minZ)
				{
					result.minZ = region.extentsClose.minZ;
				}
				if (region.extentsClose.maxX > result.maxX)
				{
					result.maxX = region.extentsClose.maxX;
				}
				if (region.extentsClose.maxZ > result.maxZ)
				{
					result.maxZ = region.extentsClose.maxZ;
				}
			}
			return result;
		}
	}

	public int OpenRoofCount
	{
		get
		{
			if (cachedOpenRoofCount == -1)
			{
				cachedOpenRoofCount = OpenRoofCountStopAt(int.MaxValue);
			}
			return cachedOpenRoofCount;
		}
	}

	public IEnumerable<IntVec3> BorderCells
	{
		get
		{
			foreach (IntVec3 c in Cells)
			{
				int i = 0;
				while (i < 8)
				{
					IntVec3 intVec = c + GenAdj.AdjacentCells[i];
					Region region = (c + GenAdj.AdjacentCells[i]).GetRegion(Map);
					if (region == null || region.Room != this)
					{
						yield return intVec;
					}
					int num = i + 1;
					i = num;
				}
			}
		}
	}

	public bool TouchesMapEdge
	{
		get
		{
			for (int i = 0; i < districts.Count; i++)
			{
				if (districts[i].TouchesMapEdge)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool PsychologicallyOutdoors
	{
		get
		{
			if (OpenRoofCountStopAt(300) >= 300)
			{
				return true;
			}
			if (TouchesMapEdge && (float)OpenRoofCount / (float)CellCount >= 0.5f)
			{
				return true;
			}
			return false;
		}
	}

	public bool OutdoorsForWork
	{
		get
		{
			if (OpenRoofCountStopAt(101) > 100 || (float)OpenRoofCount > (float)CellCount * 0.25f)
			{
				return true;
			}
			return false;
		}
	}

	public IEnumerable<Pawn> Owners
	{
		get
		{
			if (TouchesMapEdge || IsHuge || (Role != RoomRoleDefOf.Bedroom && Role != RoomRoleDefOf.PrisonCell && Role != RoomRoleDefOf.Barracks && Role != RoomRoleDefOf.PrisonBarracks))
			{
				yield break;
			}
			IEnumerable<Building_Bed> enumerable = ContainedBeds.Where((Building_Bed x) => x.def.building.bed_humanlike);
			if (enumerable.Count() > 1 && (Role == RoomRoleDefOf.Barracks || Role == RoomRoleDefOf.PrisonBarracks) && enumerable.Where((Building_Bed b) => b.OwnersForReading.Any()).Count() > 1)
			{
				yield break;
			}
			foreach (Building_Bed item in enumerable)
			{
				List<Pawn> assignedPawns = item.OwnersForReading;
				for (int i = 0; i < assignedPawns.Count; i++)
				{
					yield return assignedPawns[i];
				}
			}
		}
	}

	public IEnumerable<Building_Bed> ContainedBeds
	{
		get
		{
			List<Thing> things = ContainedAndAdjacentThings;
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i] is Building_Bed building_Bed)
				{
					yield return building_Bed;
				}
			}
		}
	}

	public bool Fogged
	{
		get
		{
			if (RegionCount == 0)
			{
				return false;
			}
			return FirstRegion.AnyCell.Fogged(Map);
		}
	}

	public bool IsDoorway
	{
		get
		{
			if (districts.Count == 1)
			{
				return districts[0].IsDoorway;
			}
			return false;
		}
	}

	public List<Thing> ContainedAndAdjacentThings
	{
		get
		{
			uniqueContainedThingsSet.Clear();
			uniqueContainedThings.Clear();
			List<Region> regions = Regions;
			for (int i = 0; i < regions.Count; i++)
			{
				List<Thing> allThings = regions[i].ListerThings.AllThings;
				if (allThings == null)
				{
					continue;
				}
				for (int j = 0; j < allThings.Count; j++)
				{
					Thing item = allThings[j];
					if (uniqueContainedThingsSet.Add(item))
					{
						uniqueContainedThings.Add(item);
					}
				}
			}
			uniqueContainedThingsSet.Clear();
			return uniqueContainedThings;
		}
	}

	public RoomRoleDef Role
	{
		get
		{
			if (statsAndRoleDirty)
			{
				UpdateRoomStatsAndRole();
			}
			return role;
		}
	}

	public bool AnyPassable
	{
		get
		{
			for (int i = 0; i < districts.Count; i++)
			{
				if (districts[i].Passable)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool ProperRoom
	{
		get
		{
			if (TouchesMapEdge)
			{
				return false;
			}
			for (int i = 0; i < districts.Count; i++)
			{
				if (districts[i].RegionType == RegionType.Normal)
				{
					return true;
				}
			}
			return false;
		}
	}

	private int OpenRoofCountStopAt(int threshold)
	{
		if (cachedOpenRoofCount != -1)
		{
			return cachedOpenRoofCount;
		}
		int num = 0;
		for (int i = 0; i < districts.Count; i++)
		{
			num += districts[i].OpenRoofCountStopAt(threshold);
			if (num >= threshold)
			{
				return num;
			}
			threshold -= num;
		}
		return num;
	}

	public static Room MakeNew(Map map)
	{
		Room obj = new Room
		{
			ID = nextRoomID
		};
		obj.tempTracker = new RoomTempTracker(obj, map);
		nextRoomID++;
		return obj;
	}

	public void AddDistrict(District district)
	{
		if (districts.Contains(district))
		{
			Log.Error(string.Concat("Tried to add the same district twice to Room. district=", district, ", room=", this));
		}
		else
		{
			districts.Add(district);
			if (districts.Count == 1)
			{
				Map.regionGrid.allRooms.Add(this);
			}
		}
	}

	public void RemoveDistrict(District district)
	{
		if (!districts.Contains(district))
		{
			Log.Error(string.Concat("Tried to remove district from Room but this district is not here. district=", district, ", room=", this));
			return;
		}
		Map map = Map;
		districts.Remove(district);
		if (districts.Count == 0)
		{
			map.regionGrid.allRooms.Remove(this);
		}
		statsAndRoleDirty = true;
	}

	public bool PushHeat(float energy)
	{
		if (UsesOutdoorTemperature)
		{
			return false;
		}
		Temperature += energy / (float)CellCount;
		return true;
	}

	public void Notify_ContainedThingSpawnedOrDespawned(Thing th)
	{
		if (th.def.category == ThingCategory.Mote || th.def.category == ThingCategory.Projectile || th.def.category == ThingCategory.Ethereal || th.def.category == ThingCategory.Pawn)
		{
			return;
		}
		if (IsDoorway)
		{
			List<Region> regions = districts[0].Regions;
			for (int i = 0; i < regions[0].links.Count; i++)
			{
				Region otherRegion = regions[0].links[i].GetOtherRegion(regions[0]);
				if (otherRegion != null && !otherRegion.IsDoorway)
				{
					otherRegion.Room.Notify_ContainedThingSpawnedOrDespawned(th);
				}
			}
		}
		statsAndRoleDirty = true;
	}

	public void Notify_TerrainChanged()
	{
		statsAndRoleDirty = true;
	}

	public void Notify_BedTypeChanged()
	{
		statsAndRoleDirty = true;
	}

	public void Notify_RoofChanged()
	{
		cachedOpenRoofCount = -1;
		tempTracker.RoofChanged();
	}

	public void Notify_RoomShapeChanged()
	{
		cachedCellCount = -1;
		cachedOpenRoofCount = -1;
		if (Dereferenced)
		{
			isPrisonCell = false;
			statsAndRoleDirty = true;
			return;
		}
		tempTracker.RoomChanged();
		if (Current.ProgramState == ProgramState.Playing && !Fogged)
		{
			Map.autoBuildRoofAreaSetter.TryGenerateAreaFor(this);
		}
		isPrisonCell = false;
		if (Building_Bed.RoomCanBePrisonCell(this))
		{
			List<Thing> containedAndAdjacentThings = ContainedAndAdjacentThings;
			for (int i = 0; i < containedAndAdjacentThings.Count; i++)
			{
				if (containedAndAdjacentThings[i] is Building_Bed { ForPrisoners: not false })
				{
					isPrisonCell = true;
					break;
				}
			}
		}
		List<Thing> list = Map.listerThings.ThingsOfDef(ThingDefOf.NutrientPasteDispenser);
		for (int j = 0; j < list.Count; j++)
		{
			list[j].Notify_ColorChanged();
		}
		if (Current.ProgramState == ProgramState.Playing && isPrisonCell)
		{
			foreach (Building_Bed containedBed in ContainedBeds)
			{
				containedBed.ForPrisoners = true;
			}
		}
		statsAndRoleDirty = true;
	}

	public bool ContainsCell(IntVec3 cell)
	{
		if (Map != null)
		{
			return cell.GetRoom(Map) == this;
		}
		return false;
	}

	public bool ContainsThing(ThingDef def)
	{
		List<Region> regions = Regions;
		for (int i = 0; i < regions.Count; i++)
		{
			if (regions[i].ListerThings.ThingsOfDef(def).Any())
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerable<Thing> ContainedThings(ThingDef def)
	{
		uniqueContainedThingsOfDef.Clear();
		List<Region> regions = Regions;
		int i = 0;
		while (i < regions.Count)
		{
			List<Thing> things = regions[i].ListerThings.ThingsOfDef(def);
			int num;
			for (int j = 0; j < things.Count; j = num)
			{
				if (uniqueContainedThingsOfDef.Add(things[j]))
				{
					yield return things[j];
				}
				num = j + 1;
			}
			num = i + 1;
			i = num;
		}
		uniqueContainedThingsOfDef.Clear();
	}

	public IEnumerable<T> ContainedThings<T>() where T : Thing
	{
		uniqueContainedThingsOfDef.Clear();
		foreach (Region region in Regions)
		{
			foreach (T item in region.ListerThings.GetThingsOfType<T>())
			{
				if (uniqueContainedThingsOfDef.Add(item))
				{
					yield return item;
				}
			}
		}
		uniqueContainedThingsOfDef.Clear();
	}

	public IEnumerable<Thing> ContainedThingsList(IEnumerable<ThingDef> thingDefs)
	{
		foreach (ThingDef thingDef in thingDefs)
		{
			foreach (Thing item in ContainedThings(thingDef))
			{
				yield return item;
			}
		}
	}

	public int ThingCount(ThingDef def)
	{
		uniqueContainedThingsOfDef.Clear();
		List<Region> regions = Regions;
		int num = 0;
		for (int i = 0; i < regions.Count; i++)
		{
			List<Thing> list = regions[i].ListerThings.ThingsOfDef(def);
			for (int j = 0; j < list.Count; j++)
			{
				if (uniqueContainedThingsOfDef.Add(list[j]))
				{
					num += list[j].stackCount;
				}
			}
		}
		uniqueContainedThingsOfDef.Clear();
		return num;
	}

	public float GetStat(RoomStatDef roomStat)
	{
		if (statsAndRoleDirty)
		{
			UpdateRoomStatsAndRole();
		}
		if (stats == null)
		{
			return roomStat.roomlessScore;
		}
		return stats[roomStat];
	}

	public void DrawFieldEdges()
	{
		fields.Clear();
		fields.AddRange(Cells);
		Color color = (isPrisonCell ? PrisonFieldColor : NonPrisonFieldColor);
		color.a = Pulser.PulseBrightness(1f, 0.6f);
		GenDraw.DrawFieldEdges(fields, color, null);
		fields.Clear();
	}

	private void UpdateRoomStatsAndRole()
	{
		statsAndRoleDirty = false;
		if (ProperRoom && RegionCount <= 36)
		{
			if (stats == null)
			{
				stats = new DefMap<RoomStatDef, float>();
			}
			foreach (RoomStatDef item in DefDatabase<RoomStatDef>.AllDefs.OrderByDescending((RoomStatDef x) => x.updatePriority))
			{
				stats[item] = item.Worker.GetScore(this);
			}
			role = DefDatabase<RoomRoleDef>.AllDefs.MaxBy((RoomRoleDef x) => x.Worker.GetScore(this));
		}
		else
		{
			stats = null;
			role = RoomRoleDefOf.None;
		}
	}

	public string GetRoomRoleLabel()
	{
		Pawn pawn = null;
		Pawn pawn2 = null;
		foreach (Pawn owner in Owners)
		{
			if (pawn == null)
			{
				pawn = owner;
			}
			else
			{
				pawn2 = owner;
			}
		}
		TaggedString taggedString = ((pawn == null) ? ((TaggedString)Role.PostProcessedLabel(this)) : ((pawn2 != null && pawn2 != pawn) ? "CouplesRoom".Translate(pawn.LabelShort, pawn2.LabelShort, Role.label, pawn.Named("PAWN1"), pawn2.Named("PAWN2")) : "SomeonesRoom".Translate(pawn.LabelShort, Role.label, pawn.Named("PAWN"))));
		return taggedString;
	}

	public string DebugString()
	{
		return string.Concat("Room ID=", ID, "\n  first cell=", Cells.FirstOrDefault(), "\n  DistrictCount=", DistrictCount, "\n  RegionCount=", RegionCount, "\n  CellCount=", CellCount, "\n  OpenRoofCount=", OpenRoofCount, "\n  PsychologicallyOutdoors=", PsychologicallyOutdoors.ToString(), "\n  OutdoorsForWork=", OutdoorsForWork.ToString(), "\n  WellEnclosed=", ProperRoom.ToString(), "\n  ", tempTracker.DebugString(), DebugViewSettings.writeRoomRoles ? ("\n" + DebugRolesString()) : "");
	}

	private string DebugRolesString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (var (num, arg) in from x in DefDatabase<RoomRoleDef>.AllDefs
			select (score: x.Worker.GetScore(this), role: x) into tuple
			orderby tuple.score descending
			select tuple)
		{
			stringBuilder.AppendLine($"{num}: {arg}");
		}
		return stringBuilder.ToString();
	}

	internal void DebugDraw()
	{
		int num = Gen.HashCombineInt(GetHashCode(), 1948571531);
		foreach (IntVec3 cell in Cells)
		{
			CellRenderer.RenderCell(cell, (float)num * 0.01f);
		}
		tempTracker.DebugDraw();
	}

	public override string ToString()
	{
		return "Room(roomID=" + ID + ", first=" + Cells.FirstOrDefault().ToString() + ", RegionsCount=" + RegionCount.ToString() + ")";
	}

	public override int GetHashCode()
	{
		return Gen.HashCombineInt(ID, 1538478891);
	}
}
