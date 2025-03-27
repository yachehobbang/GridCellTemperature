using System.Collections.Generic;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld;

public class CompHackable : ThingComp, IThingGlower
{
	public string hackingStartedSignal;

	public string hackingCompletedSignal;

	private float progress;

	public float defence;

	private float lastUserSpeed = 1f;

	private int lastHackTick = -1;

	private Pawn lastUser;

	public const string HackedSignal = "Hackend";

	private static List<Pawn> tmpAllowedPawns = new List<Pawn>();

	public CompProperties_Hackable Props => (CompProperties_Hackable)props;

	public float ProgressPercent => progress / defence;

	public bool IsHacked => progress >= defence;

	public bool ShouldBeLitNow()
	{
		if (IsHacked)
		{
			return Props.glowIfHacked;
		}
		return true;
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (ModLister.CheckIdeology("CompHackable"))
		{
			if (!respawningAfterLoad)
			{
				defence = Props.defence;
			}
			base.PostSpawnSetup(respawningAfterLoad);
		}
	}

	public void Hack(float amount, Pawn hacker = null)
	{
		bool isHacked = IsHacked;
		progress += amount;
		progress = Mathf.Clamp(progress, 0f, defence);
		if (!isHacked && IsHacked)
		{
			if (!hackingCompletedSignal.NullOrEmpty())
			{
				Find.SignalManager.SendSignal(new Signal(hackingCompletedSignal, parent.Named("SUBJECT")));
			}
			QuestUtility.SendQuestTargetSignals(parent.questTags, "Hacked", parent.Named("SUBJECT"));
			parent.BroadcastCompSignal("Hackend");
			if (Props.completedQuest != null)
			{
				Slate slate = new Slate();
				slate.Set("map", parent.Map);
				if (Props.completedQuest.CanRun(slate))
				{
					QuestUtility.GenerateQuestAndMakeAvailable(Props.completedQuest, slate);
				}
			}
		}
		if (lastHackTick < 0)
		{
			if (!hackingStartedSignal.NullOrEmpty())
			{
				Find.SignalManager.SendSignal(new Signal(hackingStartedSignal, parent.Named("SUBJECT")));
			}
			QuestUtility.SendQuestTargetSignals(parent.questTags, "HackingStarted", parent.Named("SUBJECT"));
		}
		lastUserSpeed = amount;
		lastHackTick = Find.TickManager.TicksGame;
		lastUser = hacker;
	}

	public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(List<Pawn> selPawns)
	{
		if (IsHacked)
		{
			yield return new FloatMenuOption("CannotHack".Translate(parent.Label) + ": " + "AlreadyHacked".Translate().CapitalizeFirst(), null);
			yield break;
		}
		tmpAllowedPawns.Clear();
		for (int i = 0; i < selPawns.Count; i++)
		{
			if (selPawns[i].CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
			{
				tmpAllowedPawns.Add(selPawns[i]);
			}
		}
		if (tmpAllowedPawns.Count <= 0)
		{
			yield return new FloatMenuOption("CannotHack".Translate(parent.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
			yield break;
		}
		tmpAllowedPawns.Clear();
		for (int j = 0; j < selPawns.Count; j++)
		{
			if (HackUtility.IsCapableOfHacking(selPawns[j]))
			{
				tmpAllowedPawns.Add(selPawns[j]);
			}
		}
		if (tmpAllowedPawns.Count <= 0)
		{
			yield return new FloatMenuOption("CannotHack".Translate(parent.Label) + ": " + "IncapableOfHacking".Translate(), null);
			yield break;
		}
		tmpAllowedPawns.Clear();
		for (int k = 0; k < selPawns.Count; k++)
		{
			if (HackUtility.IsCapableOfHacking(selPawns[k]) && selPawns[k].CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
			{
				tmpAllowedPawns.Add(selPawns[k]);
			}
		}
		if (tmpAllowedPawns.Count <= 0)
		{
			yield break;
		}
		yield return new FloatMenuOption("Hack".Translate(parent.Label), delegate
		{
			tmpAllowedPawns[0].jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Hack, parent), JobTag.Misc);
			for (int l = 1; l < tmpAllowedPawns.Count; l++)
			{
				FloatMenuMakerMap.PawnGotoAction(parent.Position, tmpAllowedPawns[l], RCellFinder.BestOrderedGotoDestNear(parent.Position, tmpAllowedPawns[l]));
			}
		});
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref progress, "progress", 0f);
		Scribe_Values.Look(ref lastUserSpeed, "lastUserSpeed", 0f);
		Scribe_Values.Look(ref lastHackTick, "lastHackTick", 0);
		Scribe_Values.Look(ref defence, "defence", 0f);
		Scribe_References.Look(ref lastUser, "lasterUser");
		Scribe_Values.Look(ref hackingStartedSignal, "hackingStartedSignal");
		Scribe_Values.Look(ref hackingCompletedSignal, "hackingCompletedSignal");
	}

	public override string CompInspectStringExtra()
	{
		TaggedString taggedString = "HackProgress".Translate() + ": " + progress.ToStringWorkAmount() + " / " + defence.ToStringWorkAmount();
		if (IsHacked)
		{
			taggedString += " (" + "Hacked".Translate() + ")";
		}
		if (lastHackTick > Find.TickManager.TicksGame - 30)
		{
			string text = ((lastUser == null) ? ((string)"HackingSpeed".Translate()) : ((string)("HackingLastUser".Translate(lastUser) + " " + "HackingSpeed".Translate())));
			taggedString += "\n" + text + ": " + StatDefOf.HackingSpeed.ValueToString(lastUserSpeed);
		}
		return taggedString;
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		yield return new StatDrawEntry(StatCategoryDefOf.Basics, "HackProgress".Translate(), progress.ToStringWorkAmount() + " / " + defence.ToStringWorkAmount(), "Stat_Thing_HackingProgress".Translate(), 3100);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (DebugSettings.ShowDevGizmos && !IsHacked)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Hack +10%",
				action = delegate
				{
					Hack(defence * 0.1f);
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "DEV: Complete hack",
				action = delegate
				{
					Hack(defence);
				}
			};
		}
	}
}
