using System.Collections.Generic;
using System.Text;
using Verse;

namespace RimWorld;

public class Alert_Exhaustion : Alert
{
	private List<Pawn> exhaustedColonistsResult = new List<Pawn>();

	private StringBuilder sb = new StringBuilder();

	private List<Pawn> ExhaustedColonists
	{
		get
		{
			exhaustedColonistsResult.Clear();
			List<Pawn> allMaps_FreeColonists = PawnsFinder.AllMaps_FreeColonists;
			for (int i = 0; i < allMaps_FreeColonists.Count; i++)
			{
				if ((allMaps_FreeColonists[i].Spawned || allMaps_FreeColonists[i].BrieflyDespawned()) && allMaps_FreeColonists[i].needs.rest != null && allMaps_FreeColonists[i].needs.rest.CurCategory == RestCategory.Exhausted)
				{
					exhaustedColonistsResult.Add(allMaps_FreeColonists[i]);
				}
			}
			return exhaustedColonistsResult;
		}
	}

	public Alert_Exhaustion()
	{
		defaultLabel = "Exhaustion".Translate();
		defaultPriority = AlertPriority.High;
	}

	public override TaggedString GetExplanation()
	{
		sb.Length = 0;
		foreach (Pawn exhaustedColonist in ExhaustedColonists)
		{
			sb.AppendLine("  - " + exhaustedColonist.NameShortColored.Resolve());
		}
		return "ExhaustionDesc".Translate(sb.ToString());
	}

	public override AlertReport GetReport()
	{
		return AlertReport.CulpritsAre(ExhaustedColonists);
	}
}
