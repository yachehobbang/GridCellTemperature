using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public class LordJob_TradeWithColony : LordJob
{
	private Faction faction;

	private IntVec3 chillSpot;

	public override bool AddFleeToil => false;

	public LordJob_TradeWithColony()
	{
	}

	public override bool CanOpenAnyDoor(Pawn p)
	{
		if (p.RaceProps.FenceBlocked)
		{
			return true;
		}
		return false;
	}

	public LordJob_TradeWithColony(Faction faction, IntVec3 chillSpot)
	{
		this.faction = faction;
		this.chillSpot = chillSpot;
	}

	public override StateGraph CreateGraph()
	{
		Pawn trader = TraderCaravanUtility.FindTrader(lord);
		StateGraph stateGraph = new StateGraph();
		LordToil_Travel lordToil_Travel = (LordToil_Travel)(stateGraph.StartingToil = new LordToil_Travel(chillSpot));
		LordToil_DefendTraderCaravan lordToil_DefendTraderCaravan = new LordToil_DefendTraderCaravan();
		stateGraph.AddToil(lordToil_DefendTraderCaravan);
		LordToil_DefendTraderCaravan lordToil_DefendTraderCaravan2 = new LordToil_DefendTraderCaravan(chillSpot);
		stateGraph.AddToil(lordToil_DefendTraderCaravan2);
		LordToil_ExitMapAndEscortCarriers lordToil_ExitMapAndEscortCarriers = new LordToil_ExitMapAndEscortCarriers();
		stateGraph.AddToil(lordToil_ExitMapAndEscortCarriers);
		LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap();
		stateGraph.AddToil(lordToil_ExitMap);
		LordToil_ExitMap lordToil_ExitMap2 = new LordToil_ExitMap(LocomotionUrgency.Walk, canDig: true);
		stateGraph.AddToil(lordToil_ExitMap2);
		LordToil_ExitMapTraderFighting lordToil_ExitMapTraderFighting = new LordToil_ExitMapTraderFighting();
		stateGraph.AddToil(lordToil_ExitMapTraderFighting);
		Transition transition = new Transition(lordToil_Travel, lordToil_ExitMapAndEscortCarriers);
		transition.AddSources(lordToil_DefendTraderCaravan, lordToil_DefendTraderCaravan2);
		transition.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
		transition.AddPostAction(new TransitionAction_EndAllJobs());
		transition.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
		stateGraph.AddTransition(transition);
		Transition transition2 = new Transition(lordToil_Travel, lordToil_ExitMapAndEscortCarriers);
		transition2.AddSources(lordToil_DefendTraderCaravan, lordToil_DefendTraderCaravan2);
		transition2.AddPreAction(new TransitionAction_Message("MessageVisitorsAnomalousWeather".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
		transition2.AddPostAction(new TransitionAction_EndAllJobs());
		transition2.AddTrigger(new Trigger_PawnExperiencingAnomalousWeather());
		stateGraph.AddTransition(transition2);
		Transition transition3 = new Transition(lordToil_Travel, lordToil_ExitMap2);
		transition3.AddSources(lordToil_DefendTraderCaravan, lordToil_DefendTraderCaravan2, lordToil_ExitMapAndEscortCarriers, lordToil_ExitMap, lordToil_ExitMapTraderFighting);
		transition3.AddTrigger(new Trigger_PawnCannotReachMapEdge());
		transition3.AddPostAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
		transition3.AddPostAction(new TransitionAction_WakeAll());
		transition3.AddPostAction(new TransitionAction_EndAllJobs());
		stateGraph.AddTransition(transition3);
		Transition transition4 = new Transition(lordToil_ExitMap2, lordToil_ExitMapTraderFighting);
		transition4.AddTrigger(new Trigger_PawnCanReachMapEdge());
		transition4.AddPostAction(new TransitionAction_EndAllJobs());
		stateGraph.AddTransition(transition4);
		Transition transition5 = new Transition(lordToil_Travel, lordToil_ExitMapTraderFighting);
		transition5.AddSources(lordToil_DefendTraderCaravan, lordToil_DefendTraderCaravan2, lordToil_ExitMapAndEscortCarriers, lordToil_ExitMap);
		transition5.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
		transition5.AddPostAction(new TransitionAction_EndAllJobs());
		stateGraph.AddTransition(transition5);
		Transition transition6 = new Transition(lordToil_Travel, lordToil_DefendTraderCaravan);
		transition6.AddTrigger(new Trigger_PawnHarmed(1f, requireInstigatorWithFaction: false, null, null, null));
		transition6.AddPreAction(new TransitionAction_SetDefendTrader());
		transition6.AddPostAction(new TransitionAction_WakeAll());
		transition6.AddPostAction(new TransitionAction_EndAllJobs());
		stateGraph.AddTransition(transition6);
		Transition transition7 = new Transition(lordToil_DefendTraderCaravan, lordToil_Travel);
		transition7.AddTrigger(new Trigger_TicksPassedWithoutHarm(1200));
		stateGraph.AddTransition(transition7);
		Transition transition8 = new Transition(lordToil_Travel, lordToil_DefendTraderCaravan2);
		transition8.AddTrigger(new Trigger_Memo("TravelArrived"));
		stateGraph.AddTransition(transition8);
		Transition transition9 = new Transition(lordToil_DefendTraderCaravan2, lordToil_ExitMapAndEscortCarriers);
		transition9.AddTrigger(new Trigger_TicksPassed((!DebugSettings.instantVisitorsGift) ? Rand.Range(27000, 45000) : 0));
		transition9.AddPreAction(new TransitionAction_CheckGiveGift());
		transition9.AddPreAction(new TransitionAction_Message("MessageTraderCaravanLeaving".Translate(faction.Name)));
		transition9.AddPostAction(new TransitionAction_WakeAll());
		stateGraph.AddTransition(transition9);
		Transition transition10 = new Transition(lordToil_DefendTraderCaravan2, lordToil_ExitMapAndEscortCarriers);
		transition10.AddSource(lordToil_Travel);
		transition10.AddTrigger(new Trigger_Custom((TriggerSignal s) => s.type == TriggerSignalType.Tick && trader.mindState.traderDismissed));
		transition10.AddPreAction(new TransitionAction_Message("MessageTraderCaravanDismissed".Translate(faction.Name)));
		transition10.AddPostAction(new TransitionAction_WakeAll());
		transition10.AddPostAction(new TransitionAction_EndAllJobs());
		stateGraph.AddTransition(transition10);
		Transition transition11 = new Transition(lordToil_ExitMapAndEscortCarriers, lordToil_ExitMapAndEscortCarriers, canMoveToSameState: true);
		transition11.canMoveToSameState = true;
		transition11.AddTrigger(new Trigger_PawnLost());
		transition11.AddTrigger(new Trigger_TickCondition(() => LordToil_ExitMapAndEscortCarriers.IsAnyDefendingPosition(lord.ownedPawns) && !GenHostility.AnyHostileActiveThreatTo(base.Map, faction, countDormantPawnsAsHostile: true), 60));
		stateGraph.AddTransition(transition11);
		Transition transition12 = new Transition(lordToil_ExitMapAndEscortCarriers, lordToil_ExitMap);
		transition12.AddTrigger(new Trigger_TicksPassed(60000));
		transition12.AddPostAction(new TransitionAction_WakeAll());
		stateGraph.AddTransition(transition12);
		Transition transition13 = new Transition(lordToil_DefendTraderCaravan2, lordToil_ExitMapAndEscortCarriers);
		transition13.AddSources(lordToil_Travel, lordToil_DefendTraderCaravan);
		transition13.AddTrigger(new Trigger_ImportantTraderCaravanPeopleLost());
		transition13.AddTrigger(new Trigger_BecamePlayerEnemy());
		transition13.AddPostAction(new TransitionAction_WakeAll());
		transition13.AddPostAction(new TransitionAction_EndAllJobs());
		stateGraph.AddTransition(transition13);
		return stateGraph;
	}

	public override void ExposeData()
	{
		Scribe_References.Look(ref faction, "faction");
		Scribe_Values.Look(ref chillSpot, "chillSpot");
	}
}
