using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public class RoomContentsAnimals : RoomContentsWorker
{
	private static readonly IntRange AnimalCount = new IntRange(1, 3);

	private static readonly FloatRange WildnessRange = new FloatRange(0.2f, 1f);

	public override void FillRoom(Map map, LayoutRoom room)
	{
		PawnKindDef kind = DefDatabase<PawnKindDef>.AllDefsListForReading.Where((PawnKindDef k) => k.RaceProps.Animal && k.RaceProps.CanDoHerdMigration).RandomElementByWeight((PawnKindDef x) => Mathf.Lerp(WildnessRange.min, WildnessRange.max, x.RaceProps.wildness));
		int randomInRange = AnimalCount.RandomInRange;
		PawnGenerationRequest request = new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null);
		for (int i = 0; i < randomInRange; i++)
		{
			if (!room.TryGetRandomCellInRoom(map, out var cell, 2))
			{
				break;
			}
			GenSpawn.Spawn(PawnGenerator.GeneratePawn(request), cell, map);
		}
	}
}
