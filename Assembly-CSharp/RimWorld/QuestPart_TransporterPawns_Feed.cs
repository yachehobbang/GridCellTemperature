using Verse;

namespace RimWorld;

public class QuestPart_TransporterPawns_Feed : QuestPart_TransporterPawns
{
	public override void Process(Pawn pawn)
	{
		pawn.needs.food.CurLevel = pawn.needs.food.MaxLevel;
	}
}
