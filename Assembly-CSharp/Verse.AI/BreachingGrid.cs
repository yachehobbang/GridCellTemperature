using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse.AI.Group;
using Verse.Noise;

namespace Verse.AI;

public class BreachingGrid : IExposable
{
	private class CustomTuning : PathFinderCostTuning.ICustomizer
	{
		private readonly int breachRadius;

		private readonly BreachingGrid grid;

		private readonly PathFinderCostTuning tuning;

		public CustomTuning(int breachRadius, BreachingGrid grid, PathFinderCostTuning tuning)
		{
			this.breachRadius = breachRadius;
			this.grid = grid;
			this.tuning = tuning;
		}

		public int CostOffset(IntVec3 from, IntVec3 to)
		{
			IntVec3 intVec = (to - from).RotatedBy(Rot4.East);
			int num = 0;
			for (int i = -breachRadius; i <= breachRadius; i++)
			{
				IntVec3 intVec2 = to + intVec * i;
				if (intVec2.InBounds(grid.Map) && i != 0)
				{
					num += CostOffAdjacent(intVec2) + grid.cellCostOffset[intVec2];
				}
			}
			if (tweakUsePerlin && grid.WithinNoise(to))
			{
				num += tweakPerlinCost;
			}
			return num;
		}

		private int CostOffAdjacent(IntVec3 cell)
		{
			Building edifice = cell.GetEdifice(grid.Map);
			if (edifice != null && PathFinder.IsDestroyable(edifice))
			{
				return tuning.costBlockedWallBase + (int)((float)edifice.HitPoints * tuning.costBlockedWallExtraPerHitPoint) + ((!edifice.def.IsBuildingArtificial) ? tuning.costBlockedWallExtraForNaturalWalls : 0);
			}
			return 0;
		}
	}

	private class DangerLineOfSightPainter
	{
		private Action<IntVec3> visitor;

		private int skipCount;

		private Action<IntVec3> skipThenVisitFunc;

		public DangerLineOfSightPainter()
		{
			skipThenVisitFunc = SkipThenVisit;
		}

		private void SkipThenVisit(IntVec3 cell)
		{
			if (skipCount <= 0)
			{
				visitor(cell);
			}
			skipCount--;
		}

		public void PaintLoS(Map map, IntVec3 start, IntVec3 end, Action<IntVec3> visitor)
		{
			if (start.InBounds(map) && end.InBounds(map))
			{
				this.visitor = visitor;
				skipCount = Mathf.FloorToInt(5f);
				GenSight.PointsOnLineOfSight(start, end, skipThenVisitFunc);
			}
		}
	}

	private class WalkReachabilityPainter
	{
		private BreachingGrid breachingGrid;

		private Predicate<IntVec3> floodFillPassCheckFunc;

		private Func<IntVec3, int, bool> floodFillProcessorFunc;

		public WalkReachabilityPainter()
		{
			floodFillPassCheckFunc = FloodFillPassCheck;
			floodFillProcessorFunc = FloodFillProcessor;
		}

		public void PaintWalkReachability(BreachingGrid breachingGrid)
		{
			this.breachingGrid = breachingGrid;
			breachingGrid.map.floodFiller.FloodFill(this.breachingGrid.breachStart, floodFillPassCheckFunc, floodFillProcessorFunc);
		}

		private bool FloodFillProcessor(IntVec3 c, int traversalDist)
		{
			breachingGrid.reachableGrid[c] = true;
			return false;
		}

		private bool FloodFillPassCheck(IntVec3 c)
		{
			if (breachingGrid.WalkGrid[c])
			{
				return !BreachingUtility.BlocksBreaching(breachingGrid.map, c);
			}
			return false;
		}
	}

	public const byte Marker_FiringPosition = 180;

	public const byte Marker_Dangerous = 10;

	public const byte Marker_UnUsed = 0;

	private const int DangerousRoomPathCost = 600;

	private static WalkReachabilityPainter cachedWalkReachabilityPainter = new WalkReachabilityPainter();

	private static DangerLineOfSightPainter cachedDangerLineOfSightPainter = new DangerLineOfSightPainter();

	private BoolGrid walkGrid;

	private BoolGrid breachGrid;

	private int perlinSeed;

	private int breachRadius = 1;

	private IntVec3 breachStart = IntVec3.Invalid;

	private Map map;

	private Lord lord;

	private Perlin perlinCached;

	private BoolGrid reachableGrid;

	private ByteGrid markerGrid;

	private bool cachedGridsDirty = true;

	private IntGrid cellCostOffset;

	[TweakValue("Breaching", 0f, 70f)]
	private static int tweakWallCost = 10;

	[TweakValue("Breaching", 0f, 1f)]
	private static float tweakWallHpCost = 0.01f;

	[TweakValue("Breaching", 0f, 100f)]
	private static bool tweakUsePerlin = true;

	[TweakValue("Breaching", -70f, 70f)]
	private static int tweakPerlinCost = 30;

	[TweakValue("Breaching", 1f, 7f)]
	public static int tweakOffWalkGridPathCost = 140;

	[TweakValue("Breaching", 0f, 100f)]
	private static bool tweakAvoidDangerousRooms = true;

	[TweakValue("Breaching", -70f, 70f)]
	private static int tweakNaturalWallExtraCost = 20;

	[TweakValue("Breaching", 0f, 0.1f)]
	private static float perlinFrequency = 0.06581f;

	[TweakValue("Breaching", 1f, 2f)]
	private static float perlinLacunarity = 1.5516f;

	[TweakValue("Breaching", 0f, 2f)]
	private static float perlinPersistence = 1.6569f;

	[TweakValue("Breaching", 1f, 5f)]
	private static float perlinOctaves = 4f;

	[TweakValue("Breaching", 0f, 1f)]
	private static float perlinThres = 0.5f;

	private static BoolGrid tmpWidenGrid = new BoolGrid();

	public BoolGrid WalkGrid => walkGrid;

	public BoolGrid BreachGrid => breachGrid;

	public ByteGrid MarkerGrid
	{
		get
		{
			RegenerateCachedGridIfDirty();
			return markerGrid;
		}
	}

	public BoolGrid ReachableGrid
	{
		get
		{
			RegenerateCachedGridIfDirty();
			return reachableGrid;
		}
	}

	public Map Map => map;

	public int BreachRadius => breachRadius;

	public Perlin Noise
	{
		get
		{
			if (perlinCached == null)
			{
				perlinCached = CreatePerlinNoise(perlinSeed);
			}
			return perlinCached;
		}
	}

	public BreachingGrid()
	{
	}

	public BreachingGrid(Map map, Lord lord)
	{
		this.map = map;
		this.lord = lord;
		walkGrid = new BoolGrid(map);
		breachGrid = new BoolGrid(map);
		perlinSeed = Rand.Int;
	}

	public static Perlin CreatePerlinNoise(int seed)
	{
		return new Perlin(perlinFrequency, perlinLacunarity, perlinPersistence, (int)perlinOctaves, seed, QualityMode.Medium);
	}

	public void Notify_PawnStateChanged(Pawn pawn)
	{
		cachedGridsDirty = true;
	}

	public void Notify_BuildingStateChanged(Building b)
	{
		cachedGridsDirty = true;
	}

	public bool WithinNoise(IntVec3 cell)
	{
		return Noise.GetValue(cell) >= perlinThres;
	}

	public void CreateBreachPath(IntVec3 start, IntVec3 end, int breachRadius, int walkMargin, bool useAvoidGrid = false)
	{
		this.breachRadius = breachRadius;
		breachStart = start;
		SetupCostOffsets();
		PathFinderCostTuning pathFinderCostTuning = new PathFinderCostTuning();
		pathFinderCostTuning.costBlockedDoor = tweakWallCost;
		pathFinderCostTuning.costBlockedWallBase = tweakWallCost;
		pathFinderCostTuning.costBlockedDoorPerHitPoint = tweakWallHpCost;
		pathFinderCostTuning.costBlockedWallExtraPerHitPoint = tweakWallHpCost;
		pathFinderCostTuning.costOffLordWalkGrid = tweakOffWalkGridPathCost;
		pathFinderCostTuning.costBlockedWallExtraForNaturalWalls = tweakNaturalWallExtraCost;
		pathFinderCostTuning.custom = new CustomTuning(breachRadius, this, pathFinderCostTuning);
		TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassAllDestroyableThings, Danger.Deadly, canBashDoors: false, useAvoidGrid);
		using (PawnPath pawnPath = map.pathFinder.FindPath(start, end, traverseParms, PathEndMode.OnCell, pathFinderCostTuning))
		{
			foreach (IntVec3 item in pawnPath.NodesReversed)
			{
				breachGrid[item] = true;
				walkGrid[item] = true;
			}
		}
		for (int i = 0; i < breachRadius; i++)
		{
			WidenGrid(breachGrid);
			WidenGrid(walkGrid);
		}
		for (int j = 0; j < walkMargin; j++)
		{
			WidenGrid(walkGrid);
		}
	}

	public Thing FindBuildingToBreach()
	{
		Building bestBuilding = null;
		int bestBuildingDist = int.MaxValue;
		int bestBuildingReachableSideCount = 0;
		RegenerateCachedGridIfDirty();
		Map.floodFiller.FloodFill(breachStart, (IntVec3 c) => BreachGrid[c], delegate(IntVec3 c, int dist)
		{
			List<Thing> thingList = c.GetThingList(Map);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i] is Building building && BreachingUtility.ShouldBreachBuilding(building) && BreachingUtility.IsWorthBreachingBuilding(this, building))
				{
					int num = BreachingUtility.CountReachableAdjacentCells(this, building);
					if (num > 0 && num > bestBuildingReachableSideCount)
					{
						bestBuilding = building;
						bestBuildingDist = dist;
						bestBuildingReachableSideCount = num;
						break;
					}
				}
			}
			return dist - 2 > bestBuildingDist;
		});
		return bestBuilding;
	}

	private void WidenGrid(BoolGrid grid)
	{
		tmpWidenGrid.ClearAndResizeTo(map);
		foreach (IntVec3 activeCell in grid.ActiveCells)
		{
			for (int i = 0; i < 8; i++)
			{
				IntVec3 c = activeCell + GenAdj.AdjacentCells[i];
				if (c.InBounds(map))
				{
					tmpWidenGrid[c] = true;
				}
			}
		}
		foreach (IntVec3 activeCell2 in tmpWidenGrid.ActiveCells)
		{
			grid[activeCell2] = true;
		}
	}

	public void Reset()
	{
		breachGrid.Clear();
		walkGrid.Clear();
		cellCostOffset?.Clear();
		markerGrid?.Clear(0);
		reachableGrid?.Clear();
		cachedGridsDirty = true;
		BreachingGridDebug.ClearDebugPath();
	}

	private void RegenerateCachedGridIfDirty()
	{
		if (cachedGridsDirty)
		{
			RegenerateCachedGrid();
		}
	}

	private void RegenerateCachedGrid()
	{
		cachedGridsDirty = false;
		if (markerGrid == null)
		{
			markerGrid = new ByteGrid(Map);
		}
		else
		{
			markerGrid.Clear(0);
		}
		if (reachableGrid == null)
		{
			reachableGrid = new BoolGrid(Map);
		}
		else
		{
			reachableGrid.Clear();
		}
		cachedWalkReachabilityPainter.PaintWalkReachability(this);
		if (lord == null)
		{
			return;
		}
		for (int i = 0; i < lord.ownedPawns.Count; i++)
		{
			Pawn pawn = lord.ownedPawns[i];
			if (pawn.mindState.breachingTarget != null && !pawn.mindState.breachingTarget.target.Destroyed)
			{
				PaintDangerFromPawn(pawn);
			}
		}
	}

	private void PaintDangerFromPawn(Pawn pawn)
	{
		BreachingTargetData breachingTarget = pawn.mindState.breachingTarget;
		if (breachingTarget == null)
		{
			return;
		}
		IntVec3 position = breachingTarget.target.Position;
		if (!position.IsValid)
		{
			return;
		}
		Verb verb = BreachingUtility.FindVerbToUseForBreaching(pawn);
		if (verb == null)
		{
			return;
		}
		IntVec3 firingPosition = breachingTarget.firingPosition;
		if (firingPosition.IsValid)
		{
			if (markerGrid[firingPosition] == 0)
			{
				markerGrid[firingPosition] = 180;
			}
			VisitDangerousCellsOfAttack(firingPosition, position, verb, DangerSetter);
		}
		void DangerSetter(IntVec3 cell)
		{
			markerGrid[cell] = 10;
		}
	}

	public void VisitDangerousCellsOfAttack(IntVec3 firingPosition, IntVec3 targetPosition, Verb verb, Action<IntVec3> visitor)
	{
		if (!verb.IsMeleeAttack)
		{
			cachedDangerLineOfSightPainter.PaintLoS(map, firingPosition, targetPosition, visitor);
			PaintSplashDamage(verb, targetPosition, visitor);
		}
	}

	private void PaintSplashDamage(Verb verb, IntVec3 center, Action<IntVec3> visitor)
	{
		float num = 2f;
		ThingDef projectile = verb.GetProjectile();
		if (projectile != null && projectile.projectile.explosionRadius > 0f)
		{
			num = Mathf.Max(num, projectile.projectile.explosionRadius);
		}
		int num2 = GenRadial.NumCellsInRadius(num);
		for (int i = 0; i < num2; i++)
		{
			IntVec3 obj = (center + GenRadial.RadialPattern[i]).ClampInsideMap(map);
			visitor(obj);
		}
	}

	private void SetupCostOffsets()
	{
		if (cellCostOffset == null)
		{
			cellCostOffset = new IntGrid(map);
		}
		cellCostOffset.Clear();
		if (!tweakAvoidDangerousRooms)
		{
			return;
		}
		foreach (Room allRoom in map.regionGrid.allRooms)
		{
			int num = DangerousRoomCost(allRoom);
			if (num == 0)
			{
				continue;
			}
			foreach (IntVec3 cell in allRoom.Cells)
			{
				cellCostOffset[cell] = num;
			}
			foreach (IntVec3 borderCell in allRoom.BorderCells)
			{
				if (borderCell.InBounds(map))
				{
					cellCostOffset[borderCell] = num;
				}
			}
		}
	}

	private int DangerousRoomCost(Room room)
	{
		if (!room.Fogged)
		{
			return 0;
		}
		foreach (Thing containedAndAdjacentThing in room.ContainedAndAdjacentThings)
		{
			if (containedAndAdjacentThing is Pawn { mindState: not null } pawn && !pawn.mindState.Active)
			{
				return 600;
			}
			if (containedAndAdjacentThing.def == ThingDefOf.Hive)
			{
				return 600;
			}
			if (containedAndAdjacentThing.def == ThingDefOf.AncientCryptosleepCasket)
			{
				return 600;
			}
		}
		return 0;
	}

	public void ExposeData()
	{
		Scribe_Deep.Look(ref walkGrid, "walkGrid");
		Scribe_Deep.Look(ref breachGrid, "breachGrid");
		Scribe_Values.Look(ref perlinSeed, "perlinSeed", 0);
		Scribe_Values.Look(ref breachRadius, "breachRadius", 0);
		Scribe_Values.Look(ref breachStart, "breachStart");
		Scribe_References.Look(ref map, "map");
		Scribe_References.Look(ref lord, "lord");
	}
}
