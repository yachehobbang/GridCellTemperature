using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace RimWorld.QuestGen;

public static class QuestGen_Threat
{
	public static void Raid(this Quest quest, Map map, float points, Faction faction, string inSignalLeave = null, string customLetterLabel = null, string customLetterText = null, RulePack customLetterLabelRules = null, RulePack customLetterTextRules = null, IntVec3? walkInSpot = null, string tag = null, string inSignal = null, string rootSymbol = "root", PawnsArrivalModeDef raidArrivalMode = null, RaidStrategyDef raidStrategy = null, PawnGroupKindDef pawnGroupKind = null, bool silent = false, bool canTimeoutOrFlee = true, bool canSteal = true, bool canKidnap = true)
	{
		if (pawnGroupKind == null)
		{
			pawnGroupKind = PawnGroupKindDefOf.Combat;
		}
		QuestPart_Incident questPart_Incident = new QuestPart_Incident();
		questPart_Incident.debugLabel = "raid";
		questPart_Incident.incident = IncidentDefOf.RaidEnemy;
		IncidentParms incidentParms = new IncidentParms();
		incidentParms.forced = true;
		incidentParms.target = map;
		incidentParms.points = Mathf.Max(points, faction.def.MinPointsToGeneratePawnGroup(pawnGroupKind));
		incidentParms.faction = faction;
		incidentParms.pawnGroupKind = pawnGroupKind;
		incidentParms.pawnGroupMakerSeed = Rand.Int;
		incidentParms.inSignalEnd = inSignalLeave;
		incidentParms.questTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(tag);
		incidentParms.raidArrivalMode = raidArrivalMode;
		incidentParms.raidStrategy = raidStrategy;
		incidentParms.canTimeoutOrFlee = canTimeoutOrFlee;
		incidentParms.canKidnap = canKidnap;
		incidentParms.canSteal = canSteal;
		incidentParms.sendLetter = !silent;
		incidentParms.silent = silent;
		if (!customLetterLabel.NullOrEmpty() || customLetterLabelRules != null)
		{
			QuestGen.AddTextRequest(rootSymbol, delegate(string x)
			{
				incidentParms.customLetterLabel = x;
			}, QuestGenUtility.MergeRules(customLetterLabelRules, customLetterLabel, rootSymbol));
		}
		if (!customLetterText.NullOrEmpty() || customLetterTextRules != null)
		{
			QuestGen.AddTextRequest(rootSymbol, delegate(string x)
			{
				incidentParms.customLetterText = x;
			}, QuestGenUtility.MergeRules(customLetterTextRules, customLetterText, rootSymbol));
		}
		IncidentWorker_Raid obj = (IncidentWorker_Raid)questPart_Incident.incident.Worker;
		obj.ResolveRaidStrategy(incidentParms, pawnGroupKind);
		obj.ResolveRaidArriveMode(incidentParms);
		obj.ResolveRaidAgeRestriction(incidentParms);
		if (incidentParms.raidArrivalMode.walkIn)
		{
			incidentParms.spawnCenter = walkInSpot ?? QuestGen.slate.Get<IntVec3?>("walkInSpot", null) ?? IntVec3.Invalid;
		}
		PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(pawnGroupKind, incidentParms);
		defaultPawnGroupMakerParms.points = IncidentWorker_Raid.AdjustedRaidPoints(defaultPawnGroupMakerParms.points, incidentParms.raidArrivalMode, incidentParms.raidStrategy, defaultPawnGroupMakerParms.faction, pawnGroupKind);
		IEnumerable<PawnKindDef> pawnKinds = PawnGroupMakerUtility.GeneratePawnKindsExample(defaultPawnGroupMakerParms);
		questPart_Incident.SetIncidentParmsAndRemoveTarget(incidentParms);
		questPart_Incident.inSignal = inSignal ?? QuestGen.slate.Get<string>("inSignal");
		QuestGen.quest.AddPart(questPart_Incident);
		QuestGen.AddQuestDescriptionRules(new List<Rule>
		{
			new Rule_String("raidPawnKinds", PawnUtility.PawnKindsToLineList(pawnKinds, "  - ", ColoredText.ThreatColor)),
			new Rule_String("raidArrivalModeInfo", incidentParms.raidArrivalMode.textWillArrive.Formatted(faction))
		});
	}

	public static void RandomRaid(this Quest quest, MapParent mapParent, FloatRange pointsRange, Faction faction = null, string inSignal = null, PawnsArrivalModeDef arrivalMode = null, RaidStrategyDef raidStrategy = null, string customLetterLabel = null, string customLetterText = null)
	{
		QuestPart_RandomRaid questPart_RandomRaid = new QuestPart_RandomRaid();
		questPart_RandomRaid.mapParent = mapParent;
		questPart_RandomRaid.faction = faction;
		questPart_RandomRaid.inSignal = inSignal ?? QuestGen.slate.Get<string>("inSignal");
		questPart_RandomRaid.pointsRange = pointsRange;
		questPart_RandomRaid.arrivalMode = arrivalMode;
		questPart_RandomRaid.raidStrategy = raidStrategy;
		questPart_RandomRaid.customLetterLabel = customLetterLabel;
		questPart_RandomRaid.customLetterText = customLetterText;
		quest.AddPart(questPart_RandomRaid);
	}
}
