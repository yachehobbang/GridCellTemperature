using System.Collections.Generic;
using System.Linq;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace RimWorld;

public abstract class QuestPart_SubquestGenerator : QuestPartActivable
{
	public List<QuestScriptDef> subquestDefs = new List<QuestScriptDef>();

	public IntRange interval;

	public int maxActiveSubquests = 2;

	public string expiryInfoPartKey;

	public int maxSuccessfulSubquests = -1;

	private int? currentInterval;

	private bool CanGenerateSubquest
	{
		get
		{
			if (PendingSubquestCount < maxActiveSubquests)
			{
				return SuccessfulSubquestCount + PendingSubquestCount < maxSuccessfulSubquests;
			}
			return false;
		}
	}

	private int SuccessfulSubquestCount => quest.GetSubquests(QuestState.EndedSuccess).Count();

	private int PendingSubquestCount => (from q in quest.GetSubquests(null)
		where q.State == QuestState.Ongoing || q.State == QuestState.NotYetAccepted
		select q).Count();

	public override string ExpiryInfoPart
	{
		get
		{
			if (expiryInfoPartKey.NullOrEmpty())
			{
				return null;
			}
			return $"{expiryInfoPartKey.Translate()} {SuccessfulSubquestCount} / {maxSuccessfulSubquests}";
		}
	}

	public override void QuestPartTick()
	{
		if (subquestDefs.Count == 0)
		{
			return;
		}
		if (maxSuccessfulSubquests > 0 && SuccessfulSubquestCount >= maxSuccessfulSubquests)
		{
			Complete();
			return;
		}
		if (currentInterval.HasValue)
		{
			currentInterval--;
			if (currentInterval < 0)
			{
				GenerateSubquest();
				currentInterval = null;
			}
		}
		if (!currentInterval.HasValue && CanGenerateSubquest)
		{
			currentInterval = interval.RandomInRange;
		}
	}

	protected abstract QuestScriptDef GetNextSubquestDef();

	protected abstract Slate InitSlate();

	private void GenerateSubquest()
	{
		QuestScriptDef nextSubquestDef = GetNextSubquestDef();
		if (nextSubquestDef != null)
		{
			Slate vars = InitSlate();
			Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(nextSubquestDef, vars);
			quest.parent = base.quest;
			if (!quest.hidden && quest.root.sendAvailableLetter)
			{
				QuestUtility.SendLetterQuestAvailable(quest);
			}
		}
	}

	public override void DoDebugWindowContents(Rect innerRect, ref float curY)
	{
		if (base.State != QuestPartState.Enabled)
		{
			return;
		}
		Rect rect = new Rect(innerRect.x, curY, 500f, 25f);
		if (Widgets.ButtonText(rect, "Generate " + ToString(), drawBackground: true, doMouseoverSound: true, active: true, null) && CanGenerateSubquest)
		{
			GenerateSubquest();
		}
		curY += rect.height + 4f;
		Rect rect2 = new Rect(innerRect.x, curY, 500f, 25f);
		if (Widgets.ButtonText(rect2, "Remove Active " + ToString(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			foreach (Quest subquest in quest.GetSubquests(null))
			{
				Find.QuestManager.Remove(subquest);
			}
		}
		curY += rect2.height + 4f;
		Rect rect3 = new Rect(innerRect.x, curY, 500f, 25f);
		if (Widgets.ButtonText(rect3, "Complete " + ToString(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Complete();
		}
		curY += rect3.height + 4f;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref currentInterval, "currentInterval", null);
		Scribe_Collections.Look(ref subquestDefs, "subquestDefs", LookMode.Def);
		Scribe_Values.Look(ref interval, "interval");
		Scribe_Values.Look(ref maxActiveSubquests, "maxActiveSubquests", 0);
		Scribe_Values.Look(ref maxSuccessfulSubquests, "maxSuccessfulSubquests", -1);
		Scribe_Values.Look(ref expiryInfoPartKey, "expiryInfoPartKey");
	}
}
