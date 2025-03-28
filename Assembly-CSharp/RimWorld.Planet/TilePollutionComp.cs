using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld.Planet;

public class TilePollutionComp : WorldComponent
{
	private const float WorldFractionCheckPerTick = 1.6666667E-05f;

	private const int WorldPollutionReductionInterval = 60000;

	private const float WorldPollutionReductionFactor = 0.0001f;

	private int cycleIndex;

	private float totalWorldPollution = -1f;

	private float currentWorldPollution;

	public float TotalWorldPollution
	{
		get
		{
			if (totalWorldPollution < 0f)
			{
				for (int i = 0; i < world.grid.TilesCount; i++)
				{
					totalWorldPollution += world.grid[i].pollution;
				}
			}
			return totalWorldPollution;
		}
	}

	public TilePollutionComp(World world)
		: base(world)
	{
	}

	public override void WorldComponentTick()
	{
		int tilesCount = world.grid.TilesCount;
		int num = Mathf.CeilToInt(1.6666667E-05f * (float)tilesCount);
		for (int i = 0; i < num; i++)
		{
			if (!world.grid.InBounds(cycleIndex))
			{
				totalWorldPollution = currentWorldPollution;
				cycleIndex = 0;
				currentWorldPollution = 0f;
				break;
			}
			currentWorldPollution += world.grid[cycleIndex].pollution;
			cycleIndex++;
		}
		if (Find.TickManager.TicksGame % 60000 == 0)
		{
			DoPollutionReduction();
		}
	}

	private void DoPollutionReduction()
	{
		float num = TotalWorldPollution * 0.0001f;
		List<int> tiles = Find.World.tilesInRandomOrder.Tiles;
		int num2 = Rand.Range(0, tiles.Count);
		for (int i = 0; i < tiles.Count; i++)
		{
			int num3 = tiles[(i + num2) % tiles.Count];
			if (!(world.grid[num3].pollution <= 0f))
			{
				float num4 = Mathf.Min(num, world.grid[num3].pollution);
				world.grid[num3].pollution -= num4;
				num -= num4;
				Find.World.renderer.Notify_TilePollutionChanged(num3);
				if (num <= 0f)
				{
					break;
				}
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref cycleIndex, "cycleIndex", 0);
		Scribe_Values.Look(ref totalWorldPollution, "totalWorldPollution", 0f);
		Scribe_Values.Look(ref currentWorldPollution, "currentWorldPollution", 0f);
	}
}
