using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class Alert_NeedWarden : Alert
{
	public Alert_NeedWarden()
	{
		defaultLabel = "NeedWarden".Translate();
		defaultExplanation = "NeedWardenDesc".Translate();
		defaultPriority = AlertPriority.High;
	}

	public override AlertReport GetReport()
	{
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			Map map = maps[i];
			if (!map.IsPlayerHome || !map.mapPawns.PrisonersOfColonySpawned.Any())
			{
				continue;
			}
			bool flag = false;
			foreach (Pawn freeColonist in map.mapPawns.FreeColonists)
			{
				if ((freeColonist.Spawned || freeColonist.BrieflyDespawned()) && !freeColonist.Downed && freeColonist.workSettings != null && freeColonist.workSettings.GetPriority(WorkTypeDefOf.Warden) > 0)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return AlertReport.CulpritIs(map.mapPawns.PrisonersOfColonySpawned[0]);
			}
		}
		return false;
	}
}
