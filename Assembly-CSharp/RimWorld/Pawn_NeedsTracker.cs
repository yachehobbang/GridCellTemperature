using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class Pawn_NeedsTracker : IExposable
{
	private Pawn pawn;

	private List<Need> needs = new List<Need>();

	public Need_Mood mood;

	public Need_Food food;

	public Need_MechEnergy energy;

	public Need_Rest rest;

	public Need_Joy joy;

	public Need_Beauty beauty;

	public Need_RoomSize roomsize;

	public Need_Outdoors outdoors;

	public Need_Indoors indoors;

	public Need_Chemical_Any drugsDesire;

	public Need_Comfort comfort;

	public Need_Learning learning;

	public Need_Play play;

	private List<Need> needsMisc = new List<Need>(0);

	public List<Need> AllNeeds => needs;

	public List<Need> MiscNeeds => needsMisc;

	public Pawn_NeedsTracker()
	{
	}

	public Pawn_NeedsTracker(Pawn newPawn)
	{
		pawn = newPawn;
		AddOrRemoveNeedsAsAppropriate();
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref needs, "needs", LookMode.Deep, pawn);
		if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			if (needs.RemoveAll((Need x) => x?.def == null) != 0)
			{
				Log.Error("Pawn " + pawn.ToStringSafe() + " had some null needs after loading.");
			}
			BindDirectNeedFields();
			CacheMiscNeeds();
		}
		BackCompatibility.PostExposeData(this);
	}

	public void BindDirectNeedFields()
	{
		mood = TryGetNeed<Need_Mood>();
		food = TryGetNeed<Need_Food>();
		energy = TryGetNeed<Need_MechEnergy>();
		rest = TryGetNeed<Need_Rest>();
		joy = TryGetNeed<Need_Joy>();
		beauty = TryGetNeed<Need_Beauty>();
		comfort = TryGetNeed<Need_Comfort>();
		roomsize = TryGetNeed<Need_RoomSize>();
		outdoors = TryGetNeed<Need_Outdoors>();
		indoors = TryGetNeed<Need_Indoors>();
		drugsDesire = TryGetNeed<Need_Chemical_Any>();
		learning = TryGetNeed<Need_Learning>();
		play = TryGetNeed<Need_Play>();
	}

	private void CacheMiscNeeds()
	{
		needsMisc.Clear();
	}

	public void NeedsTrackerTick()
	{
		if (pawn.IsHashIntervalTick(150))
		{
			for (int i = 0; i < needs.Count; i++)
			{
				needs[i].NeedInterval();
			}
		}
	}

	public T TryGetNeed<T>() where T : Need
	{
		for (int i = 0; i < needs.Count; i++)
		{
			if (needs[i].GetType() == typeof(T))
			{
				return (T)needs[i];
			}
		}
		return null;
	}

	public Need TryGetNeed(NeedDef def)
	{
		for (int i = 0; i < needs.Count; i++)
		{
			if (needs[i].def == def)
			{
				return needs[i];
			}
		}
		return null;
	}

	public void SetInitialLevels()
	{
		pawn.GetStatValue(StatDefOf.MaxNutrition, applyPostProcess: true, 0);
		for (int i = 0; i < needs.Count; i++)
		{
			needs[i].SetInitialLevel();
		}
	}

	public void AddOrRemoveNeedsAsAppropriate()
	{
		List<NeedDef> allDefsListForReading = DefDatabase<NeedDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			try
			{
				NeedDef needDef = allDefsListForReading[i];
				if (ShouldHaveNeed(needDef))
				{
					if (TryGetNeed(needDef) == null)
					{
						AddNeed(needDef);
					}
				}
				else if (TryGetNeed(needDef) != null)
				{
					RemoveNeed(needDef);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Error while determining if " + pawn.ToStringSafe() + " should have Need " + allDefsListForReading[i].ToStringSafe() + ": " + ex);
			}
		}
	}

	public bool EnjoysOutdoors()
	{
		if (!pawn.story.traits.HasTrait(TraitDefOf.Undergrounder))
		{
			if (pawn.Ideo != null)
			{
				return !pawn.Ideo.IdeoDisablesCrampedRoomThoughts();
			}
			return true;
		}
		return false;
	}

	private bool ShouldHaveNeed(NeedDef nd)
	{
		if ((int)pawn.RaceProps.intelligence < (int)nd.minIntelligence)
		{
			return false;
		}
		if (!nd.developmentalStageFilter.Has(pawn.DevelopmentalStage))
		{
			return false;
		}
		if (nd.colonistsOnly && (pawn.Faction == null || !pawn.Faction.IsPlayer))
		{
			return false;
		}
		if (nd.playerMechsOnly && (!pawn.RaceProps.IsMechanoid || pawn.Faction != Faction.OfPlayer || pawn.OverseerSubject == null))
		{
			return false;
		}
		if (nd.colonistAndPrisonersOnly && (pawn.Faction == null || !pawn.Faction.IsPlayer) && (pawn.HostFaction == null || pawn.HostFaction != Faction.OfPlayer))
		{
			return false;
		}
		if (pawn.health.hediffSet.hediffs.Any(delegate(Hediff x)
		{
			HediffDef def = x.def;
			return def != null && def.disablesNeeds?.Contains(nd) == true;
		}))
		{
			return false;
		}
		if (ModsConfig.BiotechActive && pawn.genes != null && pawn.genes.GenesListForReading.Any((Gene x) => x.Active && x.def.disablesNeeds != null && x.def.disablesNeeds.Contains(nd)))
		{
			return false;
		}
		if (pawn.story?.traits != null && pawn.story.traits.allTraits.Any((Trait x) => !x.Suppressed && x.CurrentData.disablesNeeds.NotNullAndContains(nd)))
		{
			return false;
		}
		if (nd.onlyIfCausedByHediff && !pawn.health.hediffSet.hediffs.Any((Hediff x) => x.def?.causesNeed == nd))
		{
			return false;
		}
		if (ModsConfig.BiotechActive && nd.onlyIfCausedByGene && (pawn.genes == null || !pawn.genes.GenesListForReading.Any((Gene x) => x.Active && x.def.causesNeed == nd)))
		{
			return false;
		}
		if (nd.neverOnPrisoner && pawn.IsPrisoner)
		{
			return false;
		}
		if (nd.neverOnSlave && pawn.IsSlave)
		{
			return false;
		}
		if (pawn.IsMutant && !pawn.mutant.Def.enabledNeeds.Contains(nd))
		{
			return false;
		}
		if (nd.titleRequiredAny != null)
		{
			if (pawn.royalty == null)
			{
				return false;
			}
			bool flag = false;
			foreach (RoyalTitle item in pawn.royalty.AllTitlesInEffectForReading)
			{
				if (nd.titleRequiredAny.Contains(item.def))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		if (nd.nullifyingPrecepts != null && pawn.Ideo != null)
		{
			bool flag2 = false;
			foreach (PreceptDef nullifyingPrecept in nd.nullifyingPrecepts)
			{
				if (pawn.Ideo.HasPrecept(nullifyingPrecept))
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				return false;
			}
		}
		if (nd.hediffRequiredAny != null)
		{
			bool flag3 = false;
			foreach (HediffDef item2 in nd.hediffRequiredAny)
			{
				if (pawn.health.hediffSet.HasHediff(item2))
				{
					flag3 = true;
					break;
				}
			}
			if (!flag3)
			{
				return false;
			}
		}
		if (nd.defName == "Authority")
		{
			return false;
		}
		if (nd.onlyIfCausedByTrait)
		{
			List<Trait> list = pawn.story?.traits?.allTraits;
			if (list.NullOrEmpty())
			{
				return false;
			}
			bool flag4 = false;
			for (int i = 0; i < list.Count; i++)
			{
				Trait trait = list[i];
				if (!trait.CurrentData.needs.NullOrEmpty() && trait.CurrentData.needs.Contains(nd) && !trait.Suppressed)
				{
					flag4 = true;
				}
			}
			if (!flag4)
			{
				return false;
			}
		}
		if (nd.slavesOnly && !pawn.IsSlave)
		{
			return false;
		}
		if (ModsConfig.AnomalyActive && nd.requiredComps != null)
		{
			foreach (CompProperties requiredComp in nd.requiredComps)
			{
				if (pawn.TryGetComp(requiredComp) == null)
				{
					return false;
				}
			}
		}
		if (nd == NeedDefOf.Food)
		{
			return pawn.RaceProps.EatsFood;
		}
		if (nd == NeedDefOf.Rest)
		{
			return pawn.RaceProps.needsRest;
		}
		return true;
	}

	private void AddNeed(NeedDef nd)
	{
		Need need = (Need)Activator.CreateInstance(nd.needClass, pawn);
		need.def = nd;
		needs.Add(need);
		need.SetInitialLevel();
		BindDirectNeedFields();
	}

	private void RemoveNeed(NeedDef nd)
	{
		Need need = TryGetNeed(nd);
		if (need != null)
		{
			need.OnNeedRemoved();
			needs.Remove(need);
			BindDirectNeedFields();
		}
	}

	public IEnumerable<Gizmo> GetGizmos()
	{
		if (DebugSettings.ShowDevGizmos && energy != null)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Mech energy +5%",
				action = delegate
				{
					energy.CurLevelPercentage += 0.05f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "DEV: Mech energy -5%",
				action = delegate
				{
					energy.CurLevelPercentage -= 0.05f;
				}
			};
		}
	}
}
