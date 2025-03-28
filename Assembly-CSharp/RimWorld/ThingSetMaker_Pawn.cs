using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class ThingSetMaker_Pawn : ThingSetMaker
{
	public PawnKindDef pawnKind;

	public bool? alive;

	public FloatRange? corpseAgeRangeDays;

	protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
	{
		Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
		outThings.Add(pawn);
		if (!alive.HasValue || alive.Value)
		{
			return;
		}
		pawn.Kill(null);
		if (corpseAgeRangeDays.HasValue)
		{
			int num = Mathf.RoundToInt(corpseAgeRangeDays.Value.RandomInRange * 60000f);
			pawn.Corpse.timeOfDeath = Find.TickManager.TicksGame - num;
			CompRottable compRottable = pawn.Corpse.TryGetComp<CompRottable>();
			if (compRottable != null)
			{
				compRottable.RotProgress += num;
			}
		}
	}

	protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
	{
		yield return pawnKind.race;
	}

	public override IEnumerable<string> ConfigErrors()
	{
		if (pawnKind == null)
		{
			yield return "pawnKind is null.";
		}
	}
}
