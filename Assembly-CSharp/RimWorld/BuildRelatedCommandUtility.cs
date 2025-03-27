using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public static class BuildRelatedCommandUtility
{
	public static IEnumerable<Command> RelatedBuildCommands(BuildableDef building)
	{
		foreach (Command item in BuildFacilityCommandUtility.BuildFacilityCommands(building))
		{
			yield return item;
		}
		List<ThingDef> list = (building as ThingDef)?.building?.relatedBuildCommands;
		if (list == null)
		{
			yield break;
		}
		foreach (ThingDef item2 in list)
		{
			if (ModsConfig.IdeologyActive && building.ideoBuilding && item2.ideoBuilding)
			{
				Ideo ideo2 = Find.FactionManager.OfPlayer.ideos.AllIdeos.FirstOrDefault((Ideo i) => IdeoHasBuilding(i, (ThingDef)building));
				if (ideo2 != null)
				{
					bool flag = false;
					foreach (Ideo allIdeo in Find.FactionManager.OfPlayer.ideos.AllIdeos)
					{
						if (allIdeo == ideo2 && IdeoHasBuilding(allIdeo, item2))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						continue;
					}
				}
			}
			Designator_Build designator_Build = BuildCopyCommandUtility.FindAllowedDesignator(item2);
			if (designator_Build != null)
			{
				yield return designator_Build;
			}
		}
		static bool IdeoHasBuilding(Ideo ideo, ThingDef td)
		{
			if (!ideo.HasPreceptForBuilding(td))
			{
				return ideo.PreceptsListForReading.Any((Precept p) => p is Precept_RitualSeat precept_RitualSeat && precept_RitualSeat.ThingDef == td);
			}
			return true;
		}
	}
}
