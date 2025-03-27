using Verse;

namespace RimWorld.Planet;

public class PocketMapParent : MapParent
{
	public Map sourceMap;

	public bool canBeCleaned;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref sourceMap, "sourceMap");
		Scribe_Values.Look(ref canBeCleaned, "canBeCleaned", defaultValue: false);
	}
}
