using Verse;

namespace RimWorld.SketchGen;

public class SketchResolver_AncientArmory : SketchResolver
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
		if (ModLister.CheckIdeology("Ancient armory"))
		{
			ResolveParams parms2 = parms;
			parms2.wallEdgeThing = ThingDefOf.AncientLockerBank;
			parms2.requireFloor = true;
			SketchResolverDefOf.AddWallEdgeThings.Resolve(parms2);
			ResolveParams parms3 = parms;
			parms3.thingCentral = ThingDefOf.AncientSecurityTurret;
			parms3.requireFloor = true;
			SketchResolverDefOf.AddThingsCentral.Resolve(parms3);
		}
	}
}
