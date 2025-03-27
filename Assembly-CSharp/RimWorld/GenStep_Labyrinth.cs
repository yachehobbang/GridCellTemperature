using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class GenStep_Labyrinth : GenStep
{
	private LayoutStructureSketch structureSketch;

	public override int SeedPart => 8767466;

	public override void Generate(Map map, GenStepParams parms)
	{
		if (!ModLister.CheckAnomaly("Labyrinth"))
		{
			return;
		}
		TerrainGrid terrainGrid = map.terrainGrid;
		foreach (IntVec3 allCell in map.AllCells)
		{
			terrainGrid.SetTerrain(allCell, TerrainDefOf.GraySurface);
		}
		StructureGenParams parms2 = new StructureGenParams
		{
			size = new IntVec2(map.Size.x, map.Size.z)
		};
		LayoutWorker worker = LayoutDefOf.Labyrinth.Worker;
		int num = 10;
		do
		{
			structureSketch = worker.GenerateStructureSketch(parms2);
		}
		while (!structureSketch.structureLayout.HasRoomWithDef(LayoutRoomDefOf.LabyrinthObelisk) && num-- > 0);
		if (num == 0)
		{
			Log.ErrorOnce("Failed to generate labyrinth, guard exceeded. Check layout worker for errors placing minimum rooms", 9868797);
			return;
		}
		worker.Spawn(structureSketch, map, IntVec3.Zero, null, null, roofs: false);
		worker.FillRoomContents(structureSketch, map);
		map.layoutStructureSketch = structureSketch;
		LabyrinthMapComponent component = map.GetComponent<LabyrinthMapComponent>();
		LayoutRoom firstRoomOfDef = structureSketch.structureLayout.GetFirstRoomOfDef(LayoutRoomDefOf.LabyrinthObelisk);
		List<LayoutRoom> spawnableRooms = GetSpawnableRooms(firstRoomOfDef);
		component.SetSpawnRooms(spawnableRooms);
		MapGenerator.PlayerStartSpot = IntVec3.Zero;
		map.fogGrid.SetAllFogged();
	}

	private List<LayoutRoom> GetSpawnableRooms(LayoutRoom obelisk)
	{
		List<LayoutRoom> list = new List<LayoutRoom>();
		list.AddRange(structureSketch.structureLayout.Rooms);
		list.Remove(obelisk);
		foreach (LayoutRoom logicalRoomConnection in structureSketch.structureLayout.GetLogicalRoomConnections(obelisk))
		{
			if (!list.Contains(logicalRoomConnection))
			{
				continue;
			}
			list.Remove(logicalRoomConnection);
			foreach (LayoutRoom logicalRoomConnection2 in structureSketch.structureLayout.GetLogicalRoomConnections(logicalRoomConnection))
			{
				if (list.Contains(logicalRoomConnection2))
				{
					list.Remove(logicalRoomConnection2);
				}
			}
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			foreach (LayoutRoomDef def in list[num].defs)
			{
				if (!def.isValidPlayerSpawnRoom)
				{
					list.RemoveAt(num);
					break;
				}
			}
		}
		if (list.Empty())
		{
			list.Clear();
			list.AddRange(structureSketch.structureLayout.Rooms);
			list.Remove(obelisk);
		}
		return list;
	}
}
