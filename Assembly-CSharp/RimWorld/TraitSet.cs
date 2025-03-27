using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class TraitSet : IExposable
{
	protected Pawn pawn;

	public List<Trait> allTraits = new List<Trait>();

	public bool anyTraitHasIngestibleOverrides;

	private List<Trait> tmpTraits = new List<Trait>();

	public bool AnyTraitHasIngestibleOverrides => anyTraitHasIngestibleOverrides;

	public float HungerRateFactor
	{
		get
		{
			float num = 1f;
			foreach (Trait allTrait in allTraits)
			{
				if (!allTrait.Suppressed)
				{
					num *= allTrait.CurrentData.hungerRateFactor;
				}
			}
			return num;
		}
	}

	public List<Trait> TraitsSorted
	{
		get
		{
			tmpTraits.Clear();
			if (!allTraits.NullOrEmpty())
			{
				for (int i = 0; i < allTraits.Count; i++)
				{
					if (allTraits[i].sourceGene == null || !allTraits[i].Suppressed)
					{
						tmpTraits.Add(allTraits[i]);
					}
				}
				tmpTraits.SortBy((Trait y) => y.sourceGene != null, (Trait x) => x.Suppressed);
			}
			return tmpTraits;
		}
	}

	public IEnumerable<MentalBreakDef> TheOnlyAllowedMentalBreaks
	{
		get
		{
			for (int i = 0; i < allTraits.Count; i++)
			{
				Trait trait = allTraits[i];
				if (!trait.Suppressed && trait.CurrentData.theOnlyAllowedMentalBreaks != null)
				{
					for (int j = 0; j < trait.CurrentData.theOnlyAllowedMentalBreaks.Count; j++)
					{
						yield return trait.CurrentData.theOnlyAllowedMentalBreaks[j];
					}
				}
			}
		}
	}

	public TraitSet(Pawn pawn)
	{
		this.pawn = pawn;
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref allTraits, "allTraits", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			if (allTraits.RemoveAll((Trait x) => x == null) != 0)
			{
				Log.Error("Some traits were null after loading.");
			}
			if (allTraits.RemoveAll((Trait x) => x.def == null) != 0)
			{
				Log.Error("Some traits had null def after loading.");
			}
			for (int i = 0; i < allTraits.Count; i++)
			{
				allTraits[i].pawn = pawn;
			}
		}
		if (Scribe.mode != LoadSaveMode.PostLoadInit)
		{
			return;
		}
		CacheAnyTraitHasIngestibleOverrides();
		for (int num = allTraits.Count - 1; num >= 0; num--)
		{
			Trait t = allTraits[num];
			if (t.sourceGene?.def != null && (t.sourceGene.def.forcedTraits == null || !t.sourceGene.def.forcedTraits.Any((GeneticTraitData x) => x.def == t.def && x.degree == t.Degree)))
			{
				allTraits.Remove(t);
			}
		}
	}

	public bool DisableHostilityFrom(Pawn p)
	{
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (!allTraits[i].Suppressed)
			{
				if (p.Faction != null && allTraits[i].def.disableHostilityFromFaction == p.Faction.def)
				{
					return true;
				}
				if (allTraits[i].def.disableHostilityFromAnimalType.HasValue && allTraits[i].def.disableHostilityFromAnimalType == p.RaceProps.animalType)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsThoughtDisallowed(ThoughtDef thought)
	{
		if (pawn.story == null || thought == null)
		{
			return false;
		}
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].Suppressed)
			{
				continue;
			}
			TraitDegreeData currentData = allTraits[i].CurrentData;
			if (currentData.disallowedThoughts == null)
			{
				continue;
			}
			for (int j = 0; j < currentData.disallowedThoughts.Count; j++)
			{
				if (currentData.disallowedThoughts[j] == thought)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsThoughtFromIngestionDisallowed(ThoughtDef thought, ThingDef ingestible, MeatSourceCategory meatSourceCategory)
	{
		if (thought == null || ingestible == null)
		{
			return false;
		}
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].Suppressed)
			{
				continue;
			}
			TraitDegreeData currentData = allTraits[i].CurrentData;
			if (currentData.disallowedThoughtsFromIngestion == null)
			{
				continue;
			}
			for (int j = 0; j < currentData.disallowedThoughtsFromIngestion.Count; j++)
			{
				TraitIngestionThoughtsOverride traitIngestionThoughtsOverride = currentData.disallowedThoughtsFromIngestion[j];
				if (traitIngestionThoughtsOverride.thoughts.NullOrEmpty())
				{
					continue;
				}
				if (traitIngestionThoughtsOverride.thing != null)
				{
					if (traitIngestionThoughtsOverride.thing != ingestible)
					{
						continue;
					}
				}
				else if (meatSourceCategory == MeatSourceCategory.NotMeat || traitIngestionThoughtsOverride.meatSource != meatSourceCategory)
				{
					continue;
				}
				for (int k = 0; k < traitIngestionThoughtsOverride.thoughts.Count; k++)
				{
					if (traitIngestionThoughtsOverride.thoughts[k] == thought)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public void GetExtraThoughtsFromIngestion(List<ThoughtDef> buffer, ThingDef ingestible, MeatSourceCategory meatSourceCategory, bool direct)
	{
		if (ingestible == null || buffer == null)
		{
			return;
		}
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].Suppressed)
			{
				continue;
			}
			TraitDegreeData currentData = allTraits[i].CurrentData;
			if (currentData.extraThoughtsFromIngestion == null)
			{
				continue;
			}
			for (int j = 0; j < currentData.extraThoughtsFromIngestion.Count; j++)
			{
				TraitIngestionThoughtsOverride traitIngestionThoughtsOverride = currentData.extraThoughtsFromIngestion[j];
				if (traitIngestionThoughtsOverride.thing != null)
				{
					if (traitIngestionThoughtsOverride.thing != ingestible)
					{
						continue;
					}
				}
				else if (meatSourceCategory == MeatSourceCategory.NotMeat || traitIngestionThoughtsOverride.meatSource != meatSourceCategory)
				{
					continue;
				}
				if (!traitIngestionThoughtsOverride.thoughts.NullOrEmpty())
				{
					buffer.AddRange(traitIngestionThoughtsOverride.thoughts);
				}
				if (direct)
				{
					if (!traitIngestionThoughtsOverride.thoughtsDirect.NullOrEmpty())
					{
						buffer.AddRange(traitIngestionThoughtsOverride.thoughtsDirect);
					}
				}
				else if (!traitIngestionThoughtsOverride.thoughtsAsIngredient.NullOrEmpty())
				{
					buffer.AddRange(traitIngestionThoughtsOverride.thoughtsAsIngredient);
				}
			}
		}
	}

	public void GainTrait(Trait trait, bool suppressConflicts = false)
	{
		if (!suppressConflicts && HasTrait(trait.def))
		{
			Log.Warning(string.Concat(pawn, " already has trait ", trait.def));
		}
		else
		{
			if (HasTrait(trait.def, trait.Degree))
			{
				return;
			}
			allTraits.Add(trait);
			trait.pawn = pawn;
			if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && suppressConflicts)
			{
				if (allTraits.Any((Trait x) => x != trait && trait.def.CanSuppress(x) && !x.def.canBeSuppressed))
				{
					trait.suppressedByTrait = true;
				}
				else
				{
					for (int i = 0; i < allTraits.Count; i++)
					{
						if (allTraits[i] != trait && trait.def.CanSuppress(allTraits[i]))
						{
							allTraits[i].suppressedByTrait = true;
						}
					}
				}
				RecalculateSuppression();
			}
			pawn.Notify_DisabledWorkTypesChanged();
			if (trait.CurrentData.HasDefinedGraphicProperties)
			{
				pawn.Drawer.renderer.SetAllGraphicsDirty();
			}
			if (pawn.skills != null)
			{
				pawn.skills.Notify_SkillDisablesChanged();
				pawn.skills.DirtyAptitudes();
			}
			if (!pawn.Dead && pawn.RaceProps.Humanlike && pawn.needs.mood != null)
			{
				pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
			}
			List<AbilityDef> abilities = trait.def.DataAtDegree(trait.Degree).abilities;
			if (!abilities.NullOrEmpty())
			{
				for (int j = 0; j < abilities.Count; j++)
				{
					pawn.abilities.GainAbility(abilities[j]);
				}
			}
			if (((trait.def.disableHostilityFromAnimalType.HasValue && trait.def.disableHostilityFromAnimalType != AnimalType.None) || trait.def.disableHostilityFromFaction != null) && pawn.Map != null)
			{
				pawn.Map.attackTargetsCache.UpdateTarget(pawn);
			}
			CacheAnyTraitHasIngestibleOverrides();
			if (!trait.CurrentData.needs.NullOrEmpty() || !trait.CurrentData.disablesNeeds.NullOrEmpty())
			{
				pawn.needs?.AddOrRemoveNeedsAsAppropriate();
			}
			MeditationFocusTypeAvailabilityCache.ClearFor(pawn);
		}
	}

	public void RemoveTrait(Trait trait, bool unsuppressConflicts = false)
	{
		if (!HasTrait(trait.def))
		{
			Log.Warning(string.Concat("Trying to remove ", trait.Label, " but ", pawn, " doesn't have it."));
			return;
		}
		List<AbilityDef> abilities = trait.def.DataAtDegree(trait.Degree).abilities;
		if (!abilities.NullOrEmpty())
		{
			for (int i = 0; i < abilities.Count; i++)
			{
				pawn.abilities.RemoveAbility(abilities[i]);
			}
		}
		allTraits.Remove(trait);
		if (trait.sourceGene != null)
		{
			pawn.genes.RemoveGene(trait.sourceGene);
		}
		if ((ModsConfig.BiotechActive || ModsConfig.AnomalyActive) && unsuppressConflicts)
		{
			for (int j = 0; j < allTraits.Count; j++)
			{
				if (!allTraits[j].Suppressed)
				{
					continue;
				}
				bool flag = false;
				for (int k = 0; k < allTraits.Count; k++)
				{
					if (allTraits[j] != allTraits[k] && (allTraits[k].sourceGene == null || !allTraits[k].sourceGene.Overridden) && allTraits[j].def.CanSuppress(allTraits[k]))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					allTraits[j].suppressedByTrait = false;
				}
			}
		}
		pawn.Notify_DisabledWorkTypesChanged();
		if (trait.CurrentData.HasDefinedGraphicProperties)
		{
			pawn.Drawer.renderer.SetAllGraphicsDirty();
		}
		if (pawn.skills != null)
		{
			pawn.skills.Notify_SkillDisablesChanged();
			pawn.skills.DirtyAptitudes();
		}
		if (!pawn.Dead && pawn.RaceProps.Humanlike && pawn.needs.mood != null)
		{
			pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
		}
		if (((trait.def.disableHostilityFromAnimalType.HasValue && trait.def.disableHostilityFromAnimalType != AnimalType.None) || trait.def.disableHostilityFromFaction != null) && pawn.Map != null)
		{
			pawn.Map.attackTargetsCache.UpdateTarget(pawn);
		}
		CacheAnyTraitHasIngestibleOverrides();
		if (!trait.CurrentData.needs.NullOrEmpty() || !trait.CurrentData.disablesNeeds.NullOrEmpty())
		{
			pawn.needs?.AddOrRemoveNeedsAsAppropriate();
		}
		MeditationFocusTypeAvailabilityCache.ClearFor(pawn);
	}

	public void Notify_GeneRemoved(Gene gene)
	{
		if (!ModsConfig.BiotechActive)
		{
			return;
		}
		for (int num = allTraits.Count - 1; num >= 0; num--)
		{
			if (allTraits[num].sourceGene == gene)
			{
				RemoveTrait(allTraits[num], unsuppressConflicts: true);
			}
		}
		RecalculateSuppression();
	}

	public void RecalculateSuppression()
	{
		if (!ModsConfig.BiotechActive || pawn.genes == null)
		{
			return;
		}
		List<Gene> genesListForReading = pawn.genes.GenesListForReading;
		int i;
		for (i = 0; i < allTraits.Count; i++)
		{
			bool flag = false;
			if (allTraits[i].sourceGene != null && allTraits[i].sourceGene.Overridden)
			{
				allTraits[i].suppressedByGene = allTraits[i].sourceGene.overriddenByGene;
				flag = true;
			}
			else
			{
				for (int j = 0; j < genesListForReading.Count; j++)
				{
					if (genesListForReading[j].Active && genesListForReading[j].def.suppressedTraits != null && genesListForReading[j].def.suppressedTraits.Any((GeneticTraitData x) => x.def == allTraits[i].def && x.degree == allTraits[i].Degree))
					{
						allTraits[i].suppressedByGene = genesListForReading[j];
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				allTraits[i].suppressedByGene = null;
			}
		}
	}

	public bool HasTrait(TraitDef tDef)
	{
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].def == tDef && !allTraits[i].Suppressed)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasTrait(TraitDef tDef, int degree)
	{
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].def == tDef && allTraits[i].Degree == degree && !allTraits[i].Suppressed)
			{
				return true;
			}
		}
		return false;
	}

	public Trait GetTrait(TraitDef tDef)
	{
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].def == tDef && !allTraits[i].Suppressed)
			{
				return allTraits[i];
			}
		}
		return null;
	}

	public Trait GetTrait(TraitDef tDef, int degree)
	{
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].def == tDef && allTraits[i].Degree == degree && !allTraits[i].Suppressed)
			{
				return allTraits[i];
			}
		}
		return null;
	}

	public int DegreeOfTrait(TraitDef tDef)
	{
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (allTraits[i].def == tDef && !allTraits[i].Suppressed)
			{
				return allTraits[i].Degree;
			}
		}
		return 0;
	}

	private void CacheAnyTraitHasIngestibleOverrides()
	{
		anyTraitHasIngestibleOverrides = false;
		for (int i = 0; i < allTraits.Count; i++)
		{
			if (!allTraits[i].Suppressed && !allTraits[i].CurrentData.ingestibleModifiers.NullOrEmpty())
			{
				anyTraitHasIngestibleOverrides = true;
				break;
			}
		}
	}
}
