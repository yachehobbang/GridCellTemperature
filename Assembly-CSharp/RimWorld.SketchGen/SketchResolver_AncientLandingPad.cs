using Verse;

namespace RimWorld.SketchGen;

public class SketchResolver_AncientLandingPad : SketchResolver
{
	protected override bool CanResolveInt(ResolveParams parms)
	{
		return parms.sketch != null;
	}

	protected override void ResolveInt(ResolveParams parms)
	{
		if (!ModLister.CheckIdeology("Ancient landing pad"))
		{
			return;
		}
		Sketch sketch = new Sketch();
		IntVec2 intVec = parms.landingPadSize ?? new IntVec2(12, 12);
		CellRect cellRect = new CellRect(0, 0, intVec.x, intVec.z);
		foreach (IntVec3 item in cellRect)
		{
			sketch.AddTerrain(TerrainDefOf.Concrete, item);
		}
		foreach (IntVec3 corner in cellRect.Corners)
		{
			sketch.AddThing(ThingDefOf.AncientShipBeacon, corner, Rot4.North, null, 1, null, null);
		}
		parms.sketch.Merge(sketch);
		ResolveParams parms2 = parms;
		parms2.destroyChanceExp = 5f;
		SketchResolverDefOf.DamageBuildings.Resolve(parms2);
	}
}
