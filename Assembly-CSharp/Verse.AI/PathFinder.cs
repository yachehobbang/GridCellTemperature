#define PFPROFILE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;

namespace Verse.AI;

public class PathFinder
{
	internal struct CostNode
	{
		public int index;

		public int cost;

		public CostNode(int index, int cost)
		{
			this.index = index;
			this.cost = cost;
		}
	}

	private struct PathFinderNodeFast
	{
		public int knownCost;

		public int heuristicCost;

		public int parentIndex;

		public int costNodeCost;

		public ushort status;
	}

	internal class CostNodeComparer : IComparer<CostNode>
	{
		public int Compare(CostNode a, CostNode b)
		{
			return a.cost.CompareTo(b.cost);
		}
	}

	private Map map;

	private PriorityQueue<int, int> openList;

	private static PathFinderNodeFast[] calcGrid;

	private static ushort statusOpenValue = 1;

	private static ushort statusClosedValue = 2;

	private RegionCostCalculatorWrapper regionCostCalculator;

	private int mapSizeX;

	private int mapSizeZ;

	private PathGrid pathGrid;

	private TraverseParms traverseParms;

	private PathingContext pathingContext;

	private Building[] edificeGrid;

	private List<Blueprint>[] blueprintGrid;

	private CellIndices cellIndices;

	private List<int> disallowedCornerIndices = new List<int>(4);

	public const int DefaultMoveTicksCardinal = 13;

	private const int DefaultMoveTicksDiagonal = 18;

	private const int SearchLimit = 160000;

	private static readonly int[] Directions = new int[16]
	{
		0, 1, 0, -1, 1, 1, -1, -1, -1, 0,
		1, 0, -1, 1, 1, -1
	};

	private const int Cost_DoorToBash = 300;

	private const int Cost_FenceToBash = 300;

	public const int Cost_OutsideAllowedArea = 600;

	private const int Cost_PawnCollision = 175;

	private const int NodesToOpenBeforeRegionBasedPathing_NonColonist = 2000;

	private const int NodesToOpenBeforeRegionBasedPathing_Colonist = 100000;

	private const float NonRegionBasedHeuristicStrengthAnimal = 1.75f;

	private static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve = new SimpleCurve
	{
		new CurvePoint(40f, 1f),
		new CurvePoint(120f, 2.8f)
	};

	private static readonly SimpleCurve RegionHeuristicWeightByNodesOpened = new SimpleCurve
	{
		new CurvePoint(0f, 1f),
		new CurvePoint(3500f, 1f),
		new CurvePoint(4500f, 5f),
		new CurvePoint(30000f, 50f),
		new CurvePoint(100000f, 500f)
	};

	public PathFinder(Map map)
	{
		this.map = map;
		mapSizeX = map.Size.x;
		mapSizeZ = map.Size.z;
		int num = mapSizeX * mapSizeZ;
		if (calcGrid == null || calcGrid.Length < num)
		{
			calcGrid = new PathFinderNodeFast[num];
		}
		openList = new PriorityQueue<int, int>();
		regionCostCalculator = new RegionCostCalculatorWrapper(map);
	}

	public PawnPath FindPath(IntVec3 start, LocalTargetInfo dest, Pawn pawn, PathEndMode peMode = PathEndMode.OnCell, PathFinderCostTuning tuning = null)
	{
		bool canBashDoors = false;
		bool canBashFences = false;
		if (pawn?.CurJob != null)
		{
			if (pawn.CurJob.canBashDoors)
			{
				canBashDoors = true;
			}
			if (pawn.CurJob.canBashFences)
			{
				canBashFences = true;
			}
		}
		return FindPath(start, dest, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, canBashDoors, alwaysUseAvoidGrid: false, canBashFences), peMode, tuning);
	}

	public PawnPath FindPath(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell, PathFinderCostTuning tuning = null)
	{
		if (DebugSettings.pathThroughWalls)
		{
			traverseParms.mode = TraverseMode.PassAllDestroyableThings;
		}
		Pawn pawn = traverseParms.pawn;
		if (pawn != null && pawn.Map != map)
		{
			Log.Error(string.Concat("Tried to FindPath for pawn which is spawned in another map. His map PathFinder should have been used, not this one. pawn=", pawn, " pawn.Map=", pawn.Map, " map=", map));
			return PawnPath.NotFound;
		}
		if (!start.IsValid)
		{
			Log.Error(string.Concat("Tried to FindPath with invalid start ", start, ", pawn= ", pawn));
			return PawnPath.NotFound;
		}
		if (!dest.IsValid)
		{
			Log.Error(string.Concat("Tried to FindPath with invalid dest ", dest, ", pawn= ", pawn));
			return PawnPath.NotFound;
		}
		if (traverseParms.mode == TraverseMode.ByPawn)
		{
			if (!pawn.CanReach(dest, peMode, Danger.Deadly, traverseParms.canBashDoors, traverseParms.canBashFences, traverseParms.mode))
			{
				return PawnPath.NotFound;
			}
		}
		else if (!map.reachability.CanReach(start, dest, peMode, traverseParms))
		{
			return PawnPath.NotFound;
		}
		PfProfilerBeginSample("FindPath");
		cellIndices = map.cellIndices;
		pathingContext = map.pathing.For(traverseParms);
		pathGrid = pathingContext.pathGrid;
		this.traverseParms = traverseParms;
		this.edificeGrid = map.edificeGrid.InnerArray;
		blueprintGrid = map.blueprintGrid.InnerArray;
		int x = dest.Cell.x;
		int z = dest.Cell.z;
		int curIndex = cellIndices.CellToIndex(start);
		int num = cellIndices.CellToIndex(dest.Cell);
		ByteGrid byteGrid = (traverseParms.alwaysUseAvoidGrid ? map.avoidGrid.Grid : pawn?.GetAvoidGrid());
		bool flag = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
		bool flag2 = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
		bool flag3 = !flag;
		CellRect cellRect = CalculateDestinationRect(dest, peMode);
		bool flag4 = cellRect.Width == 1 && cellRect.Height == 1;
		int[] array = pathGrid.pathGrid;
		TerrainDef[] topGrid = map.terrainGrid.topGrid;
		EdificeGrid edificeGrid = map.edificeGrid;
		int num2 = 0;
		int num3 = 0;
		Area allowedArea = GetAllowedArea(pawn);
		BoolGrid lordWalkGrid = GetLordWalkGrid(pawn);
		bool flag5 = pawn != null && PawnUtility.ShouldCollideWithPawns(pawn);
		bool flag6 = !flag && traverseParms.mode != TraverseMode.PassAllDestroyablePlayerOwnedThings && start.GetRegion(map) != null && flag2;
		bool flag7 = !flag || !flag3;
		bool flag8 = false;
		bool flag9 = pawn?.Drafted ?? false;
		int num4 = ((pawn != null && pawn.IsColonist) ? 100000 : 2000);
		tuning = tuning ?? PathFinderCostTuning.DefaultTuning;
		int costBlockedWallBase = tuning.costBlockedWallBase;
		float costBlockedWallExtraPerHitPoint = tuning.costBlockedWallExtraPerHitPoint;
		int costBlockedWallExtraForNaturalWalls = tuning.costBlockedWallExtraForNaturalWalls;
		int costOffLordWalkGrid = tuning.costOffLordWalkGrid;
		float num5 = DetermineHeuristicStrength(pawn, start, dest);
		float num6;
		float num7;
		if (pawn != null)
		{
			num6 = pawn.TicksPerMoveCardinal;
			num7 = pawn.TicksPerMoveDiagonal;
		}
		else
		{
			num6 = 13f;
			num7 = 18f;
		}
		CalculateAndAddDisallowedCorners_NewTemp(peMode, cellRect, dest);
		InitStatusesAndPushStartNode(ref curIndex, start);
		while (true)
		{
			PfProfilerBeginSample("Open cell");
			if (!openList.TryDequeue(out var element, out var priority))
			{
				string text = ((pawn?.CurJob != null) ? pawn.CurJob.ToString() : "null");
				string text2 = ((pawn?.Faction != null) ? pawn.Faction.ToString() : "null");
				if (pawn != null)
				{
					Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " ran out of cells to process.\nJob: ", text, "\nFaction: ", text2));
				}
				DebugDrawRichData();
				PfProfilerEndSample();
				PfProfilerEndSample();
				return PawnPath.NotFound;
			}
			curIndex = element;
			if (priority != calcGrid[curIndex].costNodeCost)
			{
				PfProfilerEndSample();
				continue;
			}
			if (calcGrid[curIndex].status == statusClosedValue)
			{
				PfProfilerEndSample();
				continue;
			}
			IntVec3 intVec = cellIndices.IndexToCell(curIndex);
			int x2 = intVec.x;
			int z2 = intVec.z;
			if (flag4)
			{
				if (curIndex == num)
				{
					PfProfilerEndSample();
					PawnPath result = FinalizedPath(curIndex, flag8);
					PfProfilerEndSample();
					return result;
				}
			}
			else if (cellRect.Contains(intVec) && !disallowedCornerIndices.Contains(curIndex))
			{
				PfProfilerEndSample();
				PawnPath result2 = FinalizedPath(curIndex, flag8);
				PfProfilerEndSample();
				return result2;
			}
			if (num2 > 160000)
			{
				break;
			}
			PfProfilerEndSample();
			PfProfilerBeginSample("Neighbor consideration");
			for (int i = 0; i < 8; i++)
			{
				uint num8 = (uint)(x2 + Directions[i]);
				uint num9 = (uint)(z2 + Directions[i + 8]);
				if (num8 >= mapSizeX || num9 >= mapSizeZ)
				{
					continue;
				}
				int num10 = (int)num8;
				int num11 = (int)num9;
				int num12 = cellIndices.CellToIndex(num10, num11);
				if (calcGrid[num12].status == statusClosedValue && !flag8)
				{
					continue;
				}
				int num13 = 0;
				bool flag10 = false;
				if (!flag2 && new IntVec3(num10, 0, num11).GetTerrain(map).HasTag("Water"))
				{
					continue;
				}
				if (!pathGrid.WalkableFast(num12))
				{
					Building building = edificeGrid[num12];
					if (!flag && (traverseParms.mode != TraverseMode.PassAllDestroyablePlayerOwnedThings || building == null || building.Faction != Faction.OfPlayer))
					{
						continue;
					}
					flag10 = true;
					num13 += costBlockedWallBase;
					if (building == null || !IsDestroyable(building))
					{
						continue;
					}
					num13 += (int)((float)building.HitPoints * costBlockedWallExtraPerHitPoint);
					if (!building.def.IsBuildingArtificial)
					{
						num13 += costBlockedWallExtraForNaturalWalls;
					}
				}
				switch (i)
				{
				case 4:
					if (BlocksDiagonalMovement(curIndex - mapSizeX))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					if (BlocksDiagonalMovement(curIndex + 1))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					break;
				case 5:
					if (BlocksDiagonalMovement(curIndex + mapSizeX))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					if (BlocksDiagonalMovement(curIndex + 1))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					break;
				case 6:
					if (BlocksDiagonalMovement(curIndex + mapSizeX))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					if (BlocksDiagonalMovement(curIndex - 1))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					break;
				case 7:
					if (BlocksDiagonalMovement(curIndex - mapSizeX))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					if (BlocksDiagonalMovement(curIndex - 1))
					{
						if (flag7)
						{
							continue;
						}
						num13 += costBlockedWallBase;
					}
					break;
				}
				float num14 = ((i > 3) ? num7 : num6);
				num14 += (float)num13;
				if (!flag10)
				{
					num14 += (float)array[num12];
					num14 = ((!flag9) ? (num14 + (float)topGrid[num12].extraNonDraftedPerceivedPathCost) : (num14 + (float)topGrid[num12].extraDraftedPerceivedPathCost));
				}
				if (byteGrid != null)
				{
					num14 += (float)(byteGrid[num12] * 8);
				}
				if (allowedArea != null && !allowedArea[num12])
				{
					num14 += 600f;
				}
				if (flag5 && PawnUtility.AnyPawnBlockingPathAt(new IntVec3(num10, 0, num11), pawn, actAsIfHadCollideWithPawnsJob: false, collideOnlyWithStandingPawns: false, forPathFinder: true))
				{
					num14 += 175f;
				}
				Building building2 = this.edificeGrid[num12];
				if (building2 != null)
				{
					PfProfilerBeginSample("Edifices");
					int buildingCost = GetBuildingCost(building2, traverseParms, pawn, tuning);
					if (buildingCost == int.MaxValue)
					{
						PfProfilerEndSample();
						continue;
					}
					num14 += (float)buildingCost;
					PfProfilerEndSample();
				}
				List<Blueprint> list = blueprintGrid[num12];
				if (list != null)
				{
					PfProfilerBeginSample("Blueprints");
					int num15 = 0;
					for (int j = 0; j < list.Count; j++)
					{
						num15 = Mathf.Max(num15, GetBlueprintCost(list[j], pawn));
					}
					if (num15 == int.MaxValue)
					{
						PfProfilerEndSample();
						continue;
					}
					num14 += (float)num15;
					PfProfilerEndSample();
				}
				if (tuning.custom != null)
				{
					num14 += (float)tuning.custom.CostOffset(intVec, new IntVec3(num10, 0, num11));
				}
				if (lordWalkGrid != null && !lordWalkGrid[new IntVec3(num10, 0, num11)])
				{
					num14 += (float)costOffLordWalkGrid;
				}
				int num16 = Mathf.RoundToInt(num14 + (float)calcGrid[curIndex].knownCost);
				ushort status = calcGrid[num12].status;
				if (status == statusClosedValue || status == statusOpenValue)
				{
					int num17 = 0;
					if (status == statusClosedValue)
					{
						num17 = Mathf.RoundToInt(num6);
					}
					if (calcGrid[num12].knownCost <= num16 + num17)
					{
						continue;
					}
				}
				if (flag8)
				{
					calcGrid[num12].heuristicCost = Mathf.RoundToInt((float)regionCostCalculator.GetPathCostFromDestToRegion(num12) * RegionHeuristicWeightByNodesOpened.Evaluate(num3));
					if (calcGrid[num12].heuristicCost < 0)
					{
						Log.ErrorOnce(string.Concat("Heuristic cost overflow for ", pawn.ToStringSafe(), " pathing from ", start, " to ", dest, "."), pawn.GetHashCode() ^ 0xB8DC389);
						calcGrid[num12].heuristicCost = 0;
					}
				}
				else if (status != statusClosedValue && status != statusOpenValue)
				{
					int dx = Math.Abs(num10 - x);
					int dz = Math.Abs(num11 - z);
					int num18 = GenMath.OctileDistance(dx, dz, Mathf.RoundToInt(num6), Mathf.RoundToInt(num7));
					calcGrid[num12].heuristicCost = Mathf.RoundToInt((float)num18 * num5);
				}
				int num19 = Mathf.RoundToInt(num16 + calcGrid[num12].heuristicCost);
				if (num19 < 0)
				{
					Log.ErrorOnce(string.Concat("Node cost overflow for ", pawn.ToStringSafe(), " pathing from ", start, " to ", dest, "."), pawn.GetHashCode() ^ 0x53CB9DE);
					num19 = 0;
				}
				calcGrid[num12].parentIndex = curIndex;
				calcGrid[num12].knownCost = num16;
				calcGrid[num12].status = statusOpenValue;
				calcGrid[num12].costNodeCost = num19;
				num3++;
				openList.Enqueue(num12, num19);
			}
			PfProfilerEndSample();
			num2++;
			calcGrid[curIndex].status = statusClosedValue;
			if (num3 >= num4 && flag6 && !flag8)
			{
				flag8 = true;
				regionCostCalculator.Init(cellRect, traverseParms, num6, num7, byteGrid, allowedArea, flag9, disallowedCornerIndices);
				InitStatusesAndPushStartNode(ref curIndex, start);
				num3 = 0;
				num2 = 0;
			}
		}
		Log.Warning(string.Concat(pawn, " pathing from ", start, " to ", dest, " hit search limit of ", 160000, " cells."));
		DebugDrawRichData();
		PfProfilerEndSample();
		PfProfilerEndSample();
		return PawnPath.NotFound;
	}

	public static int GetBuildingCost(Building b, TraverseParms traverseParms, Pawn pawn, PathFinderCostTuning tuning = null)
	{
		tuning = tuning ?? PathFinderCostTuning.DefaultTuning;
		int costBlockedDoor = tuning.costBlockedDoor;
		float costBlockedDoorPerHitPoint = tuning.costBlockedDoorPerHitPoint;
		if (b is Building_Door building_Door)
		{
			switch (traverseParms.mode)
			{
			case TraverseMode.NoPassClosedDoors:
			case TraverseMode.NoPassClosedDoorsOrWater:
				if (building_Door.FreePassage)
				{
					return 0;
				}
				return int.MaxValue;
			case TraverseMode.PassAllDestroyableThings:
			case TraverseMode.PassAllDestroyablePlayerOwnedThings:
			case TraverseMode.PassAllDestroyableThingsNotWater:
				if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
				{
					return building_Door.TicksToOpenNow;
				}
				if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
				{
					return 0;
				}
				if (traverseParms.mode == TraverseMode.PassAllDestroyablePlayerOwnedThings && building_Door.Faction != null && !building_Door.Faction.IsPlayer)
				{
					return int.MaxValue;
				}
				return costBlockedDoor + (int)((float)building_Door.HitPoints * costBlockedDoorPerHitPoint);
			case TraverseMode.PassDoors:
				if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
				{
					return building_Door.TicksToOpenNow;
				}
				if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
				{
					return 0;
				}
				return 150;
			case TraverseMode.ByPawn:
				if (!traverseParms.canBashDoors && building_Door.IsForbiddenToPass(pawn))
				{
					return int.MaxValue;
				}
				if (building_Door.PawnCanOpen(pawn) && !building_Door.FreePassage)
				{
					return building_Door.TicksToOpenNow;
				}
				if (building_Door.CanPhysicallyPass(pawn))
				{
					return 0;
				}
				if (traverseParms.canBashDoors)
				{
					return 300;
				}
				return int.MaxValue;
			}
		}
		else if (b.def.IsFence && traverseParms.fenceBlocked)
		{
			switch (traverseParms.mode)
			{
			case TraverseMode.ByPawn:
				if (traverseParms.canBashFences)
				{
					return 300;
				}
				return int.MaxValue;
			case TraverseMode.PassAllDestroyableThings:
			case TraverseMode.PassAllDestroyableThingsNotWater:
				return costBlockedDoor + (int)((float)b.HitPoints * costBlockedDoorPerHitPoint);
			case TraverseMode.PassAllDestroyablePlayerOwnedThings:
				if (!b.Faction.IsPlayer)
				{
					return int.MaxValue;
				}
				return costBlockedDoor + (int)((float)b.HitPoints * costBlockedDoorPerHitPoint);
			case TraverseMode.PassDoors:
			case TraverseMode.NoPassClosedDoors:
			case TraverseMode.NoPassClosedDoorsOrWater:
				return 0;
			}
		}
		else if (pawn != null)
		{
			return b.PathFindCostFor(pawn);
		}
		return 0;
	}

	public static int GetBlueprintCost(Blueprint b, Pawn pawn)
	{
		if (pawn != null)
		{
			return b.PathFindCostFor(pawn);
		}
		return 0;
	}

	public static bool IsDestroyable(Thing th)
	{
		if (th.def.useHitPoints)
		{
			return th.def.destroyable;
		}
		return false;
	}

	private bool BlocksDiagonalMovement(int index)
	{
		return BlocksDiagonalMovement(index, pathingContext, traverseParms.canBashFences);
	}

	public static bool BlocksDiagonalMovement(int x, int z, PathingContext pc, bool canBashFences)
	{
		return BlocksDiagonalMovement(pc.map.cellIndices.CellToIndex(x, z), pc, canBashFences);
	}

	public static bool BlocksDiagonalMovement(int index, PathingContext pc, bool canBashFences)
	{
		if (!pc.pathGrid.WalkableFast(index))
		{
			return true;
		}
		Building building = pc.map.edificeGrid[index];
		if (building != null)
		{
			if (building is Building_Door)
			{
				return true;
			}
			if (canBashFences && building.def.IsFence)
			{
				return true;
			}
		}
		return false;
	}

	private void DebugFlash(IntVec3 c, float colorPct, string str)
	{
		DebugFlash(c, map, colorPct, str);
	}

	private static void DebugFlash(IntVec3 c, Map map, float colorPct, string str)
	{
		map.debugDrawer.FlashCell(c, colorPct, str);
	}

	private PawnPath FinalizedPath(int finalIndex, bool usedRegionHeuristics)
	{
		PawnPath emptyPawnPath = map.pawnPathPool.GetEmptyPawnPath();
		int num = finalIndex;
		while (true)
		{
			int parentIndex = calcGrid[num].parentIndex;
			emptyPawnPath.AddNode(map.cellIndices.IndexToCell(num));
			if (num == parentIndex)
			{
				break;
			}
			num = parentIndex;
		}
		emptyPawnPath.SetupFound(calcGrid[finalIndex].knownCost, usedRegionHeuristics);
		return emptyPawnPath;
	}

	private void InitStatusesAndPushStartNode(ref int curIndex, IntVec3 start)
	{
		statusOpenValue += 2;
		statusClosedValue += 2;
		if (statusClosedValue >= 65435)
		{
			ResetStatuses();
		}
		curIndex = cellIndices.CellToIndex(start);
		calcGrid[curIndex].knownCost = 0;
		calcGrid[curIndex].heuristicCost = 0;
		calcGrid[curIndex].costNodeCost = 0;
		calcGrid[curIndex].parentIndex = curIndex;
		calcGrid[curIndex].status = statusOpenValue;
		openList.Clear();
		openList.Enqueue(curIndex, 0);
	}

	private void ResetStatuses()
	{
		int num = calcGrid.Length;
		for (int i = 0; i < num; i++)
		{
			calcGrid[i].status = 0;
		}
		statusOpenValue = 1;
		statusClosedValue = 2;
	}

	[Conditional("PFPROFILE")]
	private void PfProfilerBeginSample(string s)
	{
	}

	[Conditional("PFPROFILE")]
	private void PfProfilerEndSample()
	{
	}

	private void DebugDrawRichData()
	{
	}

	private float DetermineHeuristicStrength(Pawn pawn, IntVec3 start, LocalTargetInfo dest)
	{
		if (pawn != null && pawn.IsNonMutantAnimal)
		{
			return 1.75f;
		}
		float lengthHorizontal = (start - dest.Cell).LengthHorizontal;
		return Mathf.RoundToInt(NonRegionBasedHeuristicStrengthHuman_DistanceCurve.Evaluate(lengthHorizontal));
	}

	private CellRect CalculateDestinationRect(LocalTargetInfo dest, PathEndMode peMode)
	{
		CellRect result = ((dest.HasThing && peMode != PathEndMode.OnCell) ? dest.Thing.OccupiedRect() : CellRect.SingleCell(dest.Cell));
		if (peMode == PathEndMode.Touch)
		{
			result = result.ExpandedBy(1);
		}
		return result;
	}

	private Area GetAllowedArea(Pawn pawn)
	{
		if (pawn != null && pawn.playerSettings != null && !pawn.Drafted && ForbidUtility.CaresAboutForbidden(pawn, cellTarget: true))
		{
			Area area = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
			if (area != null && area.TrueCount <= 0)
			{
				area = null;
			}
			return area;
		}
		return null;
	}

	private BoolGrid GetLordWalkGrid(Pawn pawn)
	{
		return BreachingUtility.BreachingGridFor(pawn)?.WalkGrid;
	}

	[Obsolete]
	private void CalculateAndAddDisallowedCorners(PathEndMode peMode, CellRect destinationRect)
	{
		CalculateAndAddDisallowedCorners_NewTemp(peMode, destinationRect, null);
	}

	private void CalculateAndAddDisallowedCorners_NewTemp(PathEndMode peMode, CellRect destinationRect, LocalTargetInfo? dest)
	{
		disallowedCornerIndices.Clear();
		if (peMode == PathEndMode.Touch)
		{
			int minX = destinationRect.minX;
			int minZ = destinationRect.minZ;
			int maxX = destinationRect.maxX;
			int maxZ = destinationRect.maxZ;
			if (!IsCornerTouchAllowed_NewTemp(dest, minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
			{
				disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, minZ));
			}
			if (!IsCornerTouchAllowed_NewTemp(dest, minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
			{
				disallowedCornerIndices.Add(map.cellIndices.CellToIndex(minX, maxZ));
			}
			if (!IsCornerTouchAllowed_NewTemp(dest, maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
			{
				disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, maxZ));
			}
			if (!IsCornerTouchAllowed_NewTemp(dest, maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
			{
				disallowedCornerIndices.Add(map.cellIndices.CellToIndex(maxX, minZ));
			}
		}
	}

	[Obsolete]
	private bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
	{
		return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, pathingContext);
	}

	private bool IsCornerTouchAllowed_NewTemp(LocalTargetInfo? dest, int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
	{
		return TouchPathEndModeUtility.IsCornerTouchAllowed_NewTemp(dest, cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, pathingContext);
	}
}
