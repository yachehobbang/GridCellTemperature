using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace Verse.AI.Group;

public class PsychicRitualToil_SkipAbductionPlayer : PsychicRitualToil
{
	private const float ChanceForLeader = 0.08f;

	private const float ChanceForWorldPawn = 0.4f;

	public PsychicRitualRoleDef invokerRole;

	protected PsychicRitualToil_SkipAbductionPlayer()
	{
	}

	public PsychicRitualToil_SkipAbductionPlayer(PsychicRitualRoleDef invokerRole)
	{
		this.invokerRole = invokerRole;
	}

	public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
	{
		Pawn pawn = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
		if (pawn != null)
		{
			ApplyOutcome(psychicRitual, pawn);
		}
	}

	private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker)
	{
		IntVec3 cell = psychicRitual.assignments.Target.Cell;
		bool flag = false;
		Pawn[] source = psychicRitual.Map.attackTargetsCache.TargetsHostileToColony.Where((IAttackTarget t) => t.Thing is Pawn pawn && pawn.RaceProps.Humanlike && !pawn.IsMutant && pawn.Faction != Faction.OfPlayer && !t.ThreatDisabled(invoker) && !pawn.IsOnHoldingPlatform).Cast<Pawn>().ToArray();
		Pawn pawn2 = null;
		if (source.TryRandomElement(out var result))
		{
			pawn2 = result;
			psychicRitual.Map.effecterMaintainer.AddEffecterToMaintain(EffecterDefOf.Skip_EntryNoDelay.Spawn(pawn2, pawn2.Map), pawn2.PositionHeld, 60);
			SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn2.PositionHeld, pawn2.Map));
			SkipUtility.SkipTo(pawn2, cell, psychicRitual.Map);
		}
		else
		{
			List<Pawn> list = Find.WorldPawns.AllPawnsAlive.Where((Pawn p) => p.RaceProps.Humanlike && p.HostileTo(invoker) && !p.IsMutant && p.Faction?.leader == p).ToList();
			List<Pawn> list2 = Find.WorldPawns.AllPawnsAlive.Where((Pawn p) => p.RaceProps.Humanlike && p.HostileTo(invoker) && !p.IsMutant && p.Faction?.leader != p).ToList();
			float chance = 0.4f * Mathf.Clamp01((float)list2.Count / 20f);
			Pawn result2 = null;
			if (Rand.Chance(0.08f) && !list.NullOrEmpty())
			{
				list.TryRandomElement(out result2);
			}
			else if (Rand.Chance(chance))
			{
				list2.TryRandomElement(out result2);
			}
			Faction result3;
			if (result2 != null)
			{
				pawn2 = (Pawn)GenSpawn.Spawn(result2, cell, psychicRitual.Map);
				flag = true;
			}
			else if (Find.FactionManager.AllFactionsVisible.Where((Faction f) => f.def.humanlikeFaction && f.HostileTo(Faction.OfPlayer)).TryRandomElement(out result3))
			{
				pawn2 = (Pawn)GenSpawn.Spawn(PawnGenerator.GeneratePawn(new PawnGenerationRequest(result3.RandomPawnKind(), result3, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null)), cell, psychicRitual.Map);
			}
		}
		if (pawn2 == null)
		{
			Log.Error("Could not find target pawn for player's skip abduction ritual.");
			return;
		}
		if (pawn2.Dead)
		{
			Log.Error($"Skip abduction ritual abducted a dead pawn. World pawn abducted: {flag}");
		}
		if (pawn2.IsMutant)
		{
			Log.Error($"Skip abduction ritual abducted a mutant. World pawn abducted: {flag}");
		}
		psychicRitual.Map.effecterMaintainer.AddEffecterToMaintain(EffecterDefOf.Skip_ExitNoDelay.Spawn(cell, psychicRitual.Map), cell, 60);
		SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(cell, psychicRitual.Map));
		int ticksToDisappear = Mathf.RoundToInt(((PsychicRitualDef_SkipAbductionPlayer)psychicRitual.def).comaDurationDaysFromQualityCurve.Evaluate(psychicRitual.PowerPercent) * 60000f);
		Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.DarkPsychicShock, pawn2);
		hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = ticksToDisappear;
		pawn2.health.AddHediff(hediff, null, null);
		pawn2.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.PsychicRitualVictim);
		TaggedString text = "SkipAbductionPlayerCompleteText".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), pawn2.Named("TARGET"), pawn2.Faction.Named("FACTION"));
		Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), text, LetterDefOf.NeutralEvent, new LookTargets(pawn2));
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref invokerRole, "invokerRole");
	}
}
