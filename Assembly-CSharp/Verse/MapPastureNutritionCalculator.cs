using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace Verse;

public class MapPastureNutritionCalculator
{
	public class NutritionPerDayPerQuadrum
	{
		public float[] quadrum = new float[4];

		public float ForQuadrum(Quadrum q)
		{
			return quadrum[(uint)q];
		}

		public void AddFrom(NutritionPerDayPerQuadrum other)
		{
			quadrum[0] += other.quadrum[0];
			quadrum[3] += other.quadrum[3];
			quadrum[1] += other.quadrum[1];
			quadrum[2] += other.quadrum[2];
		}
	}

	public MapPlantGrowthRateCalculator plantGrowthRateCalculator;

	public BiomeDef biome;

	public int tile;

	public float mapChanceRegrowth;

	private Dictionary<ThingDef, Dictionary<TerrainDef, NutritionPerDayPerQuadrum>> cachedSeasonalDetailed = new Dictionary<ThingDef, Dictionary<TerrainDef, NutritionPerDayPerQuadrum>>();

	private Dictionary<TerrainDef, NutritionPerDayPerQuadrum> cachedSeasonalByTerrain = new Dictionary<TerrainDef, NutritionPerDayPerQuadrum>();

	public void Reset(Map map)
	{
		Reset(map.Tile, map.Biome, map.wildPlantSpawner.CachedChanceFromDensity, map.plantGrowthRateCalculator);
	}

	public void Reset(int tile, BiomeDef biome, float newMapChanceRegrowth, MapPlantGrowthRateCalculator growthRateCalculator)
	{
		newMapChanceRegrowth = (float)Math.Round(newMapChanceRegrowth, 7);
		if (this.tile != tile || !Mathf.Approximately(mapChanceRegrowth, newMapChanceRegrowth))
		{
			this.tile = tile;
			this.biome = biome;
			mapChanceRegrowth = newMapChanceRegrowth;
			plantGrowthRateCalculator = growthRateCalculator;
			cachedSeasonalDetailed.Clear();
			cachedSeasonalByTerrain.Clear();
		}
	}

	public NutritionPerDayPerQuadrum CalculateAverageNutritionPerDay(TerrainDef terrain)
	{
		if (!cachedSeasonalByTerrain.TryGetValue(terrain, out var value))
		{
			value = new NutritionPerDayPerQuadrum();
			foreach (ThingDef wildGrazingPlant in plantGrowthRateCalculator.WildGrazingPlants)
			{
				NutritionPerDayPerQuadrum other = CalculateAverageNutritionPerDay(wildGrazingPlant, terrain);
				value.AddFrom(other);
			}
			cachedSeasonalByTerrain.Add(terrain, value);
		}
		return value;
	}

	private NutritionPerDayPerQuadrum CalculateAverageNutritionPerDay(ThingDef plantDef, TerrainDef terrain)
	{
		if (!cachedSeasonalDetailed.TryGetValue(plantDef, out var value))
		{
			value = new Dictionary<TerrainDef, NutritionPerDayPerQuadrum>();
			cachedSeasonalDetailed.Add(plantDef, value);
		}
		if (!value.TryGetValue(terrain, out var value2))
		{
			value2 = new NutritionPerDayPerQuadrum();
			value.Add(terrain, value2);
			value2.quadrum[0] = GetAverageNutritionPerDay(Quadrum.Aprimay, plantDef, terrain);
			value2.quadrum[3] = GetAverageNutritionPerDay(Quadrum.Decembary, plantDef, terrain);
			value2.quadrum[1] = GetAverageNutritionPerDay(Quadrum.Jugust, plantDef, terrain);
			value2.quadrum[2] = GetAverageNutritionPerDay(Quadrum.Septober, plantDef, terrain);
		}
		return value2;
	}

	public float GetAverageNutritionPerDayToday(TerrainDef terrainDef)
	{
		float num = 0f;
		foreach (ThingDef wildGrazingPlant in plantGrowthRateCalculator.WildGrazingPlants)
		{
			num += GetAverageNutritionPerDayToday(wildGrazingPlant, terrainDef);
		}
		return num;
	}

	private float GetAverageNutritionPerDayToday(ThingDef plantDef, TerrainDef terrainDef)
	{
		if (!(terrainDef.fertility > 0f))
		{
			return 0f;
		}
		int ticksAbs = Find.TickManager.TicksAbs;
		int nowTicks = ticksAbs - ticksAbs % 60000;
		float growthRate = plantGrowthRateCalculator.GrowthRateForDay(nowTicks, plantDef, terrainDef);
		return ComputeNutritionProducedPerDay(plantDef, growthRate);
	}

	public float GetAverageNutritionPerDay(Quadrum quadrum, TerrainDef terrainDef)
	{
		float num = 0f;
		foreach (ThingDef wildGrazingPlant in plantGrowthRateCalculator.WildGrazingPlants)
		{
			num += GetAverageNutritionPerDay(quadrum, wildGrazingPlant, terrainDef);
		}
		return num;
	}

	public float GetAverageNutritionPerDay(Quadrum quadrum, ThingDef plantDef, TerrainDef terrainDef)
	{
		float growthRate = plantGrowthRateCalculator.QuadrumGrowthRateFor(quadrum, plantDef, terrainDef);
		return ComputeNutritionProducedPerDay(plantDef, growthRate);
	}

	private float ComputeNutritionProducedPerDay(ThingDef plantDef, float growthRate)
	{
		return SimplifiedPastureNutritionSimulator.NutritionProducedPerDay(biome, plantDef, growthRate, mapChanceRegrowth);
	}
}
