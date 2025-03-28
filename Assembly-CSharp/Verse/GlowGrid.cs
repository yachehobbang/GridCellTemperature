using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;

namespace Verse;

public sealed class GlowGrid
{
	private class Light
	{
		public CompGlower glower;

		public Color32[,] localGlowGrid;

		public IntVec3 localGlowGridStartPos;

		public CellRect AffectedRect => new CellRect(localGlowGridStartPos.x, localGlowGridStartPos.z, localGlowGrid.GetLength(0), localGlowGrid.GetLength(1));
	}

	private Map map;

	private Color32?[] cachedAccumulatedGlow;

	private Color32?[] cachedAccumulatedGlowNoCavePlants;

	private List<Light> lights = new List<Light>();

	private HashSet<CompGlower> litGlowers = new HashSet<CompGlower>();

	private List<IntVec3> dirtyCells = new List<IntVec3>();

	public const int AlphaOfOverlit = 1;

	private const float GameGlowLitThreshold = 0.3f;

	private const float GameGlowOverlitThreshold = 0.9f;

	private const float GroundGameGlowFactor = 3.6f;

	private const float MaxGameGlowFromNonOverlitGroundLights = 0.5f;

	private HashSet<CompGlower> affectedLightsTemp = new HashSet<CompGlower>();

	public GlowGrid(Map map)
	{
		this.map = map;
		cachedAccumulatedGlow = new Color32?[map.cellIndices.NumGridCells];
		cachedAccumulatedGlowNoCavePlants = new Color32?[map.cellIndices.NumGridCells];
	}

	private Color32 GetAccumulatedGlowAt(IntVec3 c, bool ignoreCavePlants = false)
	{
		return GetAccumulatedGlowAt(map.cellIndices.CellToIndex(c), ignoreCavePlants);
	}

	private Color32 GetAccumulatedGlowAt(int index, bool ignoreCavePlants = false)
	{
		Color32?[] array = (ignoreCavePlants ? cachedAccumulatedGlowNoCavePlants : cachedAccumulatedGlow);
		if (!array[index].HasValue)
		{
			Color32 color = default(Color32);
			IntVec3 c = map.cellIndices.IndexToCell(index);
			for (int i = 0; i < lights.Count; i++)
			{
				if (lights[i].AffectedRect.Contains(c) && (!ignoreCavePlants || lights[i].glower.parent.def.category != ThingCategory.Plant || !lights[i].glower.parent.def.plant.cavePlant))
				{
					color = CombineColors(color, lights[i].localGlowGrid[c.x - lights[i].localGlowGridStartPos.x, c.z - lights[i].localGlowGridStartPos.z], lights[i].glower);
				}
			}
			array[index] = color;
		}
		return array[index].Value;
	}

	public Color32 VisualGlowAt(int index)
	{
		return GetAccumulatedGlowAt(index);
	}

	public Color32 VisualGlowAt(IntVec3 c)
	{
		return GetAccumulatedGlowAt(c);
	}

	public float GroundGlowAt(IntVec3 c, bool ignoreCavePlants = false, bool ignoreSky = false)
	{
		float num = 0f;
		if (!ignoreSky && !map.roofGrid.Roofed(c))
		{
			num = map.skyManager.CurSkyGlow;
			if (num == 1f)
			{
				return num;
			}
		}
		Color32 accumulatedGlowAt = GetAccumulatedGlowAt(c, ignoreCavePlants);
		if (accumulatedGlowAt.a == 1)
		{
			return 1f;
		}
		float b = (float)Mathf.Max(Mathf.Max(accumulatedGlowAt.r, accumulatedGlowAt.g), accumulatedGlowAt.b) / 255f * 3.6f;
		b = Mathf.Min(0.5f, b);
		return Mathf.Max(num, b);
	}

	public PsychGlow PsychGlowAt(IntVec3 c)
	{
		return PsychGlowAtGlow(GroundGlowAt(c));
	}

	public static PsychGlow PsychGlowAtGlow(float glow)
	{
		if (glow > 0.9f)
		{
			return PsychGlow.Overlit;
		}
		if (glow > 0.3f)
		{
			return PsychGlow.Lit;
		}
		return PsychGlow.Dark;
	}

	public void RegisterGlower(CompGlower newGlow)
	{
		if (!litGlowers.Add(newGlow))
		{
			return;
		}
		bool flag = newGlow.parent.def.category == ThingCategory.Plant && newGlow.parent.def.plant.cavePlant;
		lights.Add(new Light
		{
			glower = newGlow,
			localGlowGrid = new Color32[Mathf.CeilToInt(newGlow.GlowRadius * 2f + 1f), Mathf.CeilToInt(newGlow.GlowRadius * 2f + 1f)],
			localGlowGridStartPos = newGlow.parent.Position - new IntVec3(Mathf.CeilToInt(newGlow.GlowRadius), 0, Mathf.CeilToInt(newGlow.GlowRadius))
		});
		map.glowFlooder.AddFloodGlowFor(newGlow, lights[lights.Count - 1].localGlowGrid, lights[lights.Count - 1].localGlowGridStartPos);
		foreach (IntVec3 item in lights[lights.Count - 1].AffectedRect.ClipInsideMap(map))
		{
			cachedAccumulatedGlow[map.cellIndices.CellToIndex(item)] = null;
			if (!flag)
			{
				cachedAccumulatedGlowNoCavePlants[map.cellIndices.CellToIndex(item)] = null;
			}
			map.mapDrawer.MapMeshDirty(item, MapMeshFlagDefOf.GroundGlow);
		}
	}

	public void DeRegisterGlower(CompGlower oldGlow)
	{
		if (!litGlowers.Remove(oldGlow))
		{
			return;
		}
		bool flag = oldGlow.parent.def.category == ThingCategory.Plant && oldGlow.parent.def.plant.cavePlant;
		for (int i = 0; i < lights.Count; i++)
		{
			if (lights[i].glower != oldGlow)
			{
				continue;
			}
			foreach (IntVec3 item in lights[i].AffectedRect.ClipInsideMap(map))
			{
				cachedAccumulatedGlow[map.cellIndices.CellToIndex(item)] = null;
				if (!flag)
				{
					cachedAccumulatedGlowNoCavePlants[map.cellIndices.CellToIndex(item)] = null;
				}
				map.mapDrawer.MapMeshDirty(item, MapMeshFlagDefOf.GroundGlow);
			}
			lights.RemoveAt(i);
			break;
		}
	}

	public void GlowGridUpdate_First()
	{
		if (!dirtyCells.Any())
		{
			return;
		}
		affectedLightsTemp.Clear();
		foreach (IntVec3 dirtyCell in dirtyCells)
		{
			foreach (Light light in lights)
			{
				if (light.glower.parent.Position.InHorDistOf(dirtyCell, light.glower.GlowRadius))
				{
					affectedLightsTemp.Add(light.glower);
				}
			}
		}
		foreach (CompGlower item in affectedLightsTemp)
		{
			DeRegisterGlower(item);
			RegisterGlower(item);
		}
		dirtyCells.Clear();
	}

	public void DirtyCache(IntVec3 c)
	{
		dirtyCells.Add(c);
	}

	public void Rebuild()
	{
		foreach (CompGlower item in litGlowers.ToList())
		{
			DeRegisterGlower(item);
			RegisterGlower(item);
		}
	}

	private Color32 CombineColors(Color32 existingSum, Color32 toAdd, CompGlower toAddGlower)
	{
		float num = (int)toAdd.a;
		ColorInt colorInt = toAdd.AsColorInt();
		colorInt.ClampToNonNegative();
		colorInt.a = 0;
		if (colorInt.r > 0 || colorInt.g > 0 || colorInt.b > 0)
		{
			colorInt.ClampToNonNegative();
			ColorInt colorInt2 = existingSum.AsColorInt();
			colorInt2 += colorInt;
			if (num < toAddGlower.Props.overlightRadius)
			{
				colorInt2.a = 1;
			}
			return colorInt2.ProjectToColor32;
		}
		return existingSum;
	}
}
