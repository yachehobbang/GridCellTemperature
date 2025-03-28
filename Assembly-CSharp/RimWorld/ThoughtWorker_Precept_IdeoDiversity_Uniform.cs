using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class ThoughtWorker_Precept_IdeoDiversity_Uniform : ThoughtWorker_Precept
{
	protected override ThoughtState ShouldHaveThought(Pawn p)
	{
		if (p.Faction == null || !p.IsColonist)
		{
			return false;
		}
		List<Pawn> list = p.Map.mapPawns.SpawnedPawnsInFaction(p.Faction);
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (((list[i] != p && list[i].RaceProps.Humanlike) || list[i].DevelopmentalStage.Baby()) && !list[i].IsSlave && !list[i].IsQuestLodger() && !list[i].IsGhoul)
			{
				if (list[i].Ideo != p.Ideo)
				{
					return false;
				}
				num++;
			}
		}
		return num > 0;
	}
}
