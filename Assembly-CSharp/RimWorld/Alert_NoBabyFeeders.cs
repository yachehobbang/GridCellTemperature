using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class Alert_NoBabyFeeders : Alert
{
	private static List<Pawn> tmpPawnList = new List<Pawn>(32);

	public Alert_NoBabyFeeders()
	{
		defaultLabel = "AlertNoBabyFeeder".Translate();
		defaultPriority = AlertPriority.High;
		requireBiotech = true;
	}

	public override TaggedString GetExplanation()
	{
		return "AlertNoBabyFeederDesc".Translate(Faction.OfPlayer.Named("FACTION"));
	}

	public override AlertReport GetReport()
	{
		if (MapWithNoBabyFeeder(tmpPawnList) != null)
		{
			return AlertReport.CulpritsAre(tmpPawnList);
		}
		return false;
	}

	private static Map MapWithNoBabyFeeder(List<Pawn> babiesOut)
	{
		babiesOut?.Clear();
		foreach (Map map in Find.Maps)
		{
			List<Pawn> freeColonistsAndPrisoners = map.mapPawns.FreeColonistsAndPrisoners;
			for (int i = 0; i < freeColonistsAndPrisoners.Count; i++)
			{
				Pawn pawn = freeColonistsAndPrisoners[i];
				IThingHolder parentHolder;
				if (ChildcareUtility.CanSuckle(pawn, out var _) && (pawn.Spawned || (parentHolder = pawn.ParentHolder) == null || parentHolder is Pawn_CarryTracker) && !pawn.mindState.AnyAutofeeder(AutofeedMode.Urgent, (Pawn _baby, Pawn _mom) => ChildcareUtility.CanHaulBabyToMomNow(_mom, _mom, _baby, ignoreOtherReservations: true, out var _)) && !pawn.mindState.AnyAutofeeder(AutofeedMode.Childcare, (Pawn _baby, Pawn _mom) => ChildcareUtility.CanHaulBabyToMomNow(_mom, _mom, _baby, ignoreOtherReservations: true, out var _)))
				{
					if (babiesOut == null)
					{
						return map;
					}
					babiesOut.Add(pawn);
				}
			}
			if (babiesOut != null && babiesOut.Count > 0)
			{
				return map;
			}
		}
		return null;
	}
}
