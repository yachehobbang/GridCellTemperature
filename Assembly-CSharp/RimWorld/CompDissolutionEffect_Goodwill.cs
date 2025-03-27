using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace RimWorld;

public class CompDissolutionEffect_Goodwill : CompDissolutionEffect
{
	private struct GoodwillPollutionEvent
	{
		public int tile;

		public int amount;
	}

	private static readonly SimpleCurve GoodwillFactorOverDistanceCurvePerWastepack = new SimpleCurve
	{
		new CurvePoint(0f, 1f),
		new CurvePoint(0.9999f, 1f),
		new CurvePoint(1f, 0.5f),
		new CurvePoint(1.9999f, 0.5f),
		new CurvePoint(2f, 0.2f),
		new CurvePoint(4.9999f, 0.2f),
		new CurvePoint(5f, 0.1f),
		new CurvePoint(7.9999f, 0.1f),
		new CurvePoint(8f, 0.066f),
		new CurvePoint(16f, 0.066f),
		new CurvePoint(16.0001f, 0.05f)
	};

	private const float MaxDistanceFactor = 1.25f;

	private static readonly List<GoodwillPollutionEvent> pendingGoodwillEvents = new List<GoodwillPollutionEvent>();

	private static readonly Dictionary<Settlement, float> tmpAvailableSettlements = new Dictionary<Settlement, float>();

	private static readonly List<QuestScriptDef> tmpRetaliations = new List<QuestScriptDef>();

	public override void DoDissolutionEffectMap(int amount)
	{
		if (parent.Spawned && !parent.Map.IsPlayerHome)
		{
			GoodwillPollutionEvent goodwillPollutionEvent = default(GoodwillPollutionEvent);
			goodwillPollutionEvent.tile = parent.Map.Tile;
			goodwillPollutionEvent.amount = amount;
			GoodwillPollutionEvent item = goodwillPollutionEvent;
			pendingGoodwillEvents.Add(item);
		}
	}

	public override void DoDissolutionEffectWorld(int amount, int tileId)
	{
		AddWorldDissolutionEvent(amount, tileId);
	}

	public static void AddWorldDissolutionEvent(int amount, int tileId)
	{
		if (!PollutionUtility.IsExecutingPollutionIgnoredQuest())
		{
			GoodwillPollutionEvent goodwillPollutionEvent = default(GoodwillPollutionEvent);
			goodwillPollutionEvent.tile = tileId;
			goodwillPollutionEvent.amount = amount;
			GoodwillPollutionEvent item = goodwillPollutionEvent;
			pendingGoodwillEvents.Add(item);
		}
	}

	public static void WorldUpdate()
	{
		if (pendingGoodwillEvents.Count <= 0)
		{
			return;
		}
		foreach (IGrouping<int, GoodwillPollutionEvent> item in from g in pendingGoodwillEvents
			group g by g.tile)
		{
			int key = item.Key;
			if (TryGetEffectedSettlement(key, out var result, out var distance))
			{
				int num = item.Sum((GoodwillPollutionEvent p) => p.amount);
				int num2 = Mathf.Min(-Mathf.RoundToInt(GoodwillFactorOverDistanceCurvePerWastepack.Evaluate(distance) * (float)num), -1);
				HistoryEventDef historyEventDef = ((result.Tile == key) ? HistoryEventDefOf.PollutedBase : ((distance > 8) ? HistoryEventDefOf.ToxicWasteDumping : HistoryEventDefOf.PollutedNearbySite));
				if (result.Faction.IsPlayerGoodwillMinimum())
				{
					Messages.Message("MessageAngeredPollutedCell".Translate(result.Faction.Name, historyEventDef.label), result, MessageTypeDefOf.NegativeEvent);
				}
				Faction.OfPlayer.TryAffectGoodwillWith(result.Faction, num2, canSendMessage: true, canSendHostilityLetter: true, historyEventDef, result);
				if (!Current.Game.IsPlayerTile(key) && result.Faction.HostileTo(Faction.OfPlayer) && Rand.Chance(Mathf.Clamp01((float)(-num2) / 100f)))
				{
					TriggerRetaliationEvent(result.Faction);
				}
			}
			tmpAvailableSettlements.Clear();
		}
		pendingGoodwillEvents.Clear();
	}

	private static bool TryGetEffectedSettlement(int tile, out Settlement result, out int distance)
	{
		result = null;
		distance = 0;
		if (tile < 0)
		{
			return false;
		}
		List<Settlement> settlements = Find.WorldObjects.Settlements;
		Settlement settlement = null;
		float num = 0f;
		for (int i = 0; i < settlements.Count; i++)
		{
			Settlement settlement2 = settlements[i];
			if (settlement2.Faction != Faction.OfPlayer && !settlement2.Faction.Hidden)
			{
				if (tile == settlement2.Tile)
				{
					result = settlement2;
					distance = 0;
					return true;
				}
				float num2 = Find.WorldGrid.ApproxDistanceInTiles(settlement2.Tile, tile);
				if (settlement == null || num2 < num)
				{
					settlement = settlement2;
					num = num2;
				}
			}
		}
		if (settlement == null)
		{
			return false;
		}
		float maxDistance = Find.WorldGrid.ApproxDistanceInTiles(settlement.Tile, tile) * 1.25f;
		tmpAvailableSettlements.Clear();
		for (int j = 0; j < settlements.Count; j++)
		{
			Settlement settlement3 = settlements[j];
			if (settlement3.Faction != Faction.OfPlayer && !settlement3.Faction.Hidden)
			{
				float num3 = Find.WorldGrid.ApproxDistanceInTiles(settlement3.Tile, tile);
				if (num3 <= maxDistance)
				{
					tmpAvailableSettlements.Add(settlement3, num3);
				}
			}
		}
		if (tmpAvailableSettlements.TryRandomElementByWeight((KeyValuePair<Settlement, float> kvp) => maxDistance - kvp.Value, out var result2))
		{
			result = result2.Key;
			distance = Mathf.RoundToInt(result2.Value);
		}
		tmpAvailableSettlements.Clear();
		return result != null;
	}

	private static void TriggerRetaliationEvent(Faction faction)
	{
		Slate slate = new Slate();
		Map map = QuestGen_Get.GetMap(mustBeInfestable: false, null);
		slate.Set("map", map);
		slate.Set("enemyFaction", faction);
		slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(map));
		tmpRetaliations.Clear();
		tmpRetaliations.Add(QuestScriptDefOf.PollutionRetaliation);
		tmpRetaliations.Add(QuestScriptDefOf.PollutionRaid);
		for (int num = tmpRetaliations.Count - 1; num >= 0; num--)
		{
			if (!tmpRetaliations[num].CanRun(slate))
			{
				tmpRetaliations.RemoveAt(num);
			}
		}
		if (tmpRetaliations.Count == 0)
		{
			Log.Warning("No pollution retaliations were possible");
			return;
		}
		QuestUtility.GenerateQuestAndMakeAvailable(tmpRetaliations.RandomElement(), slate);
		tmpRetaliations.Clear();
	}
}
