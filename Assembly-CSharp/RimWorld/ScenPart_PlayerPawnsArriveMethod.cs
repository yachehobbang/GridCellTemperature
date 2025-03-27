using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class ScenPart_PlayerPawnsArriveMethod : ScenPart
{
	private PlayerPawnsArriveMethod method;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref method, "method", PlayerPawnsArriveMethod.Standing);
	}

	public override void DoEditInterface(Listing_ScenEdit listing)
	{
		if (!Widgets.ButtonText(listing.GetScenPartRect(this, ScenPart.RowHeight), method.ToStringHuman(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (PlayerPawnsArriveMethod value in Enum.GetValues(typeof(PlayerPawnsArriveMethod)))
		{
			PlayerPawnsArriveMethod localM = value;
			list.Add(new FloatMenuOption(localM.ToStringHuman(), delegate
			{
				method = localM;
			}));
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	public override string Summary(Scenario scen)
	{
		if (method == PlayerPawnsArriveMethod.DropPods)
		{
			return "ScenPart_ArriveInDropPods".Translate();
		}
		return null;
	}

	public override void Randomize()
	{
		method = ((Rand.Value < 0.5f) ? PlayerPawnsArriveMethod.DropPods : PlayerPawnsArriveMethod.Standing);
	}

	public override void GenerateIntoMap(Map map)
	{
		if (Find.GameInitData == null)
		{
			return;
		}
		List<List<Thing>> list = new List<List<Thing>>();
		List<Thing> list2 = new List<Thing>();
		foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
		{
			List<Thing> list3 = new List<Thing>();
			list3.Add(startingAndOptionalPawn);
			list.Add(list3);
			foreach (ThingDefCount item in Find.GameInitData.startingPossessions[startingAndOptionalPawn])
			{
				list2.Add(StartingPawnUtility.GenerateStartingPossession(item));
			}
		}
		foreach (ScenPart allPart in Find.Scenario.AllParts)
		{
			list2.AddRange(allPart.PlayerStartingThings());
		}
		int num = 0;
		foreach (Thing item2 in list2)
		{
			if (item2.def.CanHaveFaction)
			{
				item2.SetFactionDirect(Faction.OfPlayer);
			}
			list[num].Add(item2);
			num++;
			if (num >= list.Count)
			{
				num = 0;
			}
		}
		DropPodUtility.DropThingGroupsNear(MapGenerator.PlayerStartSpot, map, list, 110, Find.GameInitData.QuickStarted || method != PlayerPawnsArriveMethod.DropPods, leaveSlag: true, canRoofPunch: true, forbid: true, allowFogged: false);
	}

	public override void PostMapGenerate(Map map)
	{
		if (Find.GameInitData != null && method == PlayerPawnsArriveMethod.DropPods)
		{
			PawnUtility.GiveAllStartingPlayerPawnsThought(ThoughtDefOf.CrashedTogether);
		}
	}

	public override int GetHashCode()
	{
		return base.GetHashCode() ^ method.GetHashCode();
	}
}
