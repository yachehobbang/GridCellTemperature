using System.Collections.Generic;
using Verse;

namespace RimWorld.Planet;

public class Tile : IExposable
{
	public struct RoadLink
	{
		public int neighbor;

		public RoadDef road;
	}

	public struct RiverLink
	{
		public int neighbor;

		public RiverDef river;
	}

	public const int Invalid = -1;

	public BiomeDef biome;

	public float elevation = 100f;

	public Hilliness hilliness;

	public float temperature = 20f;

	public float rainfall;

	public float swampiness;

	public WorldFeature feature;

	public float pollution;

	public List<RoadLink> potentialRoads;

	public List<RiverLink> potentialRivers;

	public bool WaterCovered => elevation <= 0f;

	public List<RoadLink> Roads
	{
		get
		{
			if (!biome.allowRoads)
			{
				return null;
			}
			return potentialRoads;
		}
	}

	public List<RiverLink> Rivers
	{
		get
		{
			if (!biome.allowRivers)
			{
				return null;
			}
			return potentialRivers;
		}
	}

	public override string ToString()
	{
		return string.Concat("(", biome, " elev=", elevation, "m hill=", hilliness, " temp=", temperature, "°C rain=", rainfall, "mm swampiness=", swampiness.ToStringPercent(), " potentialRoads=", (potentialRoads != null) ? potentialRoads.Count : 0, " (allowed=", biome.allowRoads.ToString(), ") potentialRivers=", (potentialRivers != null) ? potentialRivers.Count : 0, " (allowed=", biome.allowRivers.ToString(), "))");
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref biome, "biome");
		Scribe_Values.Look(ref elevation, "elevation", 100f);
		Scribe_Values.Look(ref hilliness, "hilliness", Hilliness.Undefined);
		Scribe_Values.Look(ref temperature, "temperature", 20f);
		Scribe_Values.Look(ref rainfall, "rainfall", 0f);
		Scribe_Values.Look(ref swampiness, "swampiness", 0f);
		Scribe_Deep.Look(ref feature, "feature");
		Scribe_Values.Look(ref pollution, "pollution", 0f);
	}
}
