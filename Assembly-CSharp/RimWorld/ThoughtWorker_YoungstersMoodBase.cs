using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorld;

public abstract class ThoughtWorker_YoungstersMoodBase : ThoughtWorker
{
	private static List<Pawn> tmpPawns = new List<Pawn>();

	protected abstract FloatRange MoodRange();

	public override string PostProcessLabel(Pawn p, string label)
	{
		int num = Mathf.RoundToInt(MoodMultiplier(p));
		if (num <= 1)
		{
			return base.PostProcessLabel(p, label);
		}
		return base.PostProcessLabel(p, label) + " x" + num;
	}

	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		if (!ModsConfig.BiotechActive)
		{
			return ThoughtState.Inactive;
		}
		if (!p.IsColonist || p.Suspended || !p.DevelopmentalStage.Adult())
		{
			return ThoughtState.Inactive;
		}
		if (ChildrenWithMoodInColony(p) <= 0)
		{
			return ThoughtState.Inactive;
		}
		return ThoughtState.ActiveDefault;
	}

	private int ChildrenWithMoodInColony(Pawn pawn)
	{
		int num = 0;
		tmpPawns.Clear();
		if (pawn.Spawned)
		{
			tmpPawns.AddRange(pawn.Map.mapPawns.FreeColonists);
		}
		else
		{
			Caravan caravan = pawn.GetCaravan();
			if (caravan != null)
			{
				tmpPawns.AddRange(caravan.PawnsListForReading.Where((Pawn p) => p.IsColonist));
			}
		}
		for (int i = 0; i < tmpPawns.Count; i++)
		{
			if (tmpPawns[i].RaceProps.Humanlike && !tmpPawns[i].DevelopmentalStage.Adult() && !PawnRelationDefOf.Parent.Worker.InRelation(tmpPawns[i], pawn) && ThoughtWorker_RelatedChildMoodBase.IsChildWithMood(tmpPawns[i], MoodRange()))
			{
				num++;
			}
		}
		tmpPawns.Clear();
		return num;
	}

	public override float MoodMultiplier(Pawn p)
	{
		return Mathf.Min(def.stackLimit, ChildrenWithMoodInColony(p));
	}
}
