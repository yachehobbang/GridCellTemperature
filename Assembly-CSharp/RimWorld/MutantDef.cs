using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class MutantDef : Def, IRenderNodePropertiesParent
{
	public HediffDef hediff;

	public ThinkTreeDef thinkTree;

	public ThinkTreeDef thinkTreeConstant;

	[MustTranslate]
	public string namePrefix = "";

	public bool showLabel;

	public bool canWearApparel = true;

	public bool canOpenDoors = true;

	public bool canOpenAnyDoor;

	public bool showJobReport;

	public WorkTags workDisables = WorkTags.Shooting | WorkTags.AllWork;

	public List<GeneDef> disablesGenes = new List<GeneDef>();

	public bool clearMutantStatusOnDeath;

	public bool canHaveMentalBreak;

	public bool canTravelInCaravan;

	public bool isConsideredCorpse;

	public bool canBeCaptured = true;

	public bool makesFootprints = true;

	public bool removeIdeo = true;

	public bool respectsAllowedArea;

	public DevelopmentalStage allowedDevelopmentalStages = DevelopmentalStage.Baby | DevelopmentalStage.Child | DevelopmentalStage.Adult;

	public EntityCodexEntryDef codexEntry;

	public bool canLearn;

	public ThoughtDef relativeTurnedThought;

	public bool shouldTickAge;

	public bool showInScenarioEditor = true;

	public bool clearsEgo = true;

	public List<NeedDef> enabledNeeds = new List<NeedDef>();

	public FoodTypeFlags foodType;

	public bool allowEatingCorpses;

	public bool useCorpseGraphics;

	public Color? skinColorOverride;

	public Color? hairColorOverride;

	public List<BodyTypeGraphicData> bodyTypeGraphicPaths = new List<BodyTypeGraphicData>();

	public List<HeadTypeDef> forcedHeadTypes;

	public TagFilter hairTagFilter;

	public TagFilter beardTagFilter;

	private List<PawnRenderNodeProperties> renderNodeProperties;

	public AnimationDef standingAnimation;

	public bool canBleed = true;

	public ThingDef bloodDef;

	public ThingDef bloodSmearDef;

	public bool entitledToMedicalCare = true;

	public bool isImmuneToInfections;

	public bool canUseDrugs;

	public List<ThingDef> drugWhitelist = new List<ThingDef>();

	public Color? woundColor;

	public bool preventIllnesses = true;

	public bool removeChronicIllnesses;

	public bool removePermanentInjuries;

	public bool removeAddictions;

	public bool removeAllInjuries;

	public bool restoreLegs;

	public bool terminatePregnancy = true;

	public List<StartingHediff> givesHediffs = new List<StartingHediff>();

	public List<HediffDef> removesHediffs = new List<HediffDef>();

	public List<HediffGiver> hediffGivers;

	[MustTranslate]
	public string deathLetter;

	[MustTranslate]
	public string deathLetterExtra;

	public float anomalyKnowledgeOffset;

	public KnowledgeCategoryDef knowledgeCategory;

	public SoundDef soundCall;

	public SoundDef soundAttack;

	public SoundDef soundWounded;

	public SoundDef soundDeath;

	public SoundDef soundAngry;

	public List<VerbProperties> verbs;

	public List<Tool> tools;

	public List<AbilityDef> abilities = new List<AbilityDef>();

	public List<AbilityDef> abilityWhitelist = new List<AbilityDef>();

	public bool passive;

	public bool canBeDrafted;

	public bool canAttackWhileCrawling;

	public float deathOnDownedChance = 0.25f;

	public float soundAttackChance = 0.25f;

	public List<ThingDefCountClass> killedLeavings;

	public bool psychicShockUntargetable;

	public bool hasHostilityResponse;

	public FactionDef defaultFaction;

	public bool producesBioferrite;

	private List<WorkTypeDef> cachedDisabledWorkTypes;

	public bool HasDefinedGraphicProperties => !renderNodeProperties.NullOrEmpty();

	public List<PawnRenderNodeProperties> RenderNodeProperties => renderNodeProperties;

	public List<WorkTypeDef> DisabledWorkTypes
	{
		get
		{
			if (cachedDisabledWorkTypes == null)
			{
				cachedDisabledWorkTypes = new List<WorkTypeDef>();
				List<WorkTypeDef> allDefsListForReading = DefDatabase<WorkTypeDef>.AllDefsListForReading;
				for (int i = 0; i < allDefsListForReading.Count; i++)
				{
					if (!AllowsWorkType(allDefsListForReading[i]))
					{
						cachedDisabledWorkTypes.Add(allDefsListForReading[i]);
					}
				}
			}
			return cachedDisabledWorkTypes;
		}
	}

	public override IEnumerable<string> ConfigErrors()
	{
		foreach (string item in base.ConfigErrors())
		{
			yield return item;
		}
		if (hediff == null)
		{
			yield return "mutant hediff is undefined";
		}
		if (thinkTree == null)
		{
			yield return "mutant think tree is undefined";
		}
	}

	public override void PostLoad()
	{
		base.PostLoad();
		if (tools != null)
		{
			for (int i = 0; i < tools.Count; i++)
			{
				tools[i].id = i.ToString();
			}
		}
	}

	public string GetBodyGraphicPath(Pawn pawn)
	{
		for (int i = 0; i < bodyTypeGraphicPaths.Count; i++)
		{
			if (bodyTypeGraphicPaths[i].bodyType == pawn.story.bodyType)
			{
				return bodyTypeGraphicPaths[i].texturePath;
			}
		}
		return null;
	}

	public bool StyleItemAllowed(StyleItemDef styleItem)
	{
		if (!ModLister.AnomalyInstalled)
		{
			return true;
		}
		bool flag = styleItem is HairDef;
		bool flag2 = styleItem is BeardDef;
		if (!flag && !flag2)
		{
			return true;
		}
		if (flag)
		{
			if (hairTagFilter != null && !hairTagFilter.Allows(styleItem.styleTags))
			{
				return false;
			}
		}
		else if (flag2 && beardTagFilter != null && !beardTagFilter.Allows(styleItem.styleTags))
		{
			return false;
		}
		return true;
	}

	private bool AllowsWorkType(WorkTypeDef workType)
	{
		return (workDisables & workType.workTags) == 0;
	}

	public override void ResolveReferences()
	{
		if (renderNodeProperties != null)
		{
			for (int i = 0; i < renderNodeProperties.Count; i++)
			{
				renderNodeProperties[i].ResolveReferencesRecursive();
			}
		}
	}
}
