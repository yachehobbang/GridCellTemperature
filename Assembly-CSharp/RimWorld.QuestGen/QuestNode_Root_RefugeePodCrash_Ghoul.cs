using RimWorld.Planet;
using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_Root_RefugeePodCrash_Ghoul : QuestNode_Root_WandererJoin
{
	private static readonly IntRange ShockTicksRange = new IntRange(10000, 15000);

	private bool wasStartingPawn;

	public override Pawn GeneratePawn()
	{
		Pawn pawn;
		if (Find.World.worldPawns.GetPawnsBySituationCount(WorldPawnSituation.StartingPawnLeftBehind) > 0)
		{
			pawn = Find.World.worldPawns.GetPawnsBySituation(WorldPawnSituation.StartingPawnLeftBehind).RandomElement();
			MutantUtility.SetFreshPawnAsMutant(pawn, MutantDefOf.Ghoul);
			pawn.SetFaction(Faction.OfEntities);
			wasStartingPawn = true;
		}
		else
		{
			pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Ghoul, Faction.OfEntities);
		}
		pawn.health.AddHediff(HediffDefOf.DarkPsychicShock, null, null).TryGetComp<HediffComp_Disappears>().ticksToDisappear = ShockTicksRange.RandomInRange;
		return pawn;
	}

	public override void SendLetter(Quest quest, Pawn pawn)
	{
		TaggedString title = "GhoulPodCrashLabel".Translate();
		TaggedString text = (wasStartingPawn ? "GhoulPodCrashStartingPawnText".Translate(pawn.Named("PAWN")).AdjustedFor(pawn) : "GhoulPodCrashText".Translate());
		PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref title, pawn);
		Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.ThreatSmall, new TargetInfo(pawn));
	}
}
