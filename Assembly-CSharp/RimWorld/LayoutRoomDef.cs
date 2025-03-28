using System;
using System.Collections.Generic;
using RimWorld.SketchGen;
using Verse;

namespace RimWorld;

public class LayoutRoomDef : Def
{
	public SketchResolverDef sketchResolverDef;

	public float selectionWeight = 1f;

	public IntRange countRange = new IntRange(0, int.MaxValue);

	public IntRange areaSizeRange = new IntRange(25, int.MaxValue);

	public bool requiresSingleRectRoom;

	public List<TerrainDef> floorTypes;

	public int minSingleRectWidth;

	public int minSingleRectHeight;

	public Type roomContentsWorkerType;

	public bool canBeInMixedRoom;

	public bool dontPlaceRandomly;

	public bool isValidPlayerSpawnRoom = true;

	[Unsaved(false)]
	private RoomContentsWorker workerInt;

	public RoomContentsWorker ContentsWorker => GetWorker(ref workerInt);

	public bool CanResolve(LayoutRoomParams parms)
	{
		int area = parms.room.Area;
		if (parms.room.requiredDef != this && dontPlaceRandomly)
		{
			return false;
		}
		if ((minSingleRectHeight > 0 || minSingleRectWidth > 0) && !parms.room.TryGetRectOfSize(minSingleRectWidth, minSingleRectHeight, out var _))
		{
			return false;
		}
		if (area >= areaSizeRange.min && area <= areaSizeRange.max)
		{
			if (requiresSingleRectRoom)
			{
				return parms.room.rects.Count == 1;
			}
			return true;
		}
		return false;
	}

	public void ResolveSketch(LayoutRoomParams parms)
	{
		ResolveParams parms2 = default(ResolveParams);
		foreach (CellRect rect in parms.room.rects)
		{
			if (!floorTypes.NullOrEmpty())
			{
				TerrainDef def = floorTypes.RandomElement();
				foreach (IntVec3 item in rect)
				{
					parms.sketch.AddTerrain(def, item);
				}
			}
			parms2.rect = rect;
			parms2.sketch = parms.sketch;
			if (sketchResolverDef != null)
			{
				sketchResolverDef.Resolve(parms2);
			}
		}
	}

	public void ResolveContents(Map map, LayoutRoom room)
	{
		if (ContentsWorker != null)
		{
			ContentsWorker.FillRoom(map, room);
		}
	}

	private RoomContentsWorker GetWorker(ref RoomContentsWorker worker)
	{
		if (roomContentsWorkerType == null)
		{
			return null;
		}
		if (worker != null)
		{
			return worker;
		}
		worker = (RoomContentsWorker)Activator.CreateInstance(roomContentsWorkerType);
		worker.Initialize(this);
		return worker;
	}
}
