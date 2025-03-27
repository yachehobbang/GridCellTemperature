using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Verse;

public static class WorldPollutionUtility
{
	public const int NearbyPollutionTileRadius = 4;

	public static readonly SimpleCurve NearbyPollutionOverDistanceCurve = new SimpleCurve
	{
		new CurvePoint(0f, 1f),
		new CurvePoint(2f, 1f),
		new CurvePoint(3f, 0.5f),
		new CurvePoint(4f, 0.5f)
	};

	private static HashSet<Faction> tmpSeenFactions = new HashSet<Faction>();

	private static List<int> tmpTileNeighbors = new List<int>();

	private static List<int> tmpPossiblePollutableTiles = new List<int>();

	public static void PolluteWorldAtTile(int root, float pollutionAmount)
	{
		if (root == -1)
		{
			return;
		}
		tmpSeenFactions.Clear();
		int num = FindBestTileToPollute(root);
		if (num != -1)
		{
			Tile tile = Find.WorldGrid[num];
			_ = tile.pollution;
			float num2 = tile.pollution + pollutionAmount;
			float num3 = num2 - 1f;
			tile.pollution = Mathf.Clamp01(num2);
			MapParent mapParent = Find.WorldObjects.MapParentAt(num);
			if (mapParent == null || !mapParent.HasMap)
			{
				Vector2 vector = Find.WorldGrid.LongLatOf(num);
				string text = vector.y.ToStringLatitude() + " / " + vector.x.ToStringLongitude();
				Messages.Message("MessageWorldTilePollutionChanged".Translate(pollutionAmount.ToStringPercent(), text), new LookTargets(num), MessageTypeDefOf.NegativeEvent, historical: false);
			}
			Map map = Current.Game.FindMap(num);
			if (map != null)
			{
				PollutionUtility.PolluteMapToPercent(map, tile.pollution);
			}
			Find.World.renderer.Notify_TilePollutionChanged(num);
			tmpSeenFactions.Clear();
			if (num3 > 0f)
			{
				PolluteWorldAtTile(num, num3);
			}
		}
	}

	public static int FindBestTileToPollute(int root)
	{
		if (root == -1)
		{
			return -1;
		}
		World world = Find.World;
		WorldGrid grid = world.grid;
		_ = grid[root];
		if (CanPollute(root))
		{
			return root;
		}
		tmpPossiblePollutableTiles.Clear();
		int bestDistance = int.MaxValue;
		Find.WorldFloodFiller.FloodFill(root, (int x) => !CanPollute(x), delegate(int t, int d)
		{
			tmpTileNeighbors.Clear();
			grid.GetTileNeighbors(t, tmpTileNeighbors);
			for (int i = 0; i < tmpTileNeighbors.Count; i++)
			{
				if (CanPollute(tmpTileNeighbors[i]) && !tmpPossiblePollutableTiles.Contains(tmpTileNeighbors[i]))
				{
					int num = Mathf.RoundToInt(grid.ApproxDistanceInTiles(root, tmpTileNeighbors[i]));
					if (num <= bestDistance)
					{
						bestDistance = num;
						tmpPossiblePollutableTiles.Add(tmpTileNeighbors[i]);
						tmpPossiblePollutableTiles.RemoveAll((int u) => Mathf.RoundToInt(grid.ApproxDistanceInTiles(root, u)) > bestDistance);
					}
				}
			}
			return false;
		});
		int found = (from t in tmpPossiblePollutableTiles
			orderby grid[t].PollutionLevel(), grid[t].pollution descending
			select t).FirstOrFallback(-1);
		tmpPossiblePollutableTiles.RemoveAll((int t) => grid[t].PollutionLevel() > grid[found].PollutionLevel() && grid[t].pollution < grid[found].pollution);
		found = tmpPossiblePollutableTiles.RandomElement();
		tmpPossiblePollutableTiles.Clear();
		tmpTileNeighbors.Clear();
		return found;
		bool CanPollute(int t)
		{
			if (grid[t].biome.allowPollution)
			{
				return grid[t].pollution < 1f;
			}
			return false;
		}
	}

	public static float CalculateNearbyPollutionScore(int tileId)
	{
		int maxTilesToProcess = Find.WorldGrid.TilesNumWithinTraversalDistance(4);
		float nearbyPollutionScore = 0f;
		Find.WorldFloodFiller.FloodFill(tileId, (int x) => true, delegate(int tile, int dist)
		{
			nearbyPollutionScore += NearbyPollutionOverDistanceCurve.Evaluate(Mathf.RoundToInt(dist)) * Find.WorldGrid[tile].pollution;
			return false;
		}, maxTilesToProcess);
		return nearbyPollutionScore;
	}
}
