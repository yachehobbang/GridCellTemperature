using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld.Planet;

public static class SettlementProximityGoodwillUtility
{
	private static List<Pair<Settlement, int>> tmpGoodwillOffsets = new List<Pair<Settlement, int>>();

	public static int MaxDist => Mathf.RoundToInt(DiplomacyTuning.Goodwill_PerQuadrumFromSettlementProximity.Last().x);

	public static void CheckSettlementProximityGoodwillChange()
	{
		if (Find.TickManager.TicksGame == 0 || Find.TickManager.TicksGame % 900000 != 0)
		{
			return;
		}
		List<Settlement> settlements = Find.WorldObjects.Settlements;
		tmpGoodwillOffsets.Clear();
		for (int i = 0; i < settlements.Count; i++)
		{
			Settlement settlement = settlements[i];
			if (settlement.Faction == Faction.OfPlayer)
			{
				AppendProximityGoodwillOffsets(settlement.Tile, tmpGoodwillOffsets, ignoreIfAlreadyMinGoodwill: true, ignorePermanentlyHostile: false);
			}
		}
		if (!tmpGoodwillOffsets.Any())
		{
			return;
		}
		SortProximityGoodwillOffsets(tmpGoodwillOffsets);
		List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
		bool flag = false;
		TaggedString text = "LetterFactionBaseProximity".Translate() + "\n\n" + ProximityGoodwillOffsetsToString(tmpGoodwillOffsets).ToLineList(" - ");
		for (int j = 0; j < allFactionsListForReading.Count; j++)
		{
			Faction faction = allFactionsListForReading[j];
			if (faction == Faction.OfPlayer)
			{
				continue;
			}
			int num = 0;
			for (int k = 0; k < tmpGoodwillOffsets.Count; k++)
			{
				if (tmpGoodwillOffsets[k].First.Faction == faction)
				{
					num += tmpGoodwillOffsets[k].Second;
				}
			}
			if (num != 0)
			{
				FactionRelationKind playerRelationKind = faction.PlayerRelationKind;
				Faction.OfPlayer.TryAffectGoodwillWith(faction, num, canSendMessage: false, canSendHostilityLetter: false, HistoryEventDefOf.SettlementProximity, null);
				flag = true;
				faction.TryAppendRelationKindChangedInfo(ref text, playerRelationKind, faction.PlayerRelationKind);
			}
		}
		if (flag)
		{
			Find.LetterStack.ReceiveLetter("LetterLabelFactionBaseProximity".Translate(), text, LetterDefOf.NegativeEvent);
		}
	}

	public static void AppendProximityGoodwillOffsets(int tile, List<Pair<Settlement, int>> outOffsets, bool ignoreIfAlreadyMinGoodwill, bool ignorePermanentlyHostile)
	{
		int maxDist = MaxDist;
		List<Settlement> settlements = Find.WorldObjects.Settlements;
		for (int i = 0; i < settlements.Count; i++)
		{
			Settlement settlement = settlements[i];
			if (settlement.Faction == null || settlement.Faction == Faction.OfPlayer || (ignorePermanentlyHostile && settlement.Faction.def.permanentEnemy) || (ignoreIfAlreadyMinGoodwill && settlement.Faction.PlayerGoodwill == -100))
			{
				continue;
			}
			int num = Find.WorldGrid.TraversalDistanceBetween(tile, settlement.Tile, passImpassable: false, maxDist);
			if (num != int.MaxValue)
			{
				int num2 = Mathf.RoundToInt(DiplomacyTuning.Goodwill_PerQuadrumFromSettlementProximity.Evaluate(num));
				if (num2 != 0)
				{
					outOffsets.Add(new Pair<Settlement, int>(settlement, num2));
				}
			}
		}
	}

	public static void SortProximityGoodwillOffsets(List<Pair<Settlement, int>> offsets)
	{
		offsets.SortBy((Pair<Settlement, int> x) => x.First.Faction.loadID, (Pair<Settlement, int> x) => -Mathf.Abs(x.Second));
	}

	public static IEnumerable<string> ProximityGoodwillOffsetsToString(List<Pair<Settlement, int>> offsets)
	{
		for (int i = 0; i < offsets.Count; i++)
		{
			yield return offsets[i].First.LabelCap + ": " + "ProximitySingleGoodwillChange".Translate(offsets[i].Second.ToStringWithSign(), offsets[i].First.Faction.Name);
		}
	}

	private static IEnumerable<string> GetConfirmationDescriptions(int tile)
	{
		tmpGoodwillOffsets.Clear();
		AppendProximityGoodwillOffsets(tile, tmpGoodwillOffsets, ignoreIfAlreadyMinGoodwill: false, ignorePermanentlyHostile: true);
		if (tmpGoodwillOffsets.Any())
		{
			yield return "ConfirmSettleNearFactionBase".Translate(MaxDist - 1, 15);
		}
		if (ModsConfig.BiotechActive && NoxiousHazeUtility.TryGetNoxiousHazeMTB(tile, out var mtb))
		{
			yield return "ConfirmSettleNearPollution".Translate(mtb);
		}
	}

	private static IEnumerable<string> GetConfirmationEffects(int tile)
	{
		tmpGoodwillOffsets.Clear();
		AppendProximityGoodwillOffsets(tile, tmpGoodwillOffsets, ignoreIfAlreadyMinGoodwill: false, ignorePermanentlyHostile: true);
		SortProximityGoodwillOffsets(tmpGoodwillOffsets);
		foreach (string item in ProximityGoodwillOffsetsToString(tmpGoodwillOffsets))
		{
			yield return item;
		}
	}

	public static void CheckConfirmSettle(int tile, Action settleAction)
	{
		IEnumerable<string> confirmationDescriptions = GetConfirmationDescriptions(tile);
		if (confirmationDescriptions.Any())
		{
			string text = "";
			foreach (string item in confirmationDescriptions)
			{
				if (!text.NullOrEmpty())
				{
					text += "\n\n";
				}
				text += item;
			}
			text += "\n\n" + "ConfirmSettle".Translate();
			IEnumerable<string> confirmationEffects = GetConfirmationEffects(tile);
			if (confirmationEffects.Any())
			{
				text = text + "\n\n" + confirmationEffects.ToLineList(" - ");
			}
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, settleAction));
		}
		else
		{
			settleAction();
		}
	}
}
