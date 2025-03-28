using Verse;

namespace RimWorld;

public class Alert_QuestExpiresSoon : Alert
{
	private Quest questExpiring;

	private const int TicksToAlert = 60000;

	private Quest QuestExpiring
	{
		get
		{
			questExpiring = null;
			foreach (Quest item in Find.QuestManager.questsInDisplayOrder)
			{
				if (!item.dismissed && !item.Historical && !item.initiallyAccepted && item.State == QuestState.NotYetAccepted && item.ticksUntilAcceptanceExpiry > 0 && item.ticksUntilAcceptanceExpiry < 60000)
				{
					questExpiring = item;
					break;
				}
			}
			return questExpiring;
		}
	}

	public Alert_QuestExpiresSoon()
	{
		defaultPriority = AlertPriority.High;
	}

	protected override void OnClick()
	{
		if (questExpiring != null)
		{
			Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests);
			((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(questExpiring);
		}
	}

	public override string GetLabel()
	{
		if (questExpiring == null)
		{
			return string.Empty;
		}
		return "QuestExpiresSoon".Translate(questExpiring.ticksUntilAcceptanceExpiry.ToStringTicksToPeriodVerbose());
	}

	public override TaggedString GetExplanation()
	{
		if (questExpiring == null)
		{
			return string.Empty;
		}
		return "QuestExpiresSoonDesc".Translate(questExpiring.name, questExpiring.ticksUntilAcceptanceExpiry.ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor));
	}

	public override AlertReport GetReport()
	{
		return QuestExpiring != null;
	}
}
