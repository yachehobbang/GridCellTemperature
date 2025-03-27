using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class PlaceWorker_Conduit : PlaceWorker
{
	public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
	{
		List<Thing> thingList = loc.GetThingList(map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (thingList[i].def.EverTransmitsPower)
			{
				return false;
			}
			if (thingList[i].def.entityDefToBuild != null && thingList[i].def.entityDefToBuild is ThingDef { EverTransmitsPower: not false })
			{
				return false;
			}
		}
		return true;
	}
}
