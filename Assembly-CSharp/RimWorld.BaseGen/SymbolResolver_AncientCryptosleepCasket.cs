using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RimWorld.BaseGen;

public class SymbolResolver_AncientCryptosleepCasket : SymbolResolver
{
	public override void Resolve(ResolveParams rp)
	{
		int groupID = rp.ancientCryptosleepCasketGroupID ?? Find.UniqueIDsManager.GetNextAncientCryptosleepCasketGroupID();
		PodContentsType value = rp.podContentsType ?? Gen.RandomEnumValue<PodContentsType>(disallowFirstValue: true);
		Rot4 rot = rp.thingRot ?? Rot4.North;
		Building_AncientCryptosleepCasket building_AncientCryptosleepCasket = (Building_AncientCryptosleepCasket)ThingMaker.MakeThing(ThingDefOf.AncientCryptosleepCasket);
		building_AncientCryptosleepCasket.groupID = groupID;
		ThingSetMakerParams parms = default(ThingSetMakerParams);
		parms.podContentsType = value;
		List<Thing> list = ThingSetMakerDefOf.MapGen_AncientPodContents.root.Generate(parms);
		for (int i = 0; i < list.Count; i++)
		{
			if (!building_AncientCryptosleepCasket.TryAcceptThing(list[i], allowSpecialEffects: false))
			{
				if (list[i] is Pawn pawn)
				{
					Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
				}
				else
				{
					list[i].Destroy();
				}
			}
		}
		IntVec3 randomCell = rp.rect.RandomCell;
		GenSpawn.Spawn(building_AncientCryptosleepCasket, randomCell, BaseGen.globalSettings.map, rot);
		if (rp.ancientCryptosleepCasketOpenSignalTag != null)
		{
			SignalAction_OpenCasket obj = (SignalAction_OpenCasket)ThingMaker.MakeThing(ThingDefOf.SignalAction_OpenCasket);
			obj.signalTag = rp.ancientCryptosleepCasketOpenSignalTag;
			obj.caskets.Add(building_AncientCryptosleepCasket);
			GenSpawn.Spawn(obj, randomCell, BaseGen.globalSettings.map);
		}
	}
}
