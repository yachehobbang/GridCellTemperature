using RimWorld.Planet;
using Verse;

namespace RimWorld.QuestGen;

public class QuestNode_Root_Loot_AncientComplex : QuestNode_Root_AncientComplex
{
	private const int MinDistanceFromColony = 2;

	private const int MaxDistanceFromColony = 10;

	protected override LayoutDef LayoutDef => LayoutDefOf.AncientComplex_Loot;

	protected virtual bool BeforeRunInt()
	{
		if (!ModLister.CheckIdeology("Ancient complex rescue"))
		{
			return false;
		}
		return true;
	}

	protected override void RunInt()
	{
		if (BeforeRunInt())
		{
			Slate slate = QuestGen.slate;
			Quest quest = QuestGen.quest;
			Map map = QuestGen_Get.GetMap(mustBeInfestable: false, null);
			float num = slate.Get("points", 0f);
			TryFindSiteTile(out var tile);
			TryFindEnemyFaction(out var _);
			QuestGen.GenerateNewSignal("RaidArrives");
			string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.Destroyed");
			LayoutStructureSketch ancientLayoutStructureSketch = QuestSetupComplex(quest, num);
			Site site = QuestGen_Sites.GenerateSite(Gen.YieldSingle(new SitePartDefWithParams(parms: new SitePartParams
			{
				threatPoints = (Find.Storyteller.difficulty.allowViolentQuests ? QuestNode_Root_AncientComplex.ThreatPointsOverPointsCurve.Evaluate(num) : 0f),
				ancientLayoutStructureSketch = ancientLayoutStructureSketch,
				ancientComplexRewardMaker = ThingSetMakerDefOf.MapGen_AncientComplexRoomLoot_Better
			}, def: SitePartDef)), tile, Faction.OfAncients);
			quest.SpawnWorldObject(site);
			TimedDetectionRaids component = site.GetComponent<TimedDetectionRaids>();
			if (component != null)
			{
				component.alertRaidsArrivingIn = true;
			}
			quest.End(QuestEndOutcome.Unknown, 0, null, inSignal);
			slate.Set("map", map);
			slate.Set("site", site);
		}
	}

	public override LayoutStructureSketch QuestSetupComplex(Quest quest, float points)
	{
		return GenerateStructureSketch(points, generateTerminals: false);
	}

	private bool TryFindSiteTile(out int tile)
	{
		return TileFinder.TryFindNewSiteTile(out tile, 2, 10);
	}

	private bool TryFindEnemyFaction(out Faction enemyFaction)
	{
		enemyFaction = Find.FactionManager.RandomEnemyFaction();
		return enemyFaction != null;
	}

	protected override bool TestRunInt(Slate slate)
	{
		Faction enemyFaction;
		if (TryFindSiteTile(out var _))
		{
			return TryFindEnemyFaction(out enemyFaction);
		}
		return false;
	}
}
