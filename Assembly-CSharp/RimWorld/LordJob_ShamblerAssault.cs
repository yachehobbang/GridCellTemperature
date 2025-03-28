using Verse.AI.Group;

namespace RimWorld;

public class LordJob_ShamblerAssault : LordJob
{
	public override StateGraph CreateGraph()
	{
		StateGraph stateGraph = new StateGraph();
		LordToil toil = new LordToil_ShamblerAssault();
		stateGraph.AddToil(toil);
		return stateGraph;
	}
}
