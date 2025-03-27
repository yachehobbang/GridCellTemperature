using Verse;

namespace RimWorld.SketchGen;

public class SketchResolver_AncientMechGeneratorRoom : SketchResolver
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
		if (ModLister.CheckBiotech("Ancient mechanitor generator room"))
		{
			ResolveParams parms2 = parms;
			parms2.thingCentral = ThingDefOf.AncientToxifierGenerator;
			SketchResolverDefOf.AddThingsCentral.Resolve(parms2);
		}
	}
}
