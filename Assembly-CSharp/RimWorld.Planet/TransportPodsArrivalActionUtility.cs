using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.QuestGen;
using Verse;

namespace RimWorld.Planet;

public static class TransportPodsArrivalActionUtility
{
	public static IEnumerable<FloatMenuOption> GetFloatMenuOptions<T>(Func<FloatMenuAcceptanceReport> acceptanceReportGetter, Func<T> arrivalActionGetter, string label, CompLaunchable representative, int destinationTile, Action<Action> uiConfirmationCallback = null) where T : TransportPodsArrivalAction
	{
		FloatMenuAcceptanceReport floatMenuAcceptanceReport = acceptanceReportGetter();
		if (!floatMenuAcceptanceReport.Accepted && floatMenuAcceptanceReport.FailReason.NullOrEmpty() && floatMenuAcceptanceReport.FailMessage.NullOrEmpty())
		{
			yield break;
		}
		if (!floatMenuAcceptanceReport.FailReason.NullOrEmpty())
		{
			yield return new FloatMenuOption(label + " (" + floatMenuAcceptanceReport.FailReason + ")", null);
			yield break;
		}
		yield return new FloatMenuOption(label, delegate
		{
			FloatMenuAcceptanceReport floatMenuAcceptanceReport2 = acceptanceReportGetter();
			if (floatMenuAcceptanceReport2.Accepted)
			{
				if (uiConfirmationCallback == null)
				{
					representative.TryLaunch(destinationTile, arrivalActionGetter());
				}
				else
				{
					uiConfirmationCallback(delegate
					{
						representative.TryLaunch(destinationTile, arrivalActionGetter());
					});
				}
			}
			else if (!floatMenuAcceptanceReport2.FailMessage.NullOrEmpty())
			{
				Messages.Message(floatMenuAcceptanceReport2.FailMessage, new GlobalTargetInfo(destinationTile), MessageTypeDefOf.RejectInput, historical: false);
			}
		});
	}

	public static IEnumerable<FloatMenuOption> GetFloatMenuOptions<T>(Func<FloatMenuAcceptanceReport> acceptanceReportGetter, Func<T> arrivalActionGetter, string label, Action<int, TransportPodsArrivalAction> launchAction, int destinationTile) where T : TransportPodsArrivalAction
	{
		FloatMenuAcceptanceReport floatMenuAcceptanceReport = acceptanceReportGetter();
		if (!floatMenuAcceptanceReport.Accepted && floatMenuAcceptanceReport.FailReason.NullOrEmpty() && floatMenuAcceptanceReport.FailMessage.NullOrEmpty())
		{
			yield break;
		}
		if (!floatMenuAcceptanceReport.Accepted && !floatMenuAcceptanceReport.FailReason.NullOrEmpty())
		{
			label = label + " (" + floatMenuAcceptanceReport.FailReason + ")";
		}
		yield return new FloatMenuOption(label, delegate
		{
			FloatMenuAcceptanceReport floatMenuAcceptanceReport2 = acceptanceReportGetter();
			if (floatMenuAcceptanceReport2.Accepted)
			{
				launchAction(destinationTile, arrivalActionGetter());
			}
			else if (!floatMenuAcceptanceReport2.FailMessage.NullOrEmpty())
			{
				Messages.Message(floatMenuAcceptanceReport2.FailMessage, new GlobalTargetInfo(destinationTile), MessageTypeDefOf.RejectInput, historical: false);
			}
		});
	}

	public static bool AnyNonDownedColonist(IEnumerable<IThingHolder> pods)
	{
		foreach (IThingHolder pod in pods)
		{
			ThingOwner directlyHeldThings = pod.GetDirectlyHeldThings();
			for (int i = 0; i < directlyHeldThings.Count; i++)
			{
				if (directlyHeldThings[i] is Pawn { IsColonist: not false, Downed: false })
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool AnyPotentialCaravanOwner(IEnumerable<IThingHolder> pods, Faction faction)
	{
		foreach (IThingHolder pod in pods)
		{
			ThingOwner directlyHeldThings = pod.GetDirectlyHeldThings();
			for (int i = 0; i < directlyHeldThings.Count; i++)
			{
				if (directlyHeldThings[i] is Pawn pawn && CaravanUtility.IsOwner(pawn, faction))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Thing GetLookTarget(List<ActiveDropPodInfo> pods)
	{
		for (int i = 0; i < pods.Count; i++)
		{
			ThingOwner directlyHeldThings = pods[i].GetDirectlyHeldThings();
			for (int j = 0; j < directlyHeldThings.Count; j++)
			{
				if (directlyHeldThings[j] is Pawn { IsColonist: not false } pawn)
				{
					return pawn;
				}
			}
		}
		for (int k = 0; k < pods.Count; k++)
		{
			Thing thing = pods[k].GetDirectlyHeldThings().FirstOrDefault();
			if (thing != null)
			{
				return thing;
			}
		}
		return null;
	}

	public static void DropTravelingTransportPods(List<ActiveDropPodInfo> dropPods, IntVec3 near, Map map)
	{
		RemovePawnsFromWorldPawns(dropPods);
		for (int i = 0; i < dropPods.Count; i++)
		{
			DropCellFinder.TryFindDropSpotNear(near, map, out var result, allowFogged: false, canRoofPunch: true, allowIndoors: true, null);
			DropPodUtility.MakeDropPodAt(result, map, dropPods[i]);
		}
	}

	public static Thing DropShuttle(List<ActiveDropPodInfo> pods, Map map, IntVec3 cell, Faction faction = null)
	{
		RemovePawnsFromWorldPawns(pods);
		Thing thing = QuestGen_Shuttle.GenerateShuttle(faction, null, null, acceptColonists: false, onlyAcceptColonists: false, onlyAcceptHealthy: false, 0, dropEverythingIfUnsatisfied: false, leaveImmediatelyWhenSatisfied: false, dropEverythingOnArrival: true);
		TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, thing);
		CompTransporter compTransporter = thing.TryGetComp<CompTransporter>();
		for (int i = 0; i < pods.Count; i++)
		{
			compTransporter.innerContainer.TryAddRangeOrTransfer(pods[i].innerContainer);
		}
		if (!cell.IsValid)
		{
			cell = DropCellFinder.GetBestShuttleLandingSpot(map, Faction.OfPlayer);
		}
		transportShip.ArriveAt(cell, map.Parent);
		transportShip.AddJobs(ShipJobDefOf.Unload, ShipJobDefOf.FlyAway);
		return thing;
	}

	public static void RemovePawnsFromWorldPawns(List<ActiveDropPodInfo> pods)
	{
		for (int i = 0; i < pods.Count; i++)
		{
			ThingOwner innerContainer = pods[i].innerContainer;
			for (int j = 0; j < innerContainer.Count; j++)
			{
				if (innerContainer[j] is Pawn p && p.IsWorldPawn())
				{
					Find.WorldPawns.RemovePawn(p);
				}
			}
		}
	}
}
