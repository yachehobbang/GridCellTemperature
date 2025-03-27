using System.Linq;
using Verse;

namespace RimWorld;

public class QuestPart_RequirementsToAcceptNoOngoingBestowingCeremony : QuestPart_RequirementsToAccept
{
	public override AcceptanceReport CanAccept()
	{
		if (Find.QuestManager.QuestsListForReading.Where((Quest q) => q.State == QuestState.Ongoing && q.root == QuestScriptDefOf.BestowingCeremony).Any())
		{
			return new AcceptanceReport("QuestCanNotStartUntilBestowingCeremonyFinished".Translate());
		}
		return true;
	}
}
