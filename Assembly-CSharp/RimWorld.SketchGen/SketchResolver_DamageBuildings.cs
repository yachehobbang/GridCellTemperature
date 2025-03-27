using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld.SketchGen;

public class SketchResolver_DamageBuildings : SketchResolver
{
	private const float MaxPctOfTotalDestroyed = 0.65f;

	private const float HpRandomFactor = 1.2f;

	private const float DestroyChanceExp = 1.32f;

	protected override bool CanResolveInt(ResolveParams parms)
	{
		return true;
	}

	protected override void ResolveInt(ResolveParams parms)
	{
		CellRect occupiedRect = parms.sketch.OccupiedRect;
		Rot4 random = Rot4.Random;
		int num = 0;
		int num2 = parms.sketch.Buildables.Count();
		foreach (SketchBuildable item in parms.sketch.Buildables.InRandomOrder().ToList())
		{
			Damage(item, occupiedRect, random, parms.sketch, out var destroyed, parms.destroyChanceExp);
			if (destroyed)
			{
				num++;
				ClearDisconnectedDoors(parms, item.pos);
				if ((float)num > (float)num2 * 0.65f)
				{
					break;
				}
			}
		}
	}

	private void ClearDisconnectedDoors(ResolveParams parms, IntVec3 position)
	{
		IntVec3[] cardinalDirectionsAround = GenAdj.CardinalDirectionsAround;
		for (int i = 0; i < cardinalDirectionsAround.Length; i++)
		{
			IntVec3 position2 = cardinalDirectionsAround[i] + position;
			SketchThing door = parms.sketch.GetDoor(position2);
			if (door != null && !AdjacentToWall(parms.sketch, door))
			{
				parms.sketch.Remove(door);
			}
		}
	}

	private bool AdjacentToWall(Sketch sketch, SketchEntity entity)
	{
		IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
		foreach (IntVec3 intVec in cardinalDirections)
		{
			if (sketch.ThingsAt(entity.pos + intVec).Any((SketchThing t) => t.def == ThingDefOf.Wall))
			{
				return true;
			}
		}
		return false;
	}

	private void Damage(SketchBuildable buildable, CellRect rect, Rot4 dir, Sketch sketch, out bool destroyed, float? destroyChanceExp = null)
	{
		float num = ((!dir.IsHorizontal) ? ((float)(buildable.pos.z - rect.minZ) / (float)rect.Height) : ((float)(buildable.pos.x - rect.minX) / (float)rect.Width));
		if (dir == Rot4.East || dir == Rot4.South)
		{
			num = 1f - num;
		}
		if (Rand.Chance(Mathf.Pow(num, destroyChanceExp ?? 1.32f)))
		{
			sketch.Remove(buildable);
			destroyed = true;
			if (buildable is SketchTerrain sketchTerrain && sketchTerrain.def.burnedDef != null)
			{
				sketch.AddTerrain(sketchTerrain.def.burnedDef, sketchTerrain.pos);
			}
		}
		else
		{
			destroyed = false;
			if (buildable is SketchThing sketchThing)
			{
				sketchThing.hitPoints = Mathf.Clamp(Mathf.RoundToInt((float)sketchThing.MaxHitPoints * (1f - num) * Rand.Range(1f, 1.2f)), 1, sketchThing.MaxHitPoints);
			}
		}
	}
}
