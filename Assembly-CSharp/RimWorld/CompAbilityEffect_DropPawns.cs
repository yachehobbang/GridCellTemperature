using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class CompAbilityEffect_DropPawns : CompAbilityEffect_WithDest
{
	public new CompProperties_DropPawns Props => (CompProperties_DropPawns)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		List<Pawn> list = new List<Pawn>();
		for (int i = 0; i < Props.amount; i++)
		{
			Pawn item = PawnGenerator.GeneratePawn(new PawnGenerationRequest(Props.pawnKindDef, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
			list.Add(item);
		}
		DropPodUtility.DropThingsNear(target.Cell, parent.pawn.Map, list);
	}
}
