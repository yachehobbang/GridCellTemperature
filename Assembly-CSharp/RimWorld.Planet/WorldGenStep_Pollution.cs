using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace RimWorld.Planet;

public class WorldGenStep_Pollution : WorldGenStep
{
	private const float MinPollution = 0.25f;

	private const float MaxPollution = 1f;

	private const float PerlinFrequency = 0.1f;

	private List<int> tmpTiles = new List<int>();

	private Dictionary<int, float> tmpTileNoise = new Dictionary<int, float>();

	public override int SeedPart => 759372056;

	public override void GenerateFresh(string seed)
	{
		WorldGrid worldGrid = Find.WorldGrid;
		float pollution = Find.World.info.pollution;
		if (pollution <= 0f)
		{
			return;
		}
		Perlin perlin = new Perlin(0.10000000149011612, 2.0, 0.5, 6, seed.GetHashCode(), QualityMode.Medium);
		tmpTiles.Clear();
		tmpTileNoise.Clear();
		for (int i = 0; i < worldGrid.TilesCount; i++)
		{
			if (worldGrid[i].biome.allowPollution)
			{
				tmpTiles.Add(i);
				tmpTileNoise.Add(i, perlin.GetValue(worldGrid.GetTileCenter(i)));
			}
		}
		tmpTiles.SortByDescending((int t) => tmpTileNoise[t]);
		int num = Mathf.RoundToInt((float)tmpTiles.Count * pollution);
		for (int j = 0; j < num; j++)
		{
			worldGrid[tmpTiles[j]].pollution = Mathf.Lerp(0.25f, 1f, tmpTileNoise[tmpTiles[j]]);
		}
		tmpTiles.Clear();
		tmpTileNoise.Clear();
	}
}
