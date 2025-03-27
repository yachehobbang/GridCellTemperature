using System.Collections.Generic;
using UnityEngine;

namespace Verse;

public class GlowFlooder
{
	private struct GlowFloodCell
	{
		public int intDist;

		public uint status;
	}

	private class CompareGlowFlooderLightSquares : IComparer<IntVec3>
	{
		private GlowFloodCell[,] grid;

		public CompareGlowFlooderLightSquares(GlowFloodCell[,] grid)
		{
			this.grid = grid;
		}

		public int Compare(IntVec3 a, IntVec3 b)
		{
			return grid[a.x, a.z].intDist.CompareTo(grid[b.x, b.z].intDist);
		}
	}

	private Map map;

	private GlowFloodCell[,] calcGrid;

	private FastPriorityQueue<IntVec3> openSet;

	private uint statusUnseenValue;

	private uint statusOpenValue = 1u;

	private uint statusFinalizedValue = 2u;

	private int mapSizeX;

	private int mapSizeZ;

	private CompGlower glower;

	private Color32[,] localGlowGrid;

	private IntVec3 localGlowGridStartPos;

	private float attenLinearSlope;

	private Thing[] blockers = new Thing[8];

	private static readonly sbyte[,] Directions = new sbyte[8, 2]
	{
		{ 0, -1 },
		{ 1, 0 },
		{ 0, 1 },
		{ -1, 0 },
		{ 1, -1 },
		{ 1, 1 },
		{ -1, 1 },
		{ -1, -1 }
	};

	public GlowFlooder(Map map)
	{
		this.map = map;
		mapSizeX = map.Size.x;
		mapSizeZ = map.Size.z;
		calcGrid = new GlowFloodCell[mapSizeX, mapSizeZ];
		openSet = new FastPriorityQueue<IntVec3>(new CompareGlowFlooderLightSquares(calcGrid));
	}

	public void AddFloodGlowFor(CompGlower theGlower, Color32[,] localGlowGrid, IntVec3 localGlowGridStartPos)
	{
		this.localGlowGrid = localGlowGrid;
		this.localGlowGridStartPos = localGlowGridStartPos;
		glower = theGlower;
		attenLinearSlope = -1f / theGlower.GlowRadius;
		EdificeGrid edificeGrid = map.edificeGrid;
		IntVec3 position = theGlower.parent.Position;
		int num = Mathf.RoundToInt(glower.GlowRadius * 100f);
		IntVec3 intVec = position;
		int num2 = localGlowGridStartPos.x + localGlowGrid.GetLength(0) - 1;
		int num3 = localGlowGridStartPos.z + localGlowGrid.GetLength(1) - 1;
		InitStatusesAndPushStartNode(position);
		while (openSet.Count != 0)
		{
			intVec = openSet.Pop();
			calcGrid[intVec.x, intVec.z].status = statusFinalizedValue;
			SetGlowGridFromDist(intVec.x, intVec.z);
			for (int i = 0; i < 8; i++)
			{
				uint num4 = (uint)(intVec.x + Directions[i, 0]);
				uint num5 = (uint)(intVec.z + Directions[i, 1]);
				if (num4 < localGlowGridStartPos.x || num5 < localGlowGridStartPos.z || num4 > num2 || num5 > num3 || num4 >= mapSizeX || num5 >= mapSizeZ)
				{
					continue;
				}
				int num6 = (int)num4;
				int num7 = (int)num5;
				if (calcGrid[num6, num7].status == statusFinalizedValue)
				{
					continue;
				}
				blockers[i] = edificeGrid[new IntVec3(num6, 0, num7)];
				if (blockers[i] != null)
				{
					if (blockers[i].def.blockLight)
					{
						continue;
					}
					blockers[i] = null;
				}
				int num8 = ((i >= 4) ? 141 : 100);
				int num9 = calcGrid[intVec.x, intVec.z].intDist + num8;
				if (num9 > num)
				{
					continue;
				}
				switch (i)
				{
				case 4:
					if (blockers[0] != null && blockers[1] != null)
					{
						continue;
					}
					break;
				case 5:
					if (blockers[1] != null && blockers[2] != null)
					{
						continue;
					}
					break;
				case 6:
					if (blockers[2] != null && blockers[3] != null)
					{
						continue;
					}
					break;
				case 7:
					if (blockers[0] != null && blockers[3] != null)
					{
						continue;
					}
					break;
				}
				if (calcGrid[num6, num7].status <= statusUnseenValue)
				{
					calcGrid[num6, num7].intDist = 999999;
					calcGrid[num6, num7].status = statusOpenValue;
				}
				if (num9 < calcGrid[num6, num7].intDist)
				{
					calcGrid[num6, num7].intDist = num9;
					calcGrid[num6, num7].status = statusOpenValue;
					openSet.Push(new IntVec3(num6, 0, num7));
				}
			}
		}
	}

	private void InitStatusesAndPushStartNode(IntVec3 start)
	{
		statusUnseenValue += 3u;
		statusOpenValue += 3u;
		statusFinalizedValue += 3u;
		openSet.Clear();
		calcGrid[start.x, start.z].intDist = 100;
		openSet.Clear();
		openSet.Push(start);
	}

	private void SetGlowGridFromDist(int x, int z)
	{
		float num = (float)calcGrid[x, z].intDist / 100f;
		ColorInt colorInt = default(ColorInt);
		if (num <= glower.GlowRadius)
		{
			float b = 1f / (num * num);
			float num2 = Mathf.Lerp(1f + attenLinearSlope * num, b, 0.4f);
			colorInt = glower.GlowColor * num2;
			colorInt.a = 0;
		}
		if (colorInt.r > 0 || colorInt.g > 0 || colorInt.b > 0)
		{
			colorInt.ClampToNonNegative();
			colorInt.a = (int)num;
			localGlowGrid[x - localGlowGridStartPos.x, z - localGlowGridStartPos.z] = colorInt.ProjectToColor32;
		}
	}
}
