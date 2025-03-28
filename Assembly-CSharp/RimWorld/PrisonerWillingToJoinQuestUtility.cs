using Verse;

namespace RimWorld;

public static class PrisonerWillingToJoinQuestUtility
{
	private const float RelationWithColonistWeight = 75f;

	public static Pawn GeneratePrisoner(int tile, Faction hostFaction)
	{
		PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.Slave, hostFaction, PawnGenerationContext.NonPlayer, tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 75f, forceAddFreeWarmLayerIfNeeded: true, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: true, worldPawnFactionDoesntMatter: true, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null);
		if (Find.Storyteller.difficulty.ChildrenAllowed)
		{
			request.AllowedDevelopmentalStages |= DevelopmentalStage.Child;
		}
		Pawn pawn = PawnGenerator.GeneratePawn(request);
		pawn.guest.SetGuestStatus(hostFaction, GuestStatus.Prisoner);
		return pawn;
	}
}
