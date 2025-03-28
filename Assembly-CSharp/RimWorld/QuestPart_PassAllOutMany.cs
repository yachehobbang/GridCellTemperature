using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class QuestPart_PassAllOutMany : QuestPart
{
	public List<string> inSignals = new List<string>();

	public List<string> outSignals = new List<string>();

	private List<bool> signalsReceived = new List<bool>();

	private bool AllSignalsReceived => PassAllQuestPartUtility.AllReceived(inSignals, signalsReceived);

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		if (AllSignalsReceived)
		{
			return;
		}
		int num = inSignals.IndexOf(signal.tag);
		if (num < 0)
		{
			return;
		}
		while (signalsReceived.Count <= num)
		{
			signalsReceived.Add(item: false);
		}
		signalsReceived[num] = true;
		if (AllSignalsReceived)
		{
			for (int i = 0; i < outSignals.Count; i++)
			{
				Find.SignalManager.SendSignal(new Signal(outSignals[i]));
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref inSignals, "inSignals", LookMode.Value);
		Scribe_Collections.Look(ref outSignals, "outSignals", LookMode.Value);
		Scribe_Collections.Look(ref signalsReceived, "signalsReceived", LookMode.Value);
	}

	public override void AssignDebugData()
	{
		base.AssignDebugData();
		inSignals.Clear();
		for (int i = 0; i < 3; i++)
		{
			inSignals.Add("DebugSignal" + Rand.Int);
			outSignals.Add("DebugSignal" + Rand.Int);
		}
	}
}
