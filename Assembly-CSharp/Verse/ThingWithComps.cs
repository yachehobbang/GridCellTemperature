using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;

namespace Verse;

public class ThingWithComps : Thing
{
	private List<ThingComp> comps;

	private static readonly List<ThingComp> EmptyCompsList = new List<ThingComp>();

	public List<ThingComp> AllComps
	{
		get
		{
			if (comps == null)
			{
				return EmptyCompsList;
			}
			return comps;
		}
	}

	public override Color DrawColor
	{
		get
		{
			CompColorable comp = GetComp<CompColorable>();
			if (comp != null && comp.Active)
			{
				return comp.Color;
			}
			return base.DrawColor;
		}
		set
		{
			this.SetColor(value);
		}
	}

	public override string LabelNoCount
	{
		get
		{
			string text = base.LabelNoCount;
			if (comps != null)
			{
				int i = 0;
				for (int count = comps.Count; i < count; i++)
				{
					text = comps[i].TransformLabel(text);
				}
			}
			return text;
		}
	}

	public override string DescriptionFlavor
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.DescriptionFlavor);
			if (comps != null)
			{
				for (int i = 0; i < comps.Count; i++)
				{
					string descriptionPart = comps[i].GetDescriptionPart();
					if (!descriptionPart.NullOrEmpty())
					{
						if (stringBuilder.Length > 0)
						{
							stringBuilder.AppendLine();
							stringBuilder.AppendLine();
						}
						stringBuilder.Append(descriptionPart);
					}
				}
			}
			return stringBuilder.ToString();
		}
	}

	public override void PostMake()
	{
		base.PostMake();
		InitializeComps();
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostPostMake();
			}
		}
	}

	public T GetComp<T>() where T : ThingComp
	{
		if (comps != null)
		{
			int i = 0;
			for (int count = comps.Count; i < count; i++)
			{
				if (comps[i] is T result)
				{
					return result;
				}
			}
		}
		return null;
	}

	public IEnumerable<T> GetComps<T>() where T : class
	{
		if (comps == null)
		{
			yield break;
		}
		for (int i = 0; i < comps.Count; i++)
		{
			if (comps[i] is T val)
			{
				yield return val;
			}
		}
	}

	public ThingComp GetCompByDefType(CompProperties def)
	{
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				if (comps[i].props.compClass == def.compClass)
				{
					return comps[i];
				}
			}
		}
		return null;
	}

	public void InitializeComps()
	{
		if (!def.comps.Any())
		{
			return;
		}
		comps = new List<ThingComp>();
		for (int i = 0; i < def.comps.Count; i++)
		{
			ThingComp thingComp = null;
			try
			{
				thingComp = (ThingComp)Activator.CreateInstance(def.comps[i].compClass);
				thingComp.parent = this;
				comps.Add(thingComp);
				thingComp.Initialize(def.comps[i]);
			}
			catch (Exception ex)
			{
				Log.Error("Could not instantiate or initialize a ThingComp: " + ex);
				comps.Remove(thingComp);
			}
		}
	}

	public override string GetCustomLabelNoCount(bool includeHp = true)
	{
		string text = base.GetCustomLabelNoCount(includeHp);
		if (comps != null)
		{
			int i = 0;
			for (int count = comps.Count; i < count; i++)
			{
				text = comps[i].TransformLabel(text);
			}
		}
		return text;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			InitializeComps();
		}
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostExposeData();
			}
		}
	}

	public void BroadcastCompSignal(string signal)
	{
		ReceiveCompSignal(signal);
		if (comps != null)
		{
			int i = 0;
			for (int count = comps.Count; i < count; i++)
			{
				comps[i].ReceiveCompSignal(signal);
			}
		}
	}

	protected virtual void ReceiveCompSignal(string signal)
	{
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostSpawnSetup(respawningAfterLoad);
			}
		}
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		Map map = base.Map;
		base.DeSpawn(mode);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostDeSpawn(map);
			}
		}
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		Map mapHeld = base.MapHeld;
		base.Destroy(mode);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostDestroy(mode, mapHeld);
			}
		}
	}

	public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
	{
		Map prevMap = ((!(this is Pawn pawn)) ? base.MapHeld : (pawn.prevMap ?? base.MapHeld));
		base.Kill(dinfo, exactCulprit);
		if (comps == null)
		{
			return;
		}
		foreach (ThingComp comp in comps)
		{
			comp.Notify_Killed(prevMap, dinfo);
		}
	}

	public override void Tick()
	{
		if (comps != null)
		{
			int i = 0;
			for (int count = comps.Count; i < count; i++)
			{
				comps[i].CompTick();
			}
		}
	}

	public override void TickRare()
	{
		if (comps != null)
		{
			int i = 0;
			for (int count = comps.Count; i < count; i++)
			{
				comps[i].CompTickRare();
			}
		}
	}

	public override void TickLong()
	{
		if (comps != null)
		{
			int i = 0;
			for (int count = comps.Count; i < count; i++)
			{
				comps[i].CompTickLong();
			}
		}
	}

	public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		base.PreApplyDamage(ref dinfo, out absorbed);
		if (absorbed || comps == null)
		{
			return;
		}
		for (int i = 0; i < comps.Count; i++)
		{
			comps[i].PostPreApplyDamage(ref dinfo, out absorbed);
			if (absorbed)
			{
				break;
			}
		}
	}

	public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		base.PostApplyDamage(dinfo, totalDamageDealt);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostPostApplyDamage(dinfo, totalDamageDealt);
			}
		}
	}

	public virtual IEnumerable<ThingDefCountClass> GetAdditionalLeavings(DestroyMode mode)
	{
		for (int i = 0; i < AllComps.Count; i++)
		{
			foreach (ThingDefCountClass additionalLeaving in AllComps[i].GetAdditionalLeavings(base.Map, mode))
			{
				yield return additionalLeaving;
			}
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Comps_PostDraw();
	}

	protected void Comps_PostDraw()
	{
		if (comps != null)
		{
			int count = comps.Count;
			for (int i = 0; i < count; i++)
			{
				comps[i].PostDraw();
			}
		}
	}

	public override void DrawExtraSelectionOverlays()
	{
		base.DrawExtraSelectionOverlays();
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostDrawExtraSelectionOverlays();
			}
		}
	}

	public override void Print(SectionLayer layer)
	{
		base.Print(layer);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostPrintOnto(layer);
			}
		}
	}

	public virtual void PrintForPowerGrid(SectionLayer layer)
	{
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].CompPrintForPowerGrid(layer);
			}
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (comps == null)
		{
			yield break;
		}
		for (int i = 0; i < comps.Count; i++)
		{
			foreach (Gizmo item in comps[i].CompGetGizmosExtra())
			{
				yield return item;
			}
		}
	}

	public override bool TryAbsorbStack(Thing other, bool respectStackLimit)
	{
		if (!CanStackWith(other))
		{
			return false;
		}
		int count = ThingUtility.TryAbsorbStackNumToTake(this, other, respectStackLimit);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PreAbsorbStack(other, count);
			}
		}
		return base.TryAbsorbStack(other, respectStackLimit);
	}

	public override Thing SplitOff(int count)
	{
		Thing thing = base.SplitOff(count);
		if (thing != null && comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostSplitOff(thing);
			}
		}
		return thing;
	}

	public override bool CanStackWith(Thing other)
	{
		if (!base.CanStackWith(other))
		{
			return false;
		}
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				if (!comps[i].AllowStackWith(other))
				{
					return false;
				}
			}
		}
		return true;
	}

	public override TipSignal GetTooltip()
	{
		string text = LabelCap;
		if (def.useHitPoints)
		{
			text = text + "\n" + HitPoints + " / " + base.MaxHitPoints;
		}
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				string text2 = comps[i].CompTipStringExtra();
				if (!text2.NullOrEmpty())
				{
					text = text + "\n" + text2;
				}
			}
		}
		return new TipSignal(text, thingIDNumber * 251235);
	}

	public override string GetInspectString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.GetInspectString());
		string text = InspectStringPartsFromComps();
		if (!text.NullOrEmpty())
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.AppendLine();
			}
			stringBuilder.Append(text);
		}
		return stringBuilder.ToString();
	}

	protected string InspectStringPartsFromComps()
	{
		if (comps == null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < comps.Count; i++)
		{
			string text = comps[i].CompInspectStringExtra();
			if (!text.NullOrEmpty())
			{
				if (Prefs.DevMode && char.IsWhiteSpace(text[text.Length - 1]))
				{
					Log.ErrorOnce(string.Concat(comps[i].GetType(), " CompInspectStringExtra ended with whitespace: ", text), 25612);
					text = text.TrimEndNewlines();
				}
				if (stringBuilder.Length != 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.Append(text);
			}
		}
		return stringBuilder.ToString();
	}

	public override void DrawGUIOverlay()
	{
		base.DrawGUIOverlay();
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].DrawGUIOverlay();
			}
		}
	}

	public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
	{
		foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
		{
			yield return floatMenuOption;
		}
		if (comps == null)
		{
			yield break;
		}
		for (int i = 0; i < comps.Count; i++)
		{
			foreach (FloatMenuOption item in comps[i].CompFloatMenuOptions(selPawn))
			{
				yield return item;
			}
		}
	}

	public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(List<Pawn> selPawns)
	{
		foreach (FloatMenuOption multiSelectFloatMenuOption in base.GetMultiSelectFloatMenuOptions(selPawns))
		{
			yield return multiSelectFloatMenuOption;
		}
		if (comps == null)
		{
			yield break;
		}
		for (int i = 0; i < comps.Count; i++)
		{
			foreach (FloatMenuOption item in comps[i].CompMultiSelectFloatMenuOptions(selPawns))
			{
				yield return item;
			}
		}
	}

	public override void PreTraded(TradeAction action, Pawn playerNegotiator, ITrader trader)
	{
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PrePreTraded(action, playerNegotiator, trader);
			}
		}
		base.PreTraded(action, playerNegotiator, trader);
	}

	public override void PostGeneratedForTrader(TraderKindDef trader, int forTile, Faction forFaction)
	{
		base.PostGeneratedForTrader(trader, forTile, forFaction);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostPostGeneratedForTrader(trader, forTile, forFaction);
			}
		}
	}

	protected override void PrePostIngested(Pawn ingester)
	{
		base.PrePostIngested(ingester);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PrePostIngested(ingester);
			}
		}
	}

	protected override void PostIngested(Pawn ingester)
	{
		base.PostIngested(ingester);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].PostIngested(ingester);
			}
		}
	}

	public override void Notify_DefsHotReloaded()
	{
		base.Notify_DefsHotReloaded();
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_DefsHotReloaded();
			}
		}
	}

	public override void Notify_SignalReceived(Signal signal)
	{
		base.Notify_SignalReceived(signal);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_SignalReceived(signal);
			}
		}
	}

	public override void Notify_RecipeProduced(Pawn pawn)
	{
		base.Notify_RecipeProduced(pawn);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_RecipeProduced(pawn);
			}
		}
	}

	public override void Notify_LordDestroyed()
	{
		base.Notify_LordDestroyed();
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_LordDestroyed();
			}
		}
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		base.Notify_Equipped(pawn);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_Equipped(pawn);
			}
		}
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		base.Notify_Unequipped(pawn);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_Unequipped(pawn);
			}
		}
	}

	public override void Notify_UsedVerb(Pawn pawn, Verb verb)
	{
		base.Notify_UsedVerb(pawn, verb);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_UsedVerb(pawn, verb);
			}
		}
	}

	public override void Notify_UsedWeapon(Pawn pawn)
	{
		base.Notify_UsedWeapon(pawn);
		if (ModsConfig.IdeologyActive && pawn.Ideo != null)
		{
			switch (pawn.Ideo.GetDispositionForWeapon(def))
			{
			case IdeoWeaponDisposition.Despised:
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.UsedDespisedWeapon, pawn.Named(HistoryEventArgsNames.Doer)));
				break;
			case IdeoWeaponDisposition.Noble:
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.UsedNobleWeapon, pawn.Named(HistoryEventArgsNames.Doer)));
				break;
			}
		}
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_UsedWeapon(pawn);
			}
		}
	}

	public void Notify_KilledPawn(Pawn pawn)
	{
		if (ModsConfig.IdeologyActive && pawn.Ideo != null && def.IsWeapon)
		{
			switch (pawn.Ideo.GetDispositionForWeapon(def))
			{
			case IdeoWeaponDisposition.Despised:
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.KillWithDespisedWeapon, pawn.Named(HistoryEventArgsNames.Doer)));
				break;
			case IdeoWeaponDisposition.Noble:
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.KillWithNobleWeapon, pawn.Named(HistoryEventArgsNames.Doer)));
				break;
			}
		}
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_KilledPawn(pawn);
			}
		}
	}

	public override void Notify_AbandonedAtTile(int tile)
	{
		base.Notify_AbandonedAtTile(tile);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_AbandonedAtTile(tile);
			}
		}
	}

	public override void Notify_KilledLeavingsLeft(List<Thing> leavings)
	{
		base.Notify_KilledLeavingsLeft(leavings);
		if (comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_KilledLeavingsLeft(leavings);
			}
		}
	}

	public override void Notify_Studied(Pawn studier, float amount, KnowledgeCategoryDef category = null)
	{
		List<ThingComp> allComps = AllComps;
		for (int i = 0; i < allComps.Count; i++)
		{
			if (allComps[i] is IThingStudied thingStudied)
			{
				thingStudied.OnStudied(studier, amount, category);
			}
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		foreach (StatDrawEntry item in base.SpecialDisplayStats())
		{
			yield return item;
		}
		if (comps == null)
		{
			yield break;
		}
		for (int i = 0; i < comps.Count; i++)
		{
			IEnumerable<StatDrawEntry> enumerable = comps[i].SpecialDisplayStats();
			if (enumerable == null)
			{
				continue;
			}
			foreach (StatDrawEntry item2 in enumerable)
			{
				yield return item2;
			}
		}
	}

	public override void Notify_Explosion(Explosion explosion)
	{
		base.Notify_Explosion(explosion);
		CompWakeUpDormant comp = GetComp<CompWakeUpDormant>();
		if (comp != null && (explosion.Position - base.Position).LengthHorizontal <= explosion.radius)
		{
			comp.Activate(explosion.instigator);
		}
	}

	public override void Notify_MyMapRemoved()
	{
		base.Notify_MyMapRemoved();
		if (def.notifyMapRemoved && comps != null)
		{
			for (int i = 0; i < comps.Count; i++)
			{
				comps[i].Notify_MapRemoved();
			}
		}
	}
}
