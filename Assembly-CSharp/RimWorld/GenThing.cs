using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class GenThing
{
	private static List<Thing> tmpThings = new List<Thing>();

	private static List<string> tmpThingLabels = new List<string>();

	private static List<Pair<string, int>> tmpThingCounts = new List<Pair<string, int>>();

	public static Vector3 TrueCenter(this Thing t)
	{
		if (t is Pawn pawn)
		{
			return pawn.Drawer.DrawPos;
		}
		if (t.def.category == ThingCategory.Item && t.Spawned)
		{
			return ItemCenterAt(t.Position, t.Map, t.def.Altitude, t.thingIDNumber);
		}
		return TrueCenter(t.Position, t.Rotation, t.def.size, t.def.Altitude);
	}

	public static Vector3 TrueCenter(IntVec3 loc, Rot4 rotation, IntVec2 thingSize, float altitude)
	{
		Vector3 result = loc.ToVector3ShiftedWithAltitude(altitude);
		if (thingSize.x != 1 || thingSize.z != 1)
		{
			if (rotation.IsHorizontal)
			{
				int x = thingSize.x;
				thingSize.x = thingSize.z;
				thingSize.z = x;
			}
			switch (rotation.AsInt)
			{
			case 0:
				if (thingSize.x % 2 == 0)
				{
					result.x += 0.5f;
				}
				if (thingSize.z % 2 == 0)
				{
					result.z += 0.5f;
				}
				break;
			case 1:
				if (thingSize.x % 2 == 0)
				{
					result.x += 0.5f;
				}
				if (thingSize.z % 2 == 0)
				{
					result.z -= 0.5f;
				}
				break;
			case 2:
				if (thingSize.x % 2 == 0)
				{
					result.x -= 0.5f;
				}
				if (thingSize.z % 2 == 0)
				{
					result.z -= 0.5f;
				}
				break;
			case 3:
				if (thingSize.x % 2 == 0)
				{
					result.x -= 0.5f;
				}
				if (thingSize.z % 2 == 0)
				{
					result.z += 0.5f;
				}
				break;
			}
		}
		return result;
	}

	private static Vector3 ItemCenterAt(IntVec3 c, Map map, float altitude, int thingID)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		bool flag2 = true;
		ThingDef thingDef = null;
		List<Thing> thingList = c.GetThingList(map);
		for (int i = 0; i < thingList.Count; i++)
		{
			Thing thing = thingList[i];
			if (thing.def.category == ThingCategory.Item)
			{
				if (thingDef == null)
				{
					thingDef = thing.def;
				}
				num++;
				if (thing.def.IsWeapon && thing.def != ThingDefOf.WoodLog)
				{
					flag = true;
				}
				if (thing.thingIDNumber < thingID)
				{
					num2++;
				}
				if (thing.def != thingDef)
				{
					flag2 = false;
				}
			}
		}
		float num3 = (float)num2 * (1f / 26f) / 10f;
		if (num <= 1)
		{
			Vector3 vector = c.ToVector3Shifted();
			return new Vector3(vector.x, altitude, vector.z);
		}
		if (flag)
		{
			Vector3 vector2 = c.ToVector3Shifted();
			float num4 = 1f / (float)num;
			int num5 = GetRowItemCount(new IntVec3(c.x - 1, c.y, c.z)) + num2;
			return new Vector3(vector2.x - 0.5f + num4 * ((float)num2 + 0.5f), altitude + num3, vector2.z + ((num5 % 2 == 0) ? (-0.02f) : 0.2f));
		}
		if (flag2)
		{
			Vector3 vector3 = c.ToVector3Shifted();
			return new Vector3(vector3.x + (float)num2 * 0.11f - 0.08f, altitude + num3, vector3.z + (float)num2 * 0.24f - 0.05f);
		}
		Vector3 vector4 = c.ToVector3Shifted();
		Vector2 vector5 = GenGeo.RegularPolygonVertexPosition(num, num2, ((c.x + c.z) % 2 == 0) ? 0f : 60f) * 0.3f;
		return new Vector3(vector5.x + vector4.x, altitude + num3, vector5.y + vector4.z);
		int GetRowItemCount(IntVec3 x)
		{
			if (!x.InBounds(map))
			{
				return 0;
			}
			int itemCount = x.GetItemCount(map);
			if (itemCount <= 1)
			{
				return 0;
			}
			x.x--;
			return itemCount + GetRowItemCount(x);
		}
	}

	public static bool TryDropAndSetForbidden(Thing th, IntVec3 pos, Map map, ThingPlaceMode mode, out Thing resultingThing, bool forbidden)
	{
		if (GenDrop.TryDropSpawn(th, pos, map, ThingPlaceMode.Near, out resultingThing))
		{
			if (resultingThing != null)
			{
				resultingThing.SetForbidden(forbidden, warnOnFail: false);
			}
			return true;
		}
		resultingThing = null;
		return false;
	}

	public static string ThingsToCommaList(IList<Thing> things, bool useAnd = false, bool aggregate = true, int maxCount = -1)
	{
		tmpThings.Clear();
		tmpThingLabels.Clear();
		tmpThingCounts.Clear();
		tmpThings.AddRange(things);
		if (tmpThings.Count >= 2)
		{
			tmpThings.SortByDescending((Thing x) => x is Pawn, (Thing x) => x.def.BaseMarketValue * (float)x.stackCount);
		}
		for (int i = 0; i < tmpThings.Count; i++)
		{
			string text = ((tmpThings[i] is Pawn) ? tmpThings[i].LabelShort : tmpThings[i].LabelNoCount);
			bool flag = false;
			if (aggregate)
			{
				for (int j = 0; j < tmpThingCounts.Count; j++)
				{
					if (tmpThingCounts[j].First == text)
					{
						tmpThingCounts[j] = new Pair<string, int>(tmpThingCounts[j].First, tmpThingCounts[j].Second + tmpThings[i].stackCount);
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				tmpThingCounts.Add(new Pair<string, int>(text, tmpThings[i].stackCount));
			}
		}
		tmpThings.Clear();
		bool flag2 = false;
		int num = tmpThingCounts.Count;
		if (maxCount >= 0 && num > maxCount)
		{
			num = maxCount;
			flag2 = true;
		}
		for (int k = 0; k < num; k++)
		{
			string text2 = tmpThingCounts[k].First;
			if (tmpThingCounts[k].Second != 1)
			{
				text2 = text2 + " x" + tmpThingCounts[k].Second;
			}
			tmpThingLabels.Add(text2);
		}
		string text3 = tmpThingLabels.ToCommaList(useAnd && !flag2);
		if (flag2)
		{
			text3 += "...";
		}
		return text3;
	}

	public static float GetMarketValue(IList<Thing> things)
	{
		float num = 0f;
		for (int i = 0; i < things.Count; i++)
		{
			num += things[i].MarketValue * (float)things[i].stackCount;
		}
		return num;
	}

	public static bool CloserThingBetween(ThingDef thingDef, IntVec3 a, IntVec3 b, Map map, Thing thingToIgnore = null)
	{
		foreach (IntVec3 item in CellRect.FromLimits(a, b))
		{
			if (item == a || item == b || !item.InBounds(map))
			{
				continue;
			}
			foreach (Thing thing in item.GetThingList(map))
			{
				if ((thingToIgnore == null || thingToIgnore != thing) && (thing.def == thingDef || thing.def.entityDefToBuild == thingDef))
				{
					return true;
				}
			}
		}
		return false;
	}
}
