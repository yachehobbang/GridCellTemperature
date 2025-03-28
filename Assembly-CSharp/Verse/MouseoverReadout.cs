using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace Verse;

public class MouseoverReadout
{
	private TerrainDef cachedTerrain;

	private bool cachedPolluted;

	private string cachedTerrainString;

	private const float YInterval = 19f;

	private static readonly Vector2 BotLeft = new Vector2(15f, 65f);

	public void MouseoverReadoutOnGUI()
	{
		if (Event.current.type != EventType.Repaint || Find.MainTabsRoot.OpenTab != null)
		{
			return;
		}
		GenUI.DrawTextWinterShadow(new Rect(256f, UI.screenHeight - 256, -256f, 256f));
		Text.Font = GameFont.Small;
		GUI.color = new Color(1f, 1f, 1f, 0.8f);
		IntVec3 intVec = UI.MouseCell();
		if (!intVec.InBounds(Find.CurrentMap))
		{
			return;
		}
		float num = 0f;
		if (intVec.Fogged(Find.CurrentMap))
		{
			Widgets.Label(new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - num, 999f, 999f), "Undiscovered".Translate());
			GUI.color = Color.white;
			return;
		}
		Widgets.Label(new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - num, 999f, 999f), MouseoverUtility.GetGlowLabelByValue(Find.CurrentMap.glowGrid.GroundGlowAt(intVec)));
		num += 19f;
		Rect rect = new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - num, 999f, 999f);
		TerrainDef terrain = intVec.GetTerrain(Find.CurrentMap);
		bool flag = intVec.IsPolluted(Find.CurrentMap);
		if (terrain != cachedTerrain || flag != cachedPolluted)
		{
			float fertility = intVec.GetFertility(Find.CurrentMap);
			string text = (((double)fertility > 0.0001) ? (" " + "FertShort".TranslateSimple() + " " + fertility.ToStringPercent()) : "");
			string text2 = (flag ? "PollutedTerrain".Translate(terrain.label).CapitalizeFirst() : terrain.LabelCap);
			cachedTerrainString = text2 + ((terrain.passability != Traversability.Impassable) ? (" (" + "WalkSpeed".Translate(GenPath.SpeedPercentString(terrain.pathCost)) + text + ")") : ((TaggedString)null));
			cachedTerrain = terrain;
			cachedPolluted = flag;
		}
		Widgets.Label(rect, cachedTerrainString);
		num += 19f;
		Zone zone = intVec.GetZone(Find.CurrentMap);
		if (zone != null)
		{
			Rect rect2 = new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - num, 999f, 999f);
			string label = zone.label;
			Widgets.Label(rect2, label);
			num += 19f;
		}
		float depth = Find.CurrentMap.snowGrid.GetDepth(intVec);
		if (depth > 0.03f)
		{
			Rect rect3 = new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - num, 999f, 999f);
			SnowCategory snowCategory = SnowUtility.GetSnowCategory(depth);
			string label2 = "Snow".Translate() + "(" + SnowUtility.GetDescription(snowCategory) + ")" + " (" + "WalkSpeed".Translate(GenPath.SpeedPercentString(SnowUtility.MovementTicksAddOn(snowCategory))) + ")";
			Widgets.Label(rect3, label2);
			num += 19f;
		}
		List<Thing> thingList = intVec.GetThingList(Find.CurrentMap);
		for (int i = 0; i < thingList.Count; i++)
		{
			Thing thing = thingList[i];
			CompSelectProxy compSelectProxy;
			if ((compSelectProxy = thing.TryGetComp<CompSelectProxy>()) != null && compSelectProxy.thingToSelect != null)
			{
				thing = compSelectProxy.thingToSelect;
			}
			if (thing.def.category != ThingCategory.Mote && (!(thing is Pawn pawn) || !pawn.IsHiddenFromPlayer()))
			{
				Rect rect4 = new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - num, 999f, 999f);
				string labelMouseover = thing.LabelMouseover;
				Widgets.Label(rect4, labelMouseover);
				num += 19f;
			}
		}
		RoofDef roof = intVec.GetRoof(Find.CurrentMap);
		if (roof != null)
		{
			Widgets.Label(new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - num, 999f, 999f), roof.LabelCap);
			num += 19f;
		}
		if (Find.CurrentMap.gasGrid.AnyGasAt(intVec))
		{
			DrawGas(GasType.BlindSmoke, Find.CurrentMap.gasGrid.DensityAt(intVec, GasType.BlindSmoke), ref num);
			DrawGas(GasType.ToxGas, Find.CurrentMap.gasGrid.DensityAt(intVec, GasType.ToxGas), ref num);
			DrawGas(GasType.RotStink, Find.CurrentMap.gasGrid.DensityAt(intVec, GasType.RotStink), ref num);
			DrawGas(GasType.DeadlifeDust, Find.CurrentMap.gasGrid.DensityAt(intVec, GasType.DeadlifeDust), ref num);
		}
		GUI.color = Color.white;
	}

	private void DrawGas(GasType gasType, byte density, ref float curYOffset)
	{
		if (density > 0)
		{
			Widgets.Label(new Rect(BotLeft.x, (float)UI.screenHeight - BotLeft.y - curYOffset, 999f, 999f), gasType.GetLabel().CapitalizeFirst() + " " + ((float)(int)density / 255f).ToStringPercent("F0"));
			curYOffset += 19f;
		}
	}
}
