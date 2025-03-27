using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RimWorld;

public class ThingSetMaker_RefugeePod : ThingSetMaker
{
	private const float RelationWithColonistWeight = 20f;

	protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
	{
		PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.SpaceRefugee, DownedRefugeeQuestUtility.GetRandomFactionForRefugee(), PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 20f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: true, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null);
		int num = 0;
		Pawn pawn = null;
		while (num < 10 && (pawn == null || !pawn.Downed))
		{
			num++;
			if (pawn != null)
			{
				Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
			}
			pawn = PawnGenerator.GeneratePawn(request);
			HealthUtility.DamageUntilDowned(pawn);
		}
		outThings.Add(pawn);
	}

	protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
	{
		yield return PawnKindDefOf.SpaceRefugee.race;
	}
}
