using Verse;

namespace RimWorld;

public class IncidentWorker_Nociosphere : IncidentWorker
{
	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		IntVec3 result = parms.spawnCenter;
		if (!result.IsValid && !RCellFinder.TryFindRandomSpotJustOutsideColony(parms.spawnCenter, map, out result))
		{
			return false;
		}
		for (int i = 0; i < map.mapPawns.AllPawnsCount; i++)
		{
			if (map.mapPawns.AllPawns[i].kindDef == PawnKindDefOf.Nociosphere)
			{
				return false;
			}
		}
		Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Nociosphere, Faction.OfEntities, PawnGenerationContext.NonPlayer, map.Tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
		NociosphereUtility.SkipTo((Pawn)GenSpawn.Spawn(pawn, result, map), result);
		SendStandardLetter(parms, pawn);
		return true;
	}
}
