using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class Alert_TitleRequiresBedroom : Alert
{
	private List<Pawn> targetsResult = new List<Pawn>();

	public List<Pawn> Targets
	{
		get
		{
			targetsResult.Clear();
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				if (!maps[i].IsPlayerHome)
				{
					continue;
				}
				foreach (Pawn freeColonist in maps[i].mapPawns.FreeColonists)
				{
					if (freeColonist.royalty != null && freeColonist.royalty.CanRequireBedroom() && freeColonist.royalty.HighestTitleWithBedroomRequirements() != null && !freeColonist.Suspended && !freeColonist.royalty.HasPersonalBedroom())
					{
						targetsResult.Add(freeColonist);
					}
				}
			}
			return targetsResult;
		}
	}

	public Alert_TitleRequiresBedroom()
	{
		defaultLabel = "NeedBedroomAssigned".Translate();
		defaultExplanation = "NeedBedroomAssignedDesc".Translate();
		requireRoyalty = true;
	}

	public override AlertReport GetReport()
	{
		return AlertReport.CulpritsAre(Targets);
	}

	public override TaggedString GetExplanation()
	{
		string text = defaultExplanation;
		if (MoveColonyUtility.TitleAndRoleRequirementsGracePeriodActive)
		{
			text += "\n\n" + "RoomRequirementGracePeriodDesc".Translate(MoveColonyUtility.TitleAndRoleRequirementGracePeriodTicksLeft.TicksToDays().ToString("0.0"));
		}
		return text;
	}
}
