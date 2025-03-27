using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public class RoomContentsAnimalCorpses : RoomContentsWorker
{
	private static readonly FloatRange CorpseAgeDaysRange = new FloatRange(3f, 30f);

	private static readonly IntRange CorpseCountRange = new IntRange(1, 3);

	public override void FillRoom(Map map, LayoutRoom room)
	{
		PawnKindDef kind = DefDatabase<PawnKindDef>.AllDefsListForReading.Where((PawnKindDef k) => k.RaceProps.Animal && k.RaceProps.CanDoHerdMigration).RandomElementByWeight((PawnKindDef x) => Mathf.Lerp(0.2f, 1f, x.RaceProps.wildness));
		int randomInRange = CorpseCountRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			SpawnCorpse(map, room, kind);
		}
	}

	private static Corpse SpawnCorpse(Map map, LayoutRoom room, PawnKindDef kind)
	{
		if (!room.TryGetRandomCellInRoom(map, out var cell, 2))
		{
			return null;
		}
		Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
		pawn.health.SetDead();
		Corpse corpse = pawn.MakeCorpse(null, null);
		corpse.Age = Mathf.RoundToInt(CorpseAgeDaysRange.RandomInRange * 60000f);
		corpse.GetComp<CompRottable>().RotProgress += corpse.Age;
		Find.WorldPawns.PassToWorld(pawn);
		return (Corpse)GenSpawn.Spawn(corpse, cell, map);
	}
}
