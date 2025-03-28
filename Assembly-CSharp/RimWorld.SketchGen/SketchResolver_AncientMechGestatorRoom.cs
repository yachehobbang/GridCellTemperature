using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld.SketchGen;

public class SketchResolver_AncientMechGestatorRoom : SketchResolver
{
	private const float MechanoidDebrisChance = 0.05f;

	private static IEnumerable<ThingDef> CentralThings
	{
		get
		{
			yield return ThingDefOf.AncientMechGestator;
			yield return ThingDefOf.AncientLargeMechGestator;
		}
	}

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
		if (!ModLister.CheckBiotech("Ancient mech gestator room"))
		{
			return;
		}
		ResolveParams parms2 = parms;
		parms2.cornerThing = ThingDefOf.AncientLamp;
		parms2.requireFloor = true;
		SketchResolverDefOf.AddCornerThings.Resolve(parms2);
		foreach (ThingDef centralThing in CentralThings)
		{
			ResolveParams parms3 = parms;
			parms3.thingCentral = centralThing;
			parms3.requireFloor = true;
			SketchResolverDefOf.AddThingsCentral.Resolve(parms3);
		}
		if (!ModsConfig.IdeologyActive)
		{
			return;
		}
		foreach (IntVec3 cell in parms.rect.Value.Cells)
		{
			if (Rand.Chance(0.05f) && !parms.sketch.ThingsAt(cell).Any())
			{
				parms.sketch.AddThing(ThingDefOf.ChunkMechanoidSlag, cell, ThingDefOf.ChunkMechanoidSlag.defaultPlacingRot, null, 1, null, null);
			}
		}
	}
}
