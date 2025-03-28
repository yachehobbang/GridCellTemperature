using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_Root_WandererJoin_WalkIn : QuestNode_Root_WandererJoin
{
	private const int TimeoutTicks = 60000;

	public const float RelationWithColonistWeight = 20f;

	private string signalAccept;

	private string signalReject;

	protected override void RunInt()
	{
		base.RunInt();
		Quest quest = QuestGen.quest;
		quest.Delay(60000, delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Fail);
		});
	}

	public override Pawn GeneratePawn()
	{
		Slate slate = QuestGen.slate;
		Gender? fixedGender = null;
		if (!slate.TryGet<PawnGenerationRequest>("overridePawnGenParams", out var var))
		{
			var = new PawnGenerationRequest(PawnKindDefOf.Villager, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 20f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: true, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, fixedGender, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
		}
		if (Find.Storyteller.difficulty.ChildrenAllowed)
		{
			var.AllowedDevelopmentalStages |= DevelopmentalStage.Child;
		}
		Pawn pawn = PawnGenerator.GeneratePawn(var);
		if (!pawn.IsWorldPawn())
		{
			Find.WorldPawns.PassToWorld(pawn);
		}
		return pawn;
	}

	protected override void AddSpawnPawnQuestParts(Quest quest, Map map, Pawn pawn)
	{
		signalAccept = QuestGenUtility.HardcodedSignalWithQuestID("Accept");
		signalReject = QuestGenUtility.HardcodedSignalWithQuestID("Reject");
		quest.Signal(signalAccept, delegate
		{
			quest.SetFaction(Gen.YieldSingle(pawn), Faction.OfPlayer);
			quest.PawnsArrive(Gen.YieldSingle(pawn), null, map.Parent, null, joinPlayer: false, null);
			QuestGen_End.End(quest, QuestEndOutcome.Success);
		});
		quest.Signal(signalReject, delegate
		{
			quest.GiveDiedOrDownedThoughts(pawn, PawnDiedOrDownedThoughtsKind.DeniedJoining);
			QuestGen_End.End(quest, QuestEndOutcome.Fail);
		});
	}

	public override void SendLetter(Quest quest, Pawn pawn)
	{
		TaggedString title = "LetterLabelWandererJoins".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
		TaggedString letterText = "LetterWandererJoins".Translate(pawn.Named("PAWN")).AdjustedFor(pawn);
		AppendCharityInfoToLetter("JoinerCharityInfo".Translate(pawn), ref letterText);
		PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref letterText, ref title, pawn);
		if (pawn.DevelopmentalStage.Juvenile())
		{
			string arg = (pawn.ageTracker.AgeBiologicalYears * 3600000).ToStringTicksToPeriod();
			letterText += "\n\n" + "RefugeePodCrash_Child".Translate(pawn.Named("PAWN"), arg.Named("AGE"));
		}
		ChoiceLetter_AcceptJoiner choiceLetter_AcceptJoiner = (ChoiceLetter_AcceptJoiner)LetterMaker.MakeLetter(title, letterText, LetterDefOf.AcceptJoiner);
		choiceLetter_AcceptJoiner.signalAccept = signalAccept;
		choiceLetter_AcceptJoiner.signalReject = signalReject;
		choiceLetter_AcceptJoiner.quest = quest;
		choiceLetter_AcceptJoiner.StartTimeout(60000);
		Find.LetterStack.ReceiveLetter(choiceLetter_AcceptJoiner);
	}

	public static void AppendCharityInfoToLetter(TaggedString charityInfo, ref TaggedString letterText)
	{
		if (!ModsConfig.IdeologyActive)
		{
			return;
		}
		IEnumerable<Pawn> source = IdeoUtility.AllColonistsWithCharityPrecept();
		if (!source.Any())
		{
			return;
		}
		letterText += "\n\n" + charityInfo + "\n\n" + "PawnsHaveCharitableBeliefs".Translate() + ":";
		foreach (IGrouping<Ideo, Pawn> item in from c in source
			group c by c.Ideo)
		{
			letterText += "\n  - " + "BelieversIn".Translate(item.Key.name.Colorize(item.Key.TextColor), item.Select((Pawn f) => f.NameShortColored.Resolve()).ToCommaList());
		}
	}
}
