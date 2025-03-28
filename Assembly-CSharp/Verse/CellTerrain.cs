using RimWorld;
using UnityEngine;

namespace Verse;

public struct CellTerrain
{
	public TerrainDef def;

	public bool polluted;

	public float snowCoverage;

	public ColorDef color;

	public CellTerrain(TerrainDef def, bool polluted, float snowCoverage, ColorDef color)
	{
		this.def = def;
		this.polluted = polluted;
		this.snowCoverage = snowCoverage;
		this.color = color;
	}

	public override bool Equals(object obj)
	{
		if (obj is CellTerrain terrain)
		{
			return Equals(terrain);
		}
		return false;
	}

	public bool Equals(CellTerrain terrain)
	{
		if (terrain.def == def && terrain.color == color && terrain.polluted == polluted)
		{
			return Mathf.Abs(terrain.snowCoverage - snowCoverage) < float.Epsilon;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(Gen.HashCombine(0, def), polluted), snowCoverage), color);
	}
}
