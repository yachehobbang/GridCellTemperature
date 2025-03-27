using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class Alert_HunterLacksRangedWeapon : Alert
{
	private List<Pawn> huntersWithoutRangedWeaponResult = new List<Pawn>();

	private List<Pawn> HuntersWithoutRangedWeapon
	{
		get
		{
			huntersWithoutRangedWeaponResult.Clear();
			foreach (Pawn allMaps_FreeColonist in PawnsFinder.AllMaps_FreeColonists)
			{
				if ((allMaps_FreeColonist.Spawned || allMaps_FreeColonist.BrieflyDespawned()) && allMaps_FreeColonist.workSettings.WorkIsActive(WorkTypeDefOf.Hunting) && !WorkGiver_HunterHunt.HasHuntingWeapon(allMaps_FreeColonist) && !allMaps_FreeColonist.Downed && (!HealthAIUtility.ShouldSeekMedicalRest(allMaps_FreeColonist) || !allMaps_FreeColonist.InBed()))
				{
					huntersWithoutRangedWeaponResult.Add(allMaps_FreeColonist);
				}
			}
			return huntersWithoutRangedWeaponResult;
		}
	}

	public Alert_HunterLacksRangedWeapon()
	{
		defaultLabel = "HunterLacksWeapon".Translate();
		defaultPriority = AlertPriority.High;
	}

	public override AlertReport GetReport()
	{
		return AlertReport.CulpritsAre(HuntersWithoutRangedWeapon);
	}

	public override TaggedString GetExplanation()
	{
		return "HunterLacksWeaponDesc".Translate() + ":\n" + huntersWithoutRangedWeaponResult.Select((Pawn x) => x.NameShortColored.Resolve()).ToLineList("  - ", capitalizeItems: true);
	}
}
