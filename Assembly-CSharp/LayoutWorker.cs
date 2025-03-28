using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

public abstract class LayoutWorker
{
	private readonly LayoutDef def;

	private static readonly List<IntVec3> tmpSpawnLocations = new List<IntVec3>();

	public LayoutDef Def => def;

	public LayoutWorker(LayoutDef def)
	{
		this.def = def;
	}

	public LayoutStructureSketch GenerateStructureSketch(StructureGenParams parms)
	{
		LayoutSketch layoutSketch = GenerateSketch(parms);
		ResolveRoomDefs(layoutSketch, layoutSketch.layout);
		return new LayoutStructureSketch
		{
			layoutSketch = layoutSketch,
			structureLayout = layoutSketch.layout,
			layoutDef = def
		};
	}

	protected abstract LayoutSketch GenerateSketch(StructureGenParams parms);

	private void ResolveRoomDefs(LayoutSketch sketch, StructureLayout layout)
	{
		LayoutRoomParams roomParams = new LayoutRoomParams
		{
			sketch = sketch
		};
		if (!def.roomDefs.NullOrEmpty())
		{
			List<LayoutRoomDef> usedDefs = new List<LayoutRoomDef>();
			Dictionary<LayoutRoomDef, int> dictionary = new Dictionary<LayoutRoomDef, int>();
			foreach (LayoutRoomDef roomDef2 in def.roomDefs)
			{
				if (roomDef2.countRange.min != 0)
				{
					dictionary.Add(roomDef2, roomDef2.countRange.min);
				}
			}
			foreach (LayoutRoom room in layout.Rooms)
			{
				roomParams.room = room;
				if (room.requiredDef != null)
				{
					if (room.defs == null)
					{
						room.defs = new List<LayoutRoomDef>();
					}
					room.defs.Add(room.requiredDef);
					usedDefs.Add(room.requiredDef);
					if (dictionary.ContainsKey(room.requiredDef))
					{
						if (dictionary[room.requiredDef] == 1)
						{
							dictionary.Remove(room.requiredDef);
						}
						else
						{
							dictionary[room.requiredDef]--;
						}
					}
					continue;
				}
				if (dictionary.Any())
				{
					bool flag = false;
					foreach (LayoutRoomDef key in dictionary.Keys)
					{
						if (key.CanResolve(roomParams))
						{
							if (room.defs == null)
							{
								room.defs = new List<LayoutRoomDef>();
							}
							room.defs.Add(key);
							usedDefs.Add(key);
							if (dictionary[key] == 1)
							{
								dictionary.Remove(key);
							}
							else
							{
								dictionary[key]--;
							}
							flag = true;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
				}
				if (def.roomDefs.Where((LayoutRoomDef d) => d.CanResolve(roomParams) && usedDefs.Count((LayoutRoomDef ud) => ud == d) < d.countRange.max).TryRandomElementByWeight((LayoutRoomDef d) => d.selectionWeight, out var roomDef))
				{
					if (room.defs == null)
					{
						room.defs = new List<LayoutRoomDef>();
					}
					room.defs.Add(roomDef);
					usedDefs.Add(roomDef);
					if (def.canHaveMultipleLayoutsInRoom && roomDef.canBeInMixedRoom && Rand.Chance(def.multipleLayoutRoomChance) && def.roomDefs.Where((LayoutRoomDef d) => d != roomDef && d.canBeInMixedRoom && d.CanResolve(roomParams) && usedDefs.Count((LayoutRoomDef ud) => ud == d) < d.countRange.max).TryRandomElementByWeight((LayoutRoomDef d) => d.selectionWeight, out var result))
					{
						room.defs.Add(result);
						usedDefs.Add(result);
					}
				}
			}
			if (dictionary.Any())
			{
				Log.ErrorOnce("Layout failed to spawn all required rooms, rooms failed to place: " + dictionary.Keys.Select((LayoutRoomDef x) => x.defName).ToCommaList(), 114632452);
			}
		}
		foreach (LayoutRoom room2 in layout.Rooms)
		{
			if (room2.defs.NullOrEmpty())
			{
				continue;
			}
			roomParams.room = room2;
			foreach (LayoutRoomDef def in room2.defs)
			{
				def.ResolveSketch(roomParams);
			}
		}
	}

	public virtual void Spawn(LayoutStructureSketch structureSketch, Map map, IntVec3 center, float? threatPoints = null, List<Thing> allSpawnedThings = null, bool roofs = true)
	{
		List<Thing> spawnedThings = allSpawnedThings ?? new List<Thing>();
		LayoutSketch layoutSketch = structureSketch.layoutSketch;
		bool buildRoofsInstantly = roofs;
		layoutSketch.Spawn(map, center, null, Sketch.SpawnPosType.Unchanged, Sketch.SpawnMode.Normal, wipeIfCollides: true, clearEdificeWhereFloor: true, spawnedThings, dormant: false, buildRoofsInstantly);
	}

	public void FillRoomContents(LayoutStructureSketch structureSketch, Map map)
	{
		foreach (LayoutRoom room in structureSketch.structureLayout.Rooms)
		{
			if (room.defs == null)
			{
				continue;
			}
			foreach (LayoutRoomDef def in room.defs)
			{
				def.ResolveContents(map, room);
			}
		}
	}

	protected static IntVec3 FindBestSpawnLocation(List<List<CellRect>> rooms, ThingDef thingDef, Map map, out List<CellRect> roomUsed, out Rot4 rotUsed, HashSet<List<CellRect>> usedRooms = null)
	{
		tmpSpawnLocations.Clear();
		foreach (List<CellRect> item in rooms.InRandomOrder())
		{
			if (usedRooms != null && usedRooms.Contains(item))
			{
				continue;
			}
			tmpSpawnLocations.Clear();
			tmpSpawnLocations.AddRange(item.SelectMany((CellRect r) => r.Cells));
			foreach (IntVec3 item2 in tmpSpawnLocations.InRandomOrder())
			{
				for (int i = 0; i < 4; i++)
				{
					Rot4 rot = new Rot4(i);
					CellRect cellRect = GenAdj.OccupiedRect(item2, rot, thingDef.size);
					bool flag = false;
					foreach (IntVec3 cell in cellRect.Cells)
					{
						if (!cell.Standable(map) || cell.GetDoor(map) != null)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
					bool flag2 = false;
					foreach (IntVec3 edgeCell in cellRect.ExpandedBy(1).EdgeCells)
					{
						if (edgeCell.GetThingList(map).Any((Thing t) => t.def == ThingDefOf.Door))
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2 && ThingUtility.InteractionCellWhenAt(thingDef, item2, rot, map).Standable(map))
					{
						tmpSpawnLocations.Clear();
						usedRooms?.Add(item);
						roomUsed = item;
						rotUsed = rot;
						return item2;
					}
				}
			}
		}
		roomUsed = null;
		rotUsed = default(Rot4);
		return IntVec3.Invalid;
	}
}
