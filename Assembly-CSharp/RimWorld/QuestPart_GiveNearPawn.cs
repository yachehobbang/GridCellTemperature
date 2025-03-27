using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace RimWorld;

public class QuestPart_GiveNearPawn : QuestPart
{
	public string inSignal;

	public string outSignalResult;

	public Pawn nearPawn;

	private List<Thing> items = new List<Thing>();

	private List<Pawn> pawns = new List<Pawn>();

	public List<ThingDefCountClass> thingDefs = new List<ThingDefCountClass>();

	public bool joinPlayer;

	public bool makePrisoners;

	public string customDropPodsLetterText;

	public string customDropPodsLetterLabel;

	public string customCaravanInventoryLetterText;

	public string customCaravanInventoryLetterLabel;

	public LetterDef customLetterDef;

	public bool sendStandardLetter = true;

	private Thing importantLookTarget;

	private List<Thing> tmpThingsToGive = new List<Thing>();

	public IEnumerable<Thing> Things
	{
		get
		{
			return items.Concat(pawns.Cast<Thing>());
		}
		set
		{
			items.Clear();
			pawns.Clear();
			if (value == null)
			{
				return;
			}
			foreach (Thing item2 in value)
			{
				if (item2 is Pawn item)
				{
					pawns.Add(item);
				}
				else
				{
					items.Add(item2);
				}
			}
		}
	}

	public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
	{
		get
		{
			foreach (Dialog_InfoCard.Hyperlink hyperlink in base.Hyperlinks)
			{
				yield return hyperlink;
			}
			foreach (Thing item in items)
			{
				ThingDef def = item.GetInnerIfMinified().def;
				yield return new Dialog_InfoCard.Hyperlink(def);
			}
		}
	}

	public override IEnumerable<GlobalTargetInfo> QuestLookTargets
	{
		get
		{
			foreach (GlobalTargetInfo questLookTarget in base.QuestLookTargets)
			{
				yield return questLookTarget;
			}
			foreach (Pawn questLookTarget2 in PawnsArriveQuestPartUtility.GetQuestLookTargets(pawns))
			{
				yield return questLookTarget2;
			}
			if (importantLookTarget != null)
			{
				yield return importantLookTarget;
			}
		}
	}

	public override bool IncreasesPopulation => PawnsArriveQuestPartUtility.IncreasesPopulation(pawns, joinPlayer, makePrisoners);

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		base.Notify_QuestSignalReceived(signal);
		if (!(signal.tag == inSignal) || nearPawn == null || (!nearPawn.SpawnedOrAnyParentSpawned && !nearPawn.IsCaravanMember()))
		{
			return;
		}
		pawns.RemoveAll((Pawn x) => x.Destroyed);
		items.RemoveAll((Thing x) => x.Destroyed);
		tmpThingsToGive.Clear();
		tmpThingsToGive.AddRange(Things);
		for (int i = 0; i < thingDefs.Count; i++)
		{
			Thing thing = ThingMaker.MakeThing(thingDefs[i].thingDef, GenStuff.RandomStuffByCommonalityFor(thingDefs[i].thingDef));
			thing.stackCount = thingDefs[i].count;
			tmpThingsToGive.Add(thing);
		}
		tmpThingsToGive.RemoveAll((Thing x) => x.Spawned);
		Thing thing2 = tmpThingsToGive.Where((Thing x) => x is Pawn).MaxByWithFallback((Thing x) => x.MarketValue);
		Thing thing3 = tmpThingsToGive.MaxByWithFallback((Thing x) => x.MarketValue * (float)x.stackCount);
		if (!tmpThingsToGive.Any())
		{
			return;
		}
		TaggedString text = null;
		TaggedString label = null;
		if (sendStandardLetter)
		{
			if (nearPawn.SpawnedOrAnyParentSpawned)
			{
				if (joinPlayer && pawns.Count == 1 && pawns[0].RaceProps.Humanlike)
				{
					text = "LetterRefugeeJoins".Translate(pawns[0].Named("PAWN"));
					label = "LetterLabelRefugeeJoins".Translate(pawns[0].Named("PAWN"));
					PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawns[0]);
				}
				else
				{
					text = "LetterQuestDropPodsArrived".Translate(GenLabel.ThingsLabel(tmpThingsToGive));
					label = "LetterLabelQuestDropPodsArrived".Translate();
					PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref label, ref text, "LetterRelatedPawnsNeutralGroup".Translate(Faction.OfPlayer.def.pawnsPlural), informEvenIfSeenBefore: true);
				}
				label = (customDropPodsLetterLabel.NullOrEmpty() ? label : customDropPodsLetterLabel.Formatted(label.Named("BASELABEL")));
				text = (customDropPodsLetterText.NullOrEmpty() ? text : customDropPodsLetterText.Formatted(text.Named("BASETEXT")));
			}
			else if (nearPawn.IsCaravanMember())
			{
				text = "LetterQuestItemsAddedToCaravanInventory".Translate(nearPawn.GetCaravan().Named("CARAVAN"), GenLabel.ThingsLabel(tmpThingsToGive).Named("THINGS"));
				label = "LetterLabelQuestItemsAddedToCaravanInventory".Translate(nearPawn.GetCaravan().Named("CARAVAN"));
				label = (customCaravanInventoryLetterLabel.NullOrEmpty() ? label : customCaravanInventoryLetterLabel.Formatted(label.Named("BASELABEL")));
				text = (customCaravanInventoryLetterText.NullOrEmpty() ? text : customCaravanInventoryLetterText.Formatted(text.Named("BASETEXT")));
			}
		}
		if (joinPlayer)
		{
			for (int j = 0; j < pawns.Count; j++)
			{
				if (pawns[j].Faction != Faction.OfPlayer)
				{
					pawns[j].SetFaction(Faction.OfPlayer);
				}
			}
		}
		else if (makePrisoners)
		{
			for (int k = 0; k < pawns.Count; k++)
			{
				if (pawns[k].RaceProps.Humanlike)
				{
					if (!pawns[k].IsPrisonerOfColony)
					{
						pawns[k].guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
					}
					HealthUtility.TryAnesthetize(pawns[k]);
				}
			}
		}
		GlobalTargetInfo globalTargetInfo = GlobalTargetInfo.Invalid;
		if (nearPawn.SpawnedOrAnyParentSpawned)
		{
			IntVec3 intVec = DropCellFinder.TradeDropSpot(nearPawn.MapHeld);
			DropPodUtility.DropThingsNear(intVec, nearPawn.MapHeld, tmpThingsToGive, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: false, forbid: false);
			globalTargetInfo = new GlobalTargetInfo(intVec, nearPawn.MapHeld);
			for (int l = 0; l < tmpThingsToGive.Count; l++)
			{
				if (tmpThingsToGive[l].SpawnedOrAnyParentSpawned)
				{
					globalTargetInfo = new GlobalTargetInfo(tmpThingsToGive[l].PositionHeld, nearPawn.MapHeld);
					break;
				}
			}
		}
		else if (nearPawn.IsCaravanMember())
		{
			for (int m = 0; m < tmpThingsToGive.Count; m++)
			{
				if (tmpThingsToGive[m] is Pawn pawn)
				{
					if (pawn.Faction != Faction.OfPlayer)
					{
						pawn.SetFaction(Faction.OfPlayer);
					}
					nearPawn.GetCaravan().AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
				}
				else
				{
					CaravanInventoryUtility.GiveThing(nearPawn.GetCaravan(), tmpThingsToGive[m]);
				}
			}
			globalTargetInfo = nearPawn.GetCaravan();
		}
		if (sendStandardLetter)
		{
			Find.LetterStack.ReceiveLetter(label, text, customLetterDef ?? LetterDefOf.PositiveEvent, globalTargetInfo, null, quest);
		}
		importantLookTarget = items.Find((Thing x) => x.GetInnerIfMinified() is MonumentMarker).GetInnerIfMinified();
		items.Clear();
		if (!outSignalResult.NullOrEmpty())
		{
			if (thing2 != null)
			{
				Find.SignalManager.SendSignal(new Signal(outSignalResult, thing2.Named("SUBJECT")));
			}
			else if (thing3 != null)
			{
				Find.SignalManager.SendSignal(new Signal(outSignalResult, thing3.Named("SUBJECT")));
			}
			else
			{
				Find.SignalManager.SendSignal(new Signal(outSignalResult));
			}
		}
	}

	public override bool QuestPartReserves(Pawn p)
	{
		return pawns.Contains(p);
	}

	public override void Cleanup()
	{
		base.Cleanup();
		for (int i = 0; i < items.Count; i++)
		{
			if (!items[i].Destroyed)
			{
				items[i].Destroy();
			}
		}
		items.Clear();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref inSignal, "inSignal");
		Scribe_Values.Look(ref outSignalResult, "outSignalResult");
		Scribe_References.Look(ref nearPawn, "nearPawn");
		Scribe_Collections.Look(ref items, "items", LookMode.Deep);
		Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
		Scribe_Collections.Look(ref thingDefs, "thingDefs", LookMode.Deep);
		Scribe_Values.Look(ref joinPlayer, "joinPlayer", defaultValue: false);
		Scribe_Values.Look(ref makePrisoners, "makePrisoners", defaultValue: false);
		Scribe_Values.Look(ref customDropPodsLetterLabel, "customDropPodsLetterLabel");
		Scribe_Values.Look(ref customDropPodsLetterText, "customDropPodsLetterText");
		Scribe_Values.Look(ref customCaravanInventoryLetterLabel, "customCaravanInventoryLetterLabel");
		Scribe_Values.Look(ref customCaravanInventoryLetterText, "customCaravanInventoryLetterText");
		Scribe_Defs.Look(ref customLetterDef, "customLetterDef");
		Scribe_Values.Look(ref sendStandardLetter, "sendStandardLetter", defaultValue: true);
		Scribe_References.Look(ref importantLookTarget, "importantLookTarget");
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			items.RemoveAll((Thing x) => x == null);
			pawns.RemoveAll((Pawn x) => x == null);
		}
	}

	public override void AssignDebugData()
	{
		base.AssignDebugData();
		inSignal = "DebugSignal" + Rand.Int;
	}
}
