using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_Root_MonolithMigration : QuestNode
{
	public static readonly IntRange SpawnDelayRangeTicks = new IntRange(2500, 5000);

	protected override bool TestRunInt(Slate slate)
	{
		if (!ModsConfig.AnomalyActive)
		{
			return false;
		}
		if (!Find.Anomaly.GenerateMonolith)
		{
			return false;
		}
		Map map = QuestGen_Get.GetMap(mustBeInfestable: false, null);
		if (map == null)
		{
			return false;
		}
		if (map.listerThings.AnyThingWithDef(ThingDefOf.VoidMonolith))
		{
			return false;
		}
		return true;
	}

	protected override void RunInt()
	{
		Quest quest = QuestGen.quest;
		Slate slate = QuestGen.slate;
		Map map = QuestGen_Get.GetMap(mustBeInfestable: false, null);
		QuestPart_Choice questPart_Choice = quest.RewardChoice();
		QuestPart_Choice.Choice item = new QuestPart_Choice.Choice
		{
			rewards = { (Reward)new Reward_Unknown() }
		};
		questPart_Choice.choices.Add(item);
		slate.Set("askerIsNull", var: true);
		string questTagToAdd = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("monolithMap");
		QuestUtility.AddQuestTag(ref map.Parent.questTags, questTagToAdd);
		string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("monolithMap.MapRemoved");
		string startSpawnMonolithSignal = QuestGen.GenerateNewSignal("SpawnMonolith");
		string spawnMonolithComplete = QuestGen.GenerateNewSignal("SpawnMonolithComplete");
		Find.Anomaly.TryGetCellForMonolithSpawn(map, out var cell);
		quest.Delay(SpawnDelayRangeTicks.RandomInRange, delegate
		{
			quest.SignalPass(null, null, startSpawnMonolithSignal);
		});
		quest.SpawnThing(map, ThingMaker.MakeThing(ThingDefOf.VoidStructureIncoming), null, cell, startSpawnMonolithSignal);
		quest.Delay(EffecterDefOf.VoidStructureIncoming.maintainTicks, delegate
		{
			quest.SignalPass(null, null, spawnMonolithComplete);
			quest.AddPart(new QuestPart_SpawnMonolith(spawnMonolithComplete, cell, map));
		}, startSpawnMonolithSignal);
		quest.End(QuestEndOutcome.Fail, 0, null, inSignal, QuestPart.SignalListenMode.OngoingOrNotYetAccepted, sendStandardLetter: true);
	}
}
