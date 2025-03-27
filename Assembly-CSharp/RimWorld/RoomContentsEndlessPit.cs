using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class RoomContentsEndlessPit : RoomContentsWorker
{
	private IEnumerable<ThingDef> PitDefs
	{
		get
		{
			yield return ThingDefOf.EndlessPit2x2c;
			yield return ThingDefOf.EndlessPit3x2c;
			yield return ThingDefOf.EndlessPit3x3c;
		}
	}

	public override void FillRoom(Map map, LayoutRoom room)
	{
		if (room.TryGetRandomCellInRoom(map, out var cell, 3))
		{
			ThingDef thingDef = PitDefs.RandomElement();
			GenSpawn.Spawn(ThingMaker.MakeThing(thingDef), cell, map, thingDef.rotatable ? Rot4.Random : Rot4.North);
		}
	}
}
