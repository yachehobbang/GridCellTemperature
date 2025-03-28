using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class MainButtonWorker_ToggleMechTab : MainButtonWorker_ToggleTab
{
	public override bool Disabled
	{
		get
		{
			if (base.Disabled)
			{
				return true;
			}
			Map currentMap = Find.CurrentMap;
			if (currentMap != null)
			{
				List<Pawn> list = currentMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].RaceProps.IsMechanoid)
					{
						return false;
					}
				}
				List<Pawn> list2 = currentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);
				for (int j = 0; j < list2.Count; j++)
				{
					if (list2[j].RaceProps.IsMechanoid)
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	public override bool Visible => !Disabled;
}
