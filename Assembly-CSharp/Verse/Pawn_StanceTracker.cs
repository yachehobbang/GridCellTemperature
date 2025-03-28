using System;
using RimWorld;

namespace Verse;

public class Pawn_StanceTracker : IExposable
{
	public Pawn pawn;

	public Stance curStance = new Stance_Mobile();

	public StunHandler stunner;

	public StaggerHandler stagger;

	public const int StaggerMeleeAttackTicks = 95;

	public const int StaggerBulletImpactTicks = 95;

	public const int StaggerExplosionImpactTicks = 95;

	public bool debugLog;

	public bool FullBodyBusy
	{
		get
		{
			if (!stunner.Stunned)
			{
				return curStance.StanceBusy;
			}
			return true;
		}
	}

	public Pawn_StanceTracker(Pawn newPawn)
	{
		pawn = newPawn;
		stunner = new StunHandler(pawn);
		stagger = new StaggerHandler(pawn);
	}

	public void StanceTrackerTick()
	{
		stunner.StunHandlerTick();
		stagger.StaggerHandlerTick();
		curStance.StanceTick();
	}

	public void StanceTrackerDraw()
	{
		curStance.StanceDraw();
	}

	public void ExposeData()
	{
		Scribe_Deep.Look(ref stunner, "stunner", pawn);
		Scribe_Deep.Look(ref stagger, "stagger", pawn);
		Scribe_Deep.Look(ref curStance, "curStance");
		if (Scribe.mode == LoadSaveMode.LoadingVars && curStance != null)
		{
			curStance.stanceTracker = this;
		}
		if (Scribe.mode == LoadSaveMode.PostLoadInit && stagger == null)
		{
			stagger = new StaggerHandler(pawn);
			int value = 0;
			Scribe_Values.Look(ref value, "staggerUntilTick", 0);
			if (value > Find.TickManager.TicksGame)
			{
				stagger.StaggerFor(value - Find.TickManager.TicksGame);
			}
		}
	}

	[Obsolete("Use pawn.stances.stagger.StaggerFor instead")]
	public void StaggerFor(int ticks)
	{
		stagger.StaggerFor(ticks);
	}

	public void CancelBusyStanceSoft()
	{
		if (curStance is Stance_Warmup)
		{
			SetStance(new Stance_Mobile());
		}
	}

	public void CancelBusyStanceHard()
	{
		SetStance(new Stance_Mobile());
	}

	public void SetStance(Stance newStance)
	{
		if (debugLog)
		{
			Log.Message(string.Concat(Find.TickManager.TicksGame, " ", pawn, " SetStance ", curStance, " -> ", newStance));
		}
		newStance.stanceTracker = this;
		curStance = newStance;
		if (pawn.jobs.curDriver != null)
		{
			pawn.jobs.curDriver.Notify_StanceChanged();
		}
	}

	public void Notify_DamageTaken(DamageInfo dinfo)
	{
	}
}
