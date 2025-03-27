using Verse;

namespace RimWorld.QuestGen;

public abstract class QuestNode_Root_WandererJoin : QuestNode
{
	protected virtual int AllowKilledBeforeTicks => 15000;

	public abstract Pawn GeneratePawn();

	public abstract void SendLetter(Quest quest, Pawn pawn);

	protected virtual void AddSpawnPawnQuestParts(Quest quest, Map map, Pawn pawn)
	{
		quest.DropPods(map.Parent, Gen.YieldSingle(pawn), null, null, null, null, false, useTradeDropSpot: false, joinPlayer: false, makePrisoners: false, null, null, QuestPart.SignalListenMode.OngoingOnly, null);
	}

	protected virtual void AddLeftMapQuestParts(Quest quest, Pawn pawn)
	{
		quest.AnyPawnUnhealthy(Gen.YieldSingle(pawn), delegate
		{
			QuestGen_End.End(quest, QuestEndOutcome.Fail);
		}, delegate
		{
			quest.AnyColonistWithCharityPrecept(delegate
			{
				quest.Message("MessageCharityEventFulfilled".Translate() + ": " + "MessageWandererLeftHealthy".Translate(pawn), MessageTypeDefOf.PositiveEvent, getLookTargetsFromSignal: false, null, pawn);
			});
			QuestGen_End.End(quest, QuestEndOutcome.Success);
		});
	}

	protected override void RunInt()
	{
		Quest quest = QuestGen.quest;
		Slate slate = QuestGen.slate;
		if (!slate.TryGet<Map>("map", out var var))
		{
			var = QuestGen_Get.GetMap(mustBeInfestable: false, null);
		}
		Pawn pawn = GeneratePawn();
		AddSpawnPawnQuestParts(quest, var, pawn);
		slate.Set("pawn", pawn);
		SendLetter(quest, pawn);
		string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Killed");
		string inSignal2 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.PlayerTended");
		string inSignal3 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.LeftMap");
		string inSignal4 = QuestGenUtility.HardcodedSignalWithQuestID("pawn.Recruited");
		quest.End(QuestEndOutcome.Success, 0, null, inSignal2);
		quest.Signal(inSignal, delegate
		{
			quest.AcceptedAfterTicks(AllowKilledBeforeTicks, delegate
			{
				quest.AnyColonistWithCharityPrecept(delegate
				{
					quest.Message("MessageCharityEventRefused".Translate() + ": " + "MessageWandererLeftToDie".Translate(pawn), MessageTypeDefOf.NegativeEvent, getLookTargetsFromSignal: false, null, pawn);
				});
				QuestGen_End.End(quest, QuestEndOutcome.Fail);
			}, delegate
			{
				QuestGen_End.End(quest, QuestEndOutcome.Fail);
			});
		});
		quest.AnyColonistWithCharityPrecept(delegate
		{
			quest.Message("MessageCharityEventFulfilled".Translate() + ": " + "MessageWandererRecruited".Translate(pawn), MessageTypeDefOf.PositiveEvent, getLookTargetsFromSignal: false, null, pawn);
		}, null, inSignal4);
		quest.End(QuestEndOutcome.Success, 0, null, inSignal4);
		quest.Signal(inSignal3, delegate
		{
			AddLeftMapQuestParts(quest, pawn);
		});
	}

	protected override bool TestRunInt(Slate slate)
	{
		return true;
	}
}
