using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace RimWorld;

public static class ResurrectionUtility
{
	private static SimpleCurve DementiaChancePerRotDaysCurve = new SimpleCurve
	{
		new CurvePoint(0.1f, 0.02f),
		new CurvePoint(5f, 0.8f)
	};

	private static SimpleCurve BlindnessChancePerRotDaysCurve = new SimpleCurve
	{
		new CurvePoint(0.1f, 0.02f),
		new CurvePoint(5f, 0.8f)
	};

	private static SimpleCurve ResurrectionPsychosisChancePerRotDaysCurve = new SimpleCurve
	{
		new CurvePoint(0.1f, 0.02f),
		new CurvePoint(5f, 0.8f)
	};

	public static bool TryResurrect(Pawn pawn, ResurrectionParams parms = null)
	{
		if (!pawn.Dead)
		{
			Log.Error("Tried to resurrect a pawn who is not dead: " + pawn.ToStringSafe());
			return false;
		}
		if (pawn.Discarded)
		{
			Log.Error("Tried to resurrect a discarded pawn: " + pawn.ToStringSafe());
			return false;
		}
		Corpse corpse = pawn.Corpse;
		bool flag = false;
		IntVec3 loc = IntVec3.Invalid;
		Map map = null;
		if (ModsConfig.AnomalyActive && corpse is UnnaturalCorpse)
		{
			Messages.Message("MessageUnnaturalCorpseResurrect".Translate(corpse.InnerPawn.Named("PAWN")), corpse, MessageTypeDefOf.NeutralEvent);
			return false;
		}
		bool flag2 = Find.Selector.IsSelected(corpse);
		if (corpse != null)
		{
			flag = corpse.SpawnedOrAnyParentSpawned;
			loc = corpse.PositionHeld;
			map = corpse.MapHeld;
			corpse.InnerPawn = null;
			corpse.Destroy();
		}
		if (flag && pawn.IsWorldPawn())
		{
			Find.WorldPawns.RemovePawn(pawn);
		}
		pawn.ForceSetStateToUnspawned();
		PawnComponentsUtility.CreateInitialComponents(pawn);
		pawn.health.Notify_Resurrected(parms?.restoreMissingParts ?? true, parms?.gettingScarsChance ?? 0f);
		if (pawn.Faction != null && pawn.Faction.IsPlayer)
		{
			pawn.workSettings?.EnableAndInitialize();
			Find.StoryWatcher.watcherPopAdaptation.Notify_PawnEvent(pawn, PopAdaptationEvent.GainedColonist);
		}
		if (pawn.RaceProps.IsMechanoid && MechRepairUtility.IsMissingWeapon(pawn))
		{
			MechRepairUtility.GenerateWeapon(pawn);
		}
		if (flag && (parms == null || !parms.dontSpawn))
		{
			GenSpawn.Spawn(pawn, loc, map);
			Lord lord;
			if ((lord = pawn.GetLord()) != null)
			{
				lord?.Notify_PawnUndowned(pawn);
			}
			else if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.HostileTo(Faction.OfPlayer) && (parms == null || !parms.noLord))
			{
				LordMaker.MakeNewLord(lordJob: (parms == null) ? new LordJob_AssaultColony(pawn.Faction) : new LordJob_AssaultColony(pawn.Faction, parms.canKidnap, parms.canTimeoutOrFlee, parms.sappers, parms.useAvoidGridSmart, parms.canSteal, parms.breachers, parms.canPickUpOpportunisticWeapons), faction: pawn.Faction, map: pawn.Map, startingPawns: Gen.YieldSingle(pawn));
			}
			if (pawn.apparel != null)
			{
				List<Apparel> wornApparel = pawn.apparel.WornApparel;
				for (int i = 0; i < wornApparel.Count; i++)
				{
					wornApparel[i].Notify_PawnResurrected(pawn);
				}
			}
		}
		if (parms != null && parms.removeDiedThoughts)
		{
			PawnDiedOrDownedThoughtsUtility.RemoveDiedThoughts(pawn);
		}
		pawn.royalty?.Notify_Resurrected();
		if (pawn.guest != null && pawn.guest.IsInteractionEnabled(PrisonerInteractionModeDefOf.Execution))
		{
			pawn.guest.SetNoInteraction();
		}
		if (flag2 && pawn != null)
		{
			Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
		}
		pawn.Drawer.renderer.SetAllGraphicsDirty();
		if (parms != null && parms.invisibleStun)
		{
			pawn.stances.stunner.StunFor(5f.SecondsToTicks(), pawn, addBattleLog: false, showMote: false);
		}
		pawn.needs.AddOrRemoveNeedsAsAppropriate();
		return true;
	}

	public static bool TryResurrectWithSideEffects(Pawn pawn)
	{
		Corpse corpse = pawn.Corpse;
		float x2 = ((corpse == null) ? 0f : (corpse.GetComp<CompRottable>().RotProgress / 60000f));
		if (!TryResurrect(pawn))
		{
			return false;
		}
		BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
		Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.ResurrectionSickness, pawn);
		if (!pawn.health.WouldDieAfterAddingHediff(hediff))
		{
			pawn.health.AddHediff(hediff, null, null);
		}
		if (Rand.Chance(DementiaChancePerRotDaysCurve.Evaluate(x2)) && brain != null)
		{
			Hediff hediff2 = HediffMaker.MakeHediff(HediffDefOf.Dementia, pawn, brain);
			if (!pawn.health.WouldDieAfterAddingHediff(hediff2))
			{
				pawn.health.AddHediff(hediff2, null, null);
			}
		}
		if (Rand.Chance(BlindnessChancePerRotDaysCurve.Evaluate(x2)))
		{
			foreach (BodyPartRecord item in from x in pawn.health.hediffSet.GetNotMissingParts()
				where x.def == BodyPartDefOf.Eye
				select x)
			{
				if (!pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(item))
				{
					Hediff hediff3 = HediffMaker.MakeHediff(HediffDefOf.Blindness, pawn, item);
					pawn.health.AddHediff(hediff3, null, null);
				}
			}
		}
		if (brain != null && Rand.Chance(ResurrectionPsychosisChancePerRotDaysCurve.Evaluate(x2)))
		{
			Hediff hediff4 = HediffMaker.MakeHediff(HediffDefOf.ResurrectionPsychosis, pawn, brain);
			if (!pawn.health.WouldDieAfterAddingHediff(hediff4))
			{
				pawn.health.AddHediff(hediff4, null, null);
			}
		}
		if (pawn.Dead)
		{
			Log.Error("The pawn has died while being resurrected.");
			TryResurrect(pawn);
		}
		return true;
	}
}
