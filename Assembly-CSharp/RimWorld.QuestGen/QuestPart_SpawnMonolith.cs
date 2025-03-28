using Verse;

namespace RimWorld.QuestGen;

public class QuestPart_SpawnMonolith : QuestPart
{
	private string inSignal;

	private Map map;

	private IntVec3 spawnCell = IntVec3.Invalid;

	public QuestPart_SpawnMonolith()
	{
	}

	public QuestPart_SpawnMonolith(string inSignal, IntVec3 spawnCell, Map map)
	{
		this.inSignal = inSignal;
		this.spawnCell = spawnCell;
		this.map = map;
	}

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		if (!(signal.tag != inSignal))
		{
			Building_VoidMonolith building_VoidMonolith = Find.Anomaly.SpawnNewMonolith(spawnCell, map);
			TaggedString text = "MonolithArrivalText".Translate();
			if (Find.Anomaly.Level > 0)
			{
				text += "\n\n" + "MonolithArrivalTextExt".Translate();
			}
			Find.LetterStack.ReceiveLetter("MonolithArrivalLabel".Translate(), text, LetterDefOf.NeutralEvent, building_VoidMonolith);
			quest.End(QuestEndOutcome.Success, sendLetter: false);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref spawnCell, "spawnCell");
		Scribe_Values.Look(ref inSignal, "inSignal");
		Scribe_References.Look(ref map, "map");
	}

	public override void Cleanup()
	{
		base.Cleanup();
		map = null;
	}
}
