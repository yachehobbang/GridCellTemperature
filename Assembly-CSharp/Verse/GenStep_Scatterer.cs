using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace Verse;

public abstract class GenStep_Scatterer : GenStep
{
	public int count = -1;

	public FloatRange countPer10kCellsRange = FloatRange.Zero;

	public bool nearPlayerStart;

	public bool nearMapCenter;

	public float minSpacing = 10f;

	public bool spotMustBeStandable;

	public int minDistToPlayerStart;

	public float minDistToPlayerStartPct;

	public int minEdgeDist;

	public float minEdgeDistPct;

	public int extraNoBuildEdgeDist;

	public List<ScattererValidator> validators = new List<ScattererValidator>();

	public List<ScattererValidator> fallbackValidators = new List<ScattererValidator>();

	public bool allowInWaterBiome = true;

	public bool allowFoggedPositions = true;

	public bool allowRoofed = true;

	public bool onlyOnStartingMap;

	public float minPollution;

	public bool allowMechanoidDatacoreReadOrLost = true;

	public bool warnOnFail = true;

	[Unsaved(false)]
	protected List<IntVec3> usedSpots = new List<IntVec3>();

	[Unsaved(false)]
	private bool useFallback;

	private const int ScatterNearPlayerRadius = 20;

	private bool HasFallbackValidators
	{
		get
		{
			if (fallbackValidators != null)
			{
				return fallbackValidators.Count > 0;
			}
			return false;
		}
	}

	public override void Generate(Map map, GenStepParams parms)
	{
		if (ShouldSkipMap(map))
		{
			return;
		}
		int num = CalculateFinalCount(map);
		for (int i = 0; i < num; i++)
		{
			if (!TryFindScatterCell(map, out var result))
			{
				if (HasFallbackValidators)
				{
					useFallback = true;
					TryFindScatterCell(map, out result);
				}
				return;
			}
			ScatterAt(result, map, parms);
			usedSpots.Add(result);
		}
		usedSpots.Clear();
	}

	protected virtual bool ShouldSkipMap(Map map)
	{
		if (!allowInWaterBiome && map.TileInfo.WaterCovered)
		{
			return true;
		}
		if (onlyOnStartingMap && Find.GameInfo.startingTile != map.Tile)
		{
			return true;
		}
		if (ModsConfig.BiotechActive && map.TileInfo.pollution < minPollution)
		{
			return true;
		}
		if (ModsConfig.BiotechActive && Find.History.mechanoidDatacoreReadOrLost && !allowMechanoidDatacoreReadOrLost)
		{
			return true;
		}
		return false;
	}

	protected virtual bool TryFindScatterCell(Map map, out IntVec3 result)
	{
		if (nearMapCenter)
		{
			if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => CanScatterAt(x, map), map, out result))
			{
				return true;
			}
		}
		else
		{
			if (nearPlayerStart)
			{
				result = CellFinder.RandomClosewalkCellNear(MapGenerator.PlayerStartSpot, map, 20, (IntVec3 x) => CanScatterAt(x, map));
				return true;
			}
			if (CellFinderLoose.TryFindRandomNotEdgeCellWith(5, (IntVec3 x) => CanScatterAt(x, map), map, out result))
			{
				return true;
			}
		}
		if (warnOnFail)
		{
			if (HasFallbackValidators && !useFallback)
			{
				Log.Warning("Scatterer " + ToString() + " could not find cell to generate at, trying fallback validators.");
			}
			else
			{
				Log.Warning("Scatterer " + ToString() + " could not find cell to generate at.");
			}
		}
		return false;
	}

	protected abstract void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1);

	protected virtual bool CanScatterAt(IntVec3 loc, Map map)
	{
		if (extraNoBuildEdgeDist > 0 && loc.CloseToEdge(map, extraNoBuildEdgeDist + 10))
		{
			return false;
		}
		if (minEdgeDist > 0 && loc.CloseToEdge(map, minEdgeDist))
		{
			return false;
		}
		if (minEdgeDistPct > 0f && loc.CloseToEdge(map, (int)(minEdgeDistPct * (float)Mathf.Min(map.Size.x, map.Size.z))))
		{
			return false;
		}
		if (NearUsedSpot(loc, minSpacing))
		{
			return false;
		}
		if (minDistToPlayerStart > 0 && (map.Center - loc).LengthHorizontalSquared < minDistToPlayerStart * minDistToPlayerStart)
		{
			return false;
		}
		if (minDistToPlayerStartPct > 0f && (map.Center - loc).LengthHorizontal < minDistToPlayerStartPct * (float)Mathf.Min(map.Size.x, map.Size.z))
		{
			return false;
		}
		if (spotMustBeStandable && !loc.Standable(map))
		{
			return false;
		}
		if (!allowFoggedPositions && loc.Fogged(map))
		{
			return false;
		}
		if (!allowRoofed && loc.Roofed(map))
		{
			return false;
		}
		if (useFallback)
		{
			if (fallbackValidators != null)
			{
				for (int i = 0; i < fallbackValidators.Count; i++)
				{
					if (!fallbackValidators[i].Allows(loc, map))
					{
						return false;
					}
				}
			}
		}
		else if (validators != null)
		{
			for (int j = 0; j < validators.Count; j++)
			{
				if (!validators[j].Allows(loc, map))
				{
					return false;
				}
			}
		}
		return true;
	}

	protected bool NearUsedSpot(IntVec3 c, float dist)
	{
		for (int i = 0; i < usedSpots.Count; i++)
		{
			if ((float)(usedSpots[i] - c).LengthHorizontalSquared <= dist * dist)
			{
				return true;
			}
		}
		return false;
	}

	protected int CalculateFinalCount(Map map)
	{
		if (count < 0)
		{
			return CountFromPer10kCells(countPer10kCellsRange.RandomInRange, map);
		}
		return count;
	}

	public static int CountFromPer10kCells(float countPer10kCells, Map map, int mapSize = -1)
	{
		if (mapSize < 0)
		{
			mapSize = map.Size.x;
		}
		int num = Mathf.RoundToInt(10000f / countPer10kCells);
		return Mathf.RoundToInt((float)(mapSize * mapSize) / (float)num);
	}

	public void ForceScatterAt(IntVec3 loc, Map map)
	{
		ScatterAt(loc, map, default(GenStepParams));
	}
}
