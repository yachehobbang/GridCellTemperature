using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using UnityEngine;
using Verse;

namespace RimWorld;

public class Apparel : ThingWithComps
{
	private bool wornByCorpseInt;

	public Pawn Wearer
	{
		get
		{
			if (!(base.ParentHolder is Pawn_ApparelTracker pawn_ApparelTracker))
			{
				return null;
			}
			return pawn_ApparelTracker.pawn;
		}
	}

	public bool WornByCorpse
	{
		get
		{
			return wornByCorpseInt;
		}
		set
		{
			wornByCorpseInt = value;
		}
	}

	public string WornGraphicPath
	{
		get
		{
			if (StyleDef != null && !StyleDef.wornGraphicPath.NullOrEmpty())
			{
				return StyleDef.wornGraphicPath;
			}
			if (!def.apparel.wornGraphicPaths.NullOrEmpty())
			{
				return def.apparel.wornGraphicPaths[thingIDNumber % def.apparel.wornGraphicPaths.Count];
			}
			return def.apparel.wornGraphicPath;
		}
	}

	public override string DescriptionDetailed
	{
		get
		{
			string text = base.DescriptionDetailed;
			if (WornByCorpse)
			{
				text += "\n" + "WasWornByCorpse".Translate();
			}
			return text;
		}
	}

	public override Color DrawColor
	{
		get
		{
			Color color = ((StyleDef == null || !(StyleDef.color != default(Color))) ? base.DrawColor : StyleDef.color);
			if (WornByCorpse)
			{
				color = PawnRenderUtility.GetRottenColor(color);
			}
			return color;
		}
	}

	public Color? DesiredColor
	{
		get
		{
			return GetComp<CompColorable>()?.DesiredColor;
		}
		set
		{
			CompColorable comp = GetComp<CompColorable>();
			if (comp != null)
			{
				comp.DesiredColor = value;
			}
			else
			{
				Log.Error("Tried setting Apparel.DesiredColor without having CompColorable comp!");
			}
		}
	}

	public override string GetInspectStringLowPriority()
	{
		string text = base.GetInspectStringLowPriority();
		if (StyleDef != null)
		{
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			text += "VariantOf".Translate().CapitalizeFirst() + ": " + def.LabelCap;
		}
		if (ModsConfig.BiotechActive)
		{
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			text += "WearableBy".Translate() + ": " + def.apparel.developmentalStageFilter.ToCommaList().CapitalizeFirst();
		}
		return text;
	}

	public bool PawnCanWear(Pawn pawn, bool ignoreGender = false)
	{
		if (!def.IsApparel)
		{
			return false;
		}
		if (!def.apparel.PawnCanWear(pawn, ignoreGender))
		{
			return false;
		}
		return true;
	}

	public void Notify_PawnKilled()
	{
		if (def.apparel.careIfWornByCorpse)
		{
			wornByCorpseInt = true;
		}
		foreach (ThingComp allComp in base.AllComps)
		{
			allComp.Notify_WearerDied();
		}
	}

	public void Notify_PawnResurrected(Pawn pawn)
	{
		if (!pawn.IsMutant)
		{
			wornByCorpseInt = false;
		}
	}

	public override void Notify_ColorChanged()
	{
		if (Wearer != null)
		{
			Wearer.apparel.Notify_ApparelChanged();
		}
		base.Notify_ColorChanged();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref wornByCorpseInt, "wornByCorpse", defaultValue: false);
	}

	public virtual void DrawWornExtras()
	{
		List<ThingComp> allComps = base.AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			allComps[i].CompDrawWornExtras();
		}
	}

	public virtual bool CheckPreAbsorbDamage(DamageInfo dinfo)
	{
		List<ThingComp> allComps = base.AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			allComps[i].PostPreApplyDamage(ref dinfo, out var absorbed);
			if (absorbed)
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool AllowVerbCast(Verb verb)
	{
		List<ThingComp> allComps = base.AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			if (!allComps[i].CompAllowVerbCast(verb))
			{
				return false;
			}
		}
		return true;
	}

	public virtual IEnumerable<Gizmo> GetWornGizmos()
	{
		List<ThingComp> comps = base.AllComps;
		for (int i = 0; i < comps.Count; i++)
		{
			ThingComp thingComp = comps[i];
			foreach (Gizmo item in thingComp.CompGetWornGizmosExtra())
			{
				yield return item;
			}
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		foreach (StatDrawEntry item in base.SpecialDisplayStats())
		{
			yield return item;
		}
		RoyalTitleDef royalTitleDef = (from t in DefDatabase<FactionDef>.AllDefsListForReading.SelectMany((FactionDef f) => f.RoyalTitlesAwardableInSeniorityOrderForReading)
			where t.requiredApparel != null && t.requiredApparel.Any((ApparelRequirement req) => req.ApparelMeetsRequirement(def, allowUnmatched: false))
			orderby t.seniority descending
			select t).FirstOrDefault();
		if (royalTitleDef != null)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "Stat_Thing_Apparel_MaxSatisfiedTitle".Translate(), royalTitleDef.GetLabelCapForBothGenders(), "Stat_Thing_Apparel_MaxSatisfiedTitle_Desc".Translate(), 2752, null, new Dialog_InfoCard.Hyperlink[1]
			{
				new Dialog_InfoCard.Hyperlink(royalTitleDef)
			});
		}
	}

	public override string GetInspectString()
	{
		string text = base.GetInspectString();
		if (WornByCorpse)
		{
			if (text.Length > 0)
			{
				text += "\n";
			}
			text += "WasWornByCorpse".Translate();
		}
		return text;
	}

	public virtual float GetSpecialApparelScoreOffset()
	{
		float num = 0f;
		List<ThingComp> allComps = base.AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			num += allComps[i].CompGetSpecialApparelScoreOffset();
		}
		return num;
	}

	[DebugOutput]
	private static void ApparelValidLifeStages()
	{
		List<TableDataGetter<ThingDef>> list = new List<TableDataGetter<ThingDef>>();
		list.Add(new TableDataGetter<ThingDef>("name", (ThingDef t) => t.LabelCap));
		list.Add(new TableDataGetter<ThingDef>("valid life stage", (ThingDef t) => t.apparel.developmentalStageFilter.ToCommaList()));
		DebugTables.MakeTablesDialog(DefDatabase<ThingDef>.AllDefs.Where((ThingDef t) => t.IsApparel), list.ToArray());
	}
}
