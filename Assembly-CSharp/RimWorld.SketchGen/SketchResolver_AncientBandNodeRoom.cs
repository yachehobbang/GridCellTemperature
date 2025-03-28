using Verse;

namespace RimWorld.SketchGen;

public class SketchResolver_AncientBandNodeRoom : SketchResolver
{
	protected override bool CanResolveInt(ResolveParams parms)
	{
		if (parms.rect.HasValue)
		{
			return parms.sketch != null;
		}
		return false;
	}

	protected override void ResolveInt(ResolveParams parms)
	{
		if (ModLister.CheckBiotech("Ancient band node room"))
		{
			ResolveParams parms2 = parms;
			parms2.wallEdgeThing = ThingDefOf.AncientBandNode;
			SketchResolverDefOf.AddWallEdgeThings.Resolve(parms2);
		}
	}
}
