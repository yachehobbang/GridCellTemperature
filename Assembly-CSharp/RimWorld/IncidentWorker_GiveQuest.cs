using Verse;

namespace RimWorld;

public class IncidentWorker_GiveQuest : IncidentWorker
{
	protected override bool CanFireNowSub(IncidentParms parms)
	{
		if (!base.CanFireNowSub(parms))
		{
			return false;
		}
		if (def.questScriptDef != null)
		{
			if (!def.questScriptDef.CanRun(parms.points))
			{
				return false;
			}
		}
		else if (parms.questScriptDef != null && !parms.questScriptDef.CanRun(parms.points))
		{
			return false;
		}
		return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoSuspended.Any();
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		QuestScriptDef questScriptDef = def.questScriptDef ?? parms.questScriptDef ?? NaturalRandomQuestChooser.ChooseNaturalRandomQuest(parms.points, parms.target);
		if (questScriptDef == null)
		{
			return false;
		}
		parms.questScriptDef = questScriptDef;
		GiveQuest(parms, questScriptDef);
		return true;
	}

	protected virtual void GiveQuest(IncidentParms parms, QuestScriptDef questDef)
	{
		Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, parms.points);
		if (!quest.hidden && questDef.sendAvailableLetter)
		{
			QuestUtility.SendLetterQuestAvailable(quest);
		}
	}
}
