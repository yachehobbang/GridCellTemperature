using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI;

namespace Verse;

public class RaceProperties
{
	public Intelligence intelligence;

	private FleshTypeDef fleshType;

	private ThingDef bloodDef;

	private ThingDef bloodSmearDef;

	public bool hasGenders = true;

	public bool needsRest = true;

	public ThinkTreeDef thinkTreeMain;

	public ThinkTreeDef thinkTreeConstant;

	public DutyDef dutyBoss;

	public PawnNameCategory nameCategory;

	public FoodTypeFlags foodType;

	public BodyDef body;

	public DeathActionProperties deathAction = new DeathActionProperties();

	public List<AnimalBiomeRecord> wildBiomes;

	public SimpleCurve ageGenerationCurve;

	public bool makesFootprints;

	public int executionRange = 2;

	public float lifeExpectancy = 10f;

	public List<HediffGiverSetDef> hediffGiverSets;

	public float? roamMtbDays;

	public bool allowedOnCaravan = true;

	public bool canReleaseToWild = true;

	public bool playerCanChangeMaster = true;

	public bool showTrainables = true;

	public bool hideTrainingTab;

	public bool doesntMove;

	public PawnRenderTreeDef renderTree;

	public AnimationDef startingAnimation;

	public ThingDef linkedCorpseKind;

	public bool canOpenFactionlessDoors = true;

	public bool alwaysAwake;

	public bool alwaysViolent;

	public bool isImmuneToInfections;

	public float bleedRateFactor = 1f;

	public bool canBecomeShambler;

	public bool herdAnimal;

	public bool packAnimal;

	public bool predator;

	public float maxPreyBodySize = 99999f;

	public float wildness;

	public float petness;

	public float nuzzleMtbHours = -1f;

	public float manhunterOnDamageChance;

	public float manhunterOnTameFailChance;

	public bool canBePredatorPrey = true;

	public bool herdMigrationAllowed = true;

	public AnimalType animalType;

	public List<ThingDef> willNeverEat;

	public bool giveNonToolUserBeatFireVerb;

	public bool disableIgniteVerb;

	public bool disableAreaControl;

	public int maxMechEnergy = 100;

	public List<WorkTypeDef> mechEnabledWorkTypes = new List<WorkTypeDef>();

	public int mechFixedSkillLevel = 10;

	public List<MechWorkTypePriority> mechWorkTypePriorities;

	public int? bulletStaggerDelayTicks;

	public float? bulletStaggerSpeedFactor;

	public EffecterDef bulletStaggerEffecterDef;

	public bool bulletStaggerIgnoreBodySize;

	public MechWeightClass mechWeightClass = MechWeightClass.Medium;

	public List<DetritusLeavingType> detritusLeavings = new List<DetritusLeavingType>();

	public bool overrideShouldHaveAbilityTracker;

	public float gestationPeriodDays = -1f;

	public SimpleCurve litterSizeCurve;

	public float mateMtbHours = 12f;

	[NoTranslate]
	public List<string> untrainableTags;

	[NoTranslate]
	public List<string> trainableTags;

	public TrainabilityDef trainability;

	private RulePackDef nameGenerator;

	private RulePackDef nameGeneratorFemale;

	public float nameOnTameChance;

	public float baseBodySize = 1f;

	public float baseHealthScale = 1f;

	public float baseHungerRate = 1f;

	public List<LifeStageAge> lifeStageAges = new List<LifeStageAge>();

	public List<LifeStageWorkSettings> lifeStageWorkSettings = new List<LifeStageWorkSettings>();

	public bool hasMeat = true;

	[MustTranslate]
	public string meatLabel;

	public Color meatColor = Color.white;

	public float meatMarketValue = 2f;

	public ThingDef specificMeatDef;

	public ThingDef useMeatFrom;

	public ThingDef useLeatherFrom;

	public bool hasCorpse = true;

	public bool hasUnnaturalCorpse;

	public bool corpseHiddenWhileUndiscovered;

	public ThingDef leatherDef;

	public ShadowData specialShadowData;

	public List<Vector3> headPosPerRotation;

	public IntRange soundCallIntervalRange = new IntRange(2000, 4000);

	public float soundCallIntervalFriendlyFactor = 1f;

	public float soundCallIntervalAggressiveFactor = 0.25f;

	public SoundDef soundMeleeHitPawn;

	public SoundDef soundMeleeHitBuilding;

	public SoundDef soundMeleeMiss;

	public SoundDef soundMeleeDodge;

	public SoundDef soundAmbience;

	public SoundDef soundMoving;

	public SoundDef soundEating;

	public KnowledgeCategoryDef knowledgeCategory;

	public int anomalyKnowledge;

	[Unsaved(false)]
	public ThingDef meatDef;

	[Unsaved(false)]
	public ThingDef corpseDef;

	[Unsaved(false)]
	public ThingDef unnaturalCorpseDef;

	[Unsaved(false)]
	private PawnKindDef cachedAnyPawnKind;

	public bool Humanlike => (int)intelligence >= 2;

	public bool ToolUser => (int)intelligence >= 1;

	public bool Animal
	{
		get
		{
			if (!ToolUser && IsFlesh)
			{
				return !IsAnomalyEntity;
			}
			return false;
		}
	}

	public bool Insect => FleshType == FleshTypeDefOf.Insectoid;

	public bool Dryad => animalType == AnimalType.Dryad;

	public bool ShouldHaveAbilityTracker
	{
		get
		{
			if (!Humanlike && !IsMechanoid)
			{
				return overrideShouldHaveAbilityTracker;
			}
			return true;
		}
	}

	public bool EatsFood => foodType != FoodTypeFlags.None;

	public float FoodLevelPercentageWantEat => ResolvedDietCategory switch
	{
		DietCategory.NeverEats => 0.3f, 
		DietCategory.Omnivorous => 0.3f, 
		DietCategory.Carnivorous => 0.3f, 
		DietCategory.Ovivorous => 0.4f, 
		DietCategory.Herbivorous => 0.45f, 
		DietCategory.Dendrovorous => 0.45f, 
		_ => throw new InvalidOperationException(), 
	};

	public DietCategory ResolvedDietCategory
	{
		get
		{
			if (!EatsFood)
			{
				return DietCategory.NeverEats;
			}
			if (Eats(FoodTypeFlags.Tree))
			{
				return DietCategory.Dendrovorous;
			}
			if (Eats(FoodTypeFlags.Meat))
			{
				if (Eats(FoodTypeFlags.VegetableOrFruit) || Eats(FoodTypeFlags.Plant))
				{
					return DietCategory.Omnivorous;
				}
				return DietCategory.Carnivorous;
			}
			if (Eats(FoodTypeFlags.AnimalProduct))
			{
				return DietCategory.Ovivorous;
			}
			return DietCategory.Herbivorous;
		}
	}

	public DeathActionWorker DeathActionWorker => deathAction.Worker;

	public FleshTypeDef FleshType => fleshType ?? FleshTypeDefOf.Normal;

	public bool IsMechanoid => FleshType == FleshTypeDefOf.Mechanoid;

	public bool IsFlesh => FleshType.isOrganic;

	public bool IsAnomalyEntity
	{
		get
		{
			if (ModsConfig.AnomalyActive)
			{
				if (FleshType != FleshTypeDefOf.EntityMechanical && FleshType != FleshTypeDefOf.EntityFlesh)
				{
					return FleshType == FleshTypeDefOf.Fleshbeast;
				}
				return true;
			}
			return false;
		}
	}

	public ThingDef BloodDef => bloodDef;

	public ThingDef BloodSmearDef => bloodSmearDef;

	public bool CanDoHerdMigration
	{
		get
		{
			if (Animal)
			{
				return herdMigrationAllowed;
			}
			return false;
		}
	}

	public bool CanPassFences => !FenceBlocked;

	public bool FenceBlocked => Roamer;

	public bool Roamer => roamMtbDays.HasValue;

	public bool IsWorkMech => !mechEnabledWorkTypes.NullOrEmpty();

	public PawnKindDef AnyPawnKind
	{
		get
		{
			if (cachedAnyPawnKind == null)
			{
				List<PawnKindDef> allDefsListForReading = DefDatabase<PawnKindDef>.AllDefsListForReading;
				for (int i = 0; i < allDefsListForReading.Count; i++)
				{
					if (allDefsListForReading[i].race.race == this)
					{
						cachedAnyPawnKind = allDefsListForReading[i];
						break;
					}
				}
			}
			return cachedAnyPawnKind;
		}
	}

	public RulePackDef GetNameGenerator(Gender gender)
	{
		if (gender == Gender.Female && nameGeneratorFemale != null)
		{
			return nameGeneratorFemale;
		}
		return nameGenerator;
	}

	public bool CanEverEat(Thing t)
	{
		return CanEverEat(t.def);
	}

	public bool CanEverEat(ThingDef t)
	{
		if (!EatsFood)
		{
			return false;
		}
		if (t.ingestible == null)
		{
			return false;
		}
		if (t.ingestible.preferability == FoodPreferability.Undefined)
		{
			return false;
		}
		if (willNeverEat != null && willNeverEat.Contains(t))
		{
			return false;
		}
		return Eats(t.ingestible.foodType);
	}

	public bool Eats(FoodTypeFlags food)
	{
		if (!EatsFood)
		{
			return false;
		}
		return (foodType & food) != 0;
	}

	public void ResolveReferencesSpecial()
	{
		if (specificMeatDef != null)
		{
			meatDef = specificMeatDef;
		}
		else if (useMeatFrom != null)
		{
			meatDef = useMeatFrom.race.meatDef;
		}
		if (useLeatherFrom != null)
		{
			leatherDef = useLeatherFrom.race.leatherDef;
		}
	}

	public IEnumerable<string> ConfigErrors(ThingDef thingDef)
	{
		if (thingDef.IsCorpse)
		{
			yield break;
		}
		if (predator && !Eats(FoodTypeFlags.Meat))
		{
			yield return "predator but doesn't eat meat";
		}
		for (int i = 0; i < lifeStageAges.Count; i++)
		{
			for (int j = 0; j < i; j++)
			{
				if (lifeStageAges[j].minAge > lifeStageAges[i].minAge)
				{
					yield return "lifeStages minAges are not in ascending order";
				}
			}
		}
		if (thingDef.IsCaravanRideable() && !lifeStageAges.Any((LifeStageAge s) => s.def.caravanRideable))
		{
			yield return "must contain at least one lifeStage with caravanRideable when CaravanRidingSpeedFactor is defined";
		}
		if (litterSizeCurve != null)
		{
			foreach (string item in litterSizeCurve.ConfigErrors("litterSizeCurve"))
			{
				yield return item;
			}
		}
		if (nameOnTameChance > 0f && nameGenerator == null)
		{
			yield return "can be named, but has no nameGenerator";
		}
		if (Animal && wildness < 0f)
		{
			yield return "is animal but wildness is not defined";
		}
		if (specificMeatDef != null && useMeatFrom != null)
		{
			yield return "specificMeatDef and useMeatFrom are both non-null. specificMeatDef will be chosen.";
		}
		if (useMeatFrom != null && useMeatFrom.category != ThingCategory.Pawn)
		{
			yield return "tries to use meat from non-pawn " + useMeatFrom;
		}
		if (useMeatFrom?.race.useMeatFrom != null)
		{
			yield return string.Concat("tries to use meat from ", useMeatFrom, " which uses meat from ", useMeatFrom.race.useMeatFrom);
		}
		if (useLeatherFrom != null && useLeatherFrom.category != ThingCategory.Pawn)
		{
			yield return "tries to use leather from non-pawn " + useLeatherFrom;
		}
		if (useLeatherFrom != null && useLeatherFrom.race.useLeatherFrom != null)
		{
			yield return string.Concat("tries to use leather from ", useLeatherFrom, " which uses leather from ", useLeatherFrom.race.useLeatherFrom);
		}
		if (Animal && trainability == null)
		{
			yield return "animal has trainability = null";
		}
		if (fleshType == FleshTypeDefOf.Normal && gestationPeriodDays < 0f)
		{
			yield return "normal flesh but gestationPeriodDays not configured.";
		}
		if (IsMechanoid && thingDef.butcherProducts.NullOrEmpty())
		{
			yield return thingDef.label + " mech has no butcher products set";
		}
		foreach (string item2 in deathAction.ConfigErrors())
		{
			yield return item2;
		}
		if (renderTree == null)
		{
			yield return "renderTree is null";
		}
	}

	public IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef, StatRequest req)
	{
		Pawn pawnThing = req.Pawn ?? (req.Thing as Pawn);
		if (!ModsConfig.BiotechActive || !Humanlike || pawnThing?.genes == null || pawnThing.genes.Xenotype == XenotypeDefOf.Baseliner)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "Race".Translate(), parentDef.LabelCap, parentDef.description, 2100, null, null, forceUnfinalizedMode: false, overridesHideStats: true);
		}
		if (pawnThing != null && pawnThing.IsMutant)
		{
			string text = pawnThing.mutant.Def.foodType.ToHumanString().CapitalizeFirst();
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "Diet".Translate(), text, "Stat_Race_Diet_Desc".Translate(text), 1500);
		}
		else if (!parentDef.race.IsMechanoid && !parentDef.race.IsAnomalyEntity)
		{
			string text2 = foodType.ToHumanString().CapitalizeFirst();
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "Diet".Translate(), text2, "Stat_Race_Diet_Desc".Translate(text2), 1500);
		}
		if (req.Thing is Pawn pawn && pawn.needs?.food != null)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "FoodConsumption".Translate(), NutritionEatenPerDay(pawn), NutritionEatenPerDayExplanation(pawn), 1600);
		}
		if (parentDef.race.leatherDef != null)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "LeatherType".Translate(), parentDef.race.leatherDef.LabelCap, "Stat_Race_LeatherType_Desc".Translate(), 3550, null, new Dialog_InfoCard.Hyperlink[1]
			{
				new Dialog_InfoCard.Hyperlink(parentDef.race.leatherDef)
			});
		}
		if (parentDef.race.Animal || wildness > 0f)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "Wildness".Translate(), wildness.ToStringPercent(), TrainableUtility.GetWildnessExplanation(parentDef), 2050);
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "HarmedRevengeChance".Translate(), PawnUtility.GetManhunterOnDamageChance(parentDef.race).ToStringPercent(), "HarmedRevengeChanceExplanation".Translate(), 510);
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "TameFailedRevengeChance".Translate(), parentDef.race.manhunterOnTameFailChance.ToStringPercent(), "Stat_Race_Animal_TameFailedRevengeChance_Desc".Translate(), 511);
		}
		if ((int)intelligence < 2 && trainability != null && !parentDef.race.IsAnomalyEntity)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "Trainability".Translate(), trainability.LabelCap, "Stat_Race_Trainability_Desc".Translate(), 2500);
		}
		yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "StatsReport_LifeExpectancy".Translate(), lifeExpectancy.ToStringByStyle(ToStringStyle.Integer), "Stat_Race_LifeExpectancy_Desc".Translate(), 2000);
		if (parentDef.race.Animal || FenceBlocked)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "StatsReport_BlockedByFences".Translate(), FenceBlocked ? "Yes".Translate() : "No".Translate(), "Stat_Race_BlockedByFences_Desc".Translate(), 2040);
		}
		if (parentDef.race.Animal)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "PackAnimal".Translate(), packAnimal ? "Yes".Translate() : "No".Translate(), "PackAnimalExplanation".Translate(), 2202);
			if (req.Thing is Pawn { gender: not Gender.None } pawn2)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "Sex".Translate(), pawn2.gender.GetLabel(animal: true).CapitalizeFirst(), pawn2.gender.GetLabel(animal: true).CapitalizeFirst(), 2208);
			}
			if (parentDef.race.nuzzleMtbHours > 0f)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.PawnSocial, "NuzzleInterval".Translate(), Mathf.RoundToInt(parentDef.race.nuzzleMtbHours * 2500f).ToStringTicksToPeriod(), "NuzzleIntervalExplanation".Translate(), 500);
			}
			if (parentDef.race.roamMtbDays.HasValue)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "StatsReport_RoamInterval".Translate(), Mathf.RoundToInt(parentDef.race.roamMtbDays.Value * 60000f).ToStringTicksToPeriod(), "Stat_Race_RoamInterval_Desc".Translate(), 2030);
			}
			foreach (StatDrawEntry item in AnimalProductionUtility.AnimalProductionStats(parentDef))
			{
				yield return item;
			}
		}
		if (!ModsConfig.BiotechActive || !IsMechanoid)
		{
			yield break;
		}
		yield return new StatDrawEntry(StatCategoryDefOf.Mechanoid, "MechWeightClass".Translate(), mechWeightClass.ToStringHuman().CapitalizeFirst(), "MechWeightClassExplanation".Translate(), 500);
		ThingDef thingDef = MechanitorUtility.RechargerForMech(parentDef);
		if (thingDef != null)
		{
			yield return new StatDrawEntry(StatCategoryDefOf.Mechanoid, "StatsReport_RechargerNeeded".Translate(), thingDef.LabelCap, "StatsReport_RechargerNeeded_Desc".Translate(), 503, null, new Dialog_InfoCard.Hyperlink[1]
			{
				new Dialog_InfoCard.Hyperlink(thingDef)
			});
		}
		foreach (StatDrawEntry item2 in MechWorkUtility.SpecialDisplayStats(parentDef, req))
		{
			yield return item2;
		}
	}

	public static string NutritionEatenPerDay(Pawn p)
	{
		return (p.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * 60000f).ToString("0.##");
	}

	public static string NutritionEatenPerDayExplanation(Pawn p, bool showDiet = false, bool showLegend = false, bool showCalculations = true)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("NutritionEatenPerDayTip".Translate(ThingDefOf.MealSimple.GetStatValueAbstract(StatDefOf.Nutrition).ToString("0.##")));
		stringBuilder.AppendLine();
		if (showDiet)
		{
			stringBuilder.AppendLine("CanEat".Translate() + ": " + p.RaceProps.foodType.ToHumanString());
			stringBuilder.AppendLine();
		}
		if (showLegend)
		{
			stringBuilder.AppendLine("Legend".Translate() + ":");
			stringBuilder.AppendLine("NoDietCategoryLetter".Translate() + " - " + DietCategory.Omnivorous.ToStringHuman());
			DietCategory[] array = (DietCategory[])Enum.GetValues(typeof(DietCategory));
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != 0 && array[i] != DietCategory.Omnivorous)
				{
					stringBuilder.AppendLine(array[i].ToStringHumanShort() + " - " + array[i].ToStringHuman());
				}
			}
			stringBuilder.AppendLine();
		}
		if (showCalculations)
		{
			stringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + (p.ageTracker.CurLifeStage.hungerRateFactor * p.RaceProps.baseHungerRate * 2.6666667E-05f * 60000f).ToStringByStyle(ToStringStyle.FloatTwo));
			if (p.health.hediffSet.HungerRateFactor != 1f)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("StatsReport_RelevantHediffs".Translate() + ": " + p.health.hediffSet.HungerRateFactor.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
				foreach (Hediff hediff in p.health.hediffSet.hediffs)
				{
					if (hediff.CurStage != null && hediff.CurStage.hungerRateFactor != 1f)
					{
						stringBuilder.AppendLine("    " + hediff.LabelCap + ": " + hediff.CurStage.hungerRateFactor.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
					}
				}
				foreach (Hediff hediff2 in p.health.hediffSet.hediffs)
				{
					if (hediff2.CurStage != null && hediff2.CurStage.hungerRateFactorOffset != 0f)
					{
						stringBuilder.AppendLine("    " + hediff2.LabelCap + ": " + hediff2.CurStage.hungerRateFactorOffset.ToStringByStyle(ToStringStyle.FloatMaxOne, ToStringNumberSense.Offset));
					}
				}
			}
			if (p.story?.traits != null && p.story.traits.HungerRateFactor != 1f)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("StatsReport_RelevantTraits".Translate() + ": " + p.story.traits.HungerRateFactor.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
				foreach (Trait allTrait in p.story.traits.allTraits)
				{
					if (!allTrait.Suppressed && allTrait.CurrentData.hungerRateFactor != 1f)
					{
						stringBuilder.AppendLine("    " + allTrait.LabelCap + ": " + allTrait.CurrentData.hungerRateFactor.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
					}
				}
			}
			Building_Bed building_Bed = p.CurrentBed() ?? p.CurrentCaravanBed();
			if (building_Bed != null)
			{
				float statValue = building_Bed.GetStatValue(StatDefOf.BedHungerRateFactor);
				if (statValue != 1f)
				{
					stringBuilder.AppendLine().AppendLine("StatsReport_InBed".Translate() + ": x" + statValue.ToStringPercent());
				}
			}
			Hediff firstHediffOfDef;
			HediffComp_Lactating hediffComp_Lactating;
			if (ModsConfig.BiotechActive && (firstHediffOfDef = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating)) != null && (hediffComp_Lactating = firstHediffOfDef.TryGetComp<HediffComp_Lactating>()) != null)
			{
				float f = hediffComp_Lactating.AddedNutritionPerDay();
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(firstHediffOfDef.LabelBaseCap + ": " + f.ToStringWithSign());
			}
			if (p.genes != null)
			{
				int num = 0;
				foreach (Gene item in p.genes.GenesListForReading)
				{
					if (!item.Overridden)
					{
						num += item.def.biostatMet;
					}
				}
				float num2 = GeneTuning.MetabolismToFoodConsumptionFactorCurve.Evaluate(num);
				if (num2 != 1f)
				{
					stringBuilder.AppendLine().AppendLine("FactorForMetabolism".Translate() + ": x" + num2.ToStringPercent());
				}
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("StatsReport_FinalValue".Translate() + ": " + NutritionEatenPerDay(p));
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}
}
