using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class Alert_NeedMechChargers : Alert
{
	private List<Pawn> mechs = new List<Pawn>();

	private List<Building_MechCharger> chargers = new List<Building_MechCharger>();

	private List<ThingDef> allChargerDefs = new List<ThingDef>();

	private List<ThingDef> requiredChargerDefs = new List<ThingDef>();

	public Alert_NeedMechChargers()
	{
		defaultLabel = "AlertNeedMechChargers".Translate();
		requireBiotech = true;
	}

	public override AlertReport GetReport()
	{
		RecacheMechsAndChargers();
		return requiredChargerDefs.Count > 0;
	}

	private void RecacheMechsAndChargers()
	{
		chargers.Clear();
		mechs.Clear();
		List<Map> maps = Find.Maps;
		if (allChargerDefs == null)
		{
			allChargerDefs = new List<ThingDef>();
			allChargerDefs.AddRange(DefDatabase<ThingDef>.AllDefs.Where((ThingDef t) => ThingRequestGroup.MechCharger.Includes(t)));
		}
		for (int i = 0; i < maps.Count; i++)
		{
			List<Thing> list = maps[i].listerThings.ThingsInGroup(ThingRequestGroup.MechCharger);
			for (int j = 0; j < list.Count; j++)
			{
				Building_MechCharger building_MechCharger = (Building_MechCharger)list[j];
				if (building_MechCharger.Faction == Faction.OfPlayer)
				{
					chargers.Add(building_MechCharger);
				}
			}
		}
		for (int k = 0; k < maps.Count; k++)
		{
			List<Pawn> list2 = maps[k].mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
			for (int l = 0; l < list2.Count; l++)
			{
				if (list2[l].IsColonyMech)
				{
					mechs.Add(list2[l]);
				}
			}
		}
		requiredChargerDefs.Clear();
		for (int m = 0; m < mechs.Count; m++)
		{
			Pawn mech = mechs[m];
			if (!chargers.Any((Building_MechCharger c) => c.IsCompatibleWithCharger(mech.kindDef)))
			{
				ThingDef thingDef = allChargerDefs.FirstOrDefault((ThingDef c) => Building_MechCharger.IsCompatibleWithCharger(c, mech.kindDef));
				if (thingDef != null && !requiredChargerDefs.Contains(thingDef))
				{
					requiredChargerDefs.Add(thingDef);
				}
			}
		}
	}

	public override TaggedString GetExplanation()
	{
		TaggedString result = "AlertNeedMechChargersDesc".Translate(requiredChargerDefs.Select((ThingDef c) => c.LabelCap.Resolve()).ToLineList("  - "));
		if (!ResearchProjectDefOf.BasicMechtech.IsFinished)
		{
			result += "\n\n" + "AlertNeedMechChargerBasicMechtech".Translate();
		}
		return result;
	}
}
