using System;
using System.Collections.Generic;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace Verse;

public class BackCompatibilityConverter_Universal : BackCompatibilityConverter
{
	private Dictionary<Building, ColorInt> lampsToColors = new Dictionary<Building, ColorInt>(128);

	public override bool AppliesToVersion(int majorVer, int minorVer)
	{
		return true;
	}

	public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
	{
		if (defType == typeof(ThingDef))
		{
			switch (defName)
			{
			case "WoolYak":
				return "WoolSheep";
			case "Plant_TreeAnimus":
			case "Plant_TreeAnimusSmall":
			case "Plant_TreeAnimaSmall":
			case "Plant_TreeAnimaNormal":
			case "Plant_TreeAnimaHardy":
				return "Plant_TreeAnima";
			case "Psytrainer_EntropyLink":
				return "Psytrainer_EntropyDump";
			case "PsylinkNeuroformer":
				return "PsychicAmplifier";
			case "PsychicShockLance":
				return "Apparel_PsychicShockLance";
			case "PsychicInsanityLance":
				return "Apparel_PsychicInsanityLance";
			case "Nutrifungus":
				return "Plant_Nutrifungus";
			case "Mech_Centipede":
				return "Mech_CentipedeBlaster";
			case "Corpse_Mech_Centipede":
				return "Corpse_Mech_CentipedeBlaster";
			case "AncientDiabolusRemains":
				return "AncientUltraDiabolusRemains";
			case "AncientUltraDiabolusRemains":
				return "AncientExostriderRemains";
			case "MegaspiderCocoon":
				return "CocoonMegaspider";
			case "MegascarabCocoon":
				return "CocoonMegascarab";
			case "SpelopedeCocoon":
				return "CocoonSpelopede";
			case "AncientCentipedeShell":
				return "ChunkMechanoidSlag";
			case "BasicSubcore":
				return "SubcoreBasic";
			case "RegularSubcore":
				return "SubcoreRegular";
			case "HighSubcore":
				return "SubcoreHigh";
			case "AncientMechanoidShell":
				return "ChunkMechanoidSlag";
			case "XenogermExtractor":
				return "GeneExtractor";
			case "Mech_Purger":
				return "Mech_Tunneler";
			case "RemoteCharger":
				return "MechBooster";
			case "StandingLamp_Red":
				return "StandingLamp";
			case "StandingLamp_Green":
				return "StandingLamp";
			case "StandingLamp_Blue":
				return "StandingLamp";
			case "Darklamp":
				return "StandingLamp";
			case "MechanitorComplexMap":
				return "MechanoidTransponder";
			}
		}
		if (defType == typeof(HediffDef))
		{
			if (defName == "Psylink")
			{
				return "PsychicAmplifier";
			}
			if (defName == "RemoteCharge")
			{
				return "MechBoost";
			}
		}
		if (defType == typeof(PreceptDef) && defName == "FuneralDestroyed")
		{
			return "FuneralNoCorpse";
		}
		if (defType == typeof(RitualOutcomeEffectDef) && defName == "AttendedFuneralDestroyed")
		{
			return "AttendedFuneralNoCorpse";
		}
		if (defType == typeof(AbilityDef))
		{
			if (defName == "PreachingOfHealing")
			{
				return "PreachHealth";
			}
			if (defName == "HeartenHealth")
			{
				return "PreachHealth";
			}
		}
		if (defType == typeof(IdeoIconDef))
		{
			switch (defName)
			{
			case "PoliticalA":
				return "Eagle";
			case "NatureA":
				return "Treeflat";
			case "PirateA":
				return "Steer";
			case "PirateB":
				return "Skull";
			case "PoliticalB":
				return "OliveBranches";
			case "ReligionA":
				return "DownBurst";
			case "ReligionB":
				return "TripleCross";
			}
		}
		if (defType == typeof(PawnKindDef))
		{
			if (defName == "Mech_Centipede")
			{
				return "Mech_CentipedeBlaster";
			}
			if (defName == "Mech_Purger")
			{
				return "Mech_Tunneler";
			}
		}
		if (defType == typeof(ThoughtDef))
		{
			if (defName == "AteFungus_Prefered")
			{
				return "AteFungus_Preferred";
			}
			if (defName == "AteFungusAsIngredient_Prefered")
			{
				return "AteFungusAsIngredient_Preferred";
			}
		}
		if (defType == typeof(JobDef))
		{
			if (defName == "StudyThing")
			{
				return "StudyInteract";
			}
			if (defName == "StudyBuilding")
			{
				return "StudyInteract";
			}
		}
		if (defType == typeof(ThingStyleDef))
		{
			if (defName.EndsWith("StandingLamp_Red") || defName.EndsWith("StandingLamp_Green") || defName.EndsWith("StandingLamp_Blue"))
			{
				return defName.Substring(0, defName.IndexOf("StandingLamp") + "StandingLamp".Length);
			}
			if (defName.EndsWith("DarklampStanding"))
			{
				return defName.Substring(0, defName.IndexOf("DarklampStanding")) + "StandingLamp";
			}
		}
		return null;
	}

	public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
	{
		if (providedClassName == "Hediff_PsychicAmplifier")
		{
			return typeof(Hediff_Psylink);
		}
		if (providedClassName == "Graphic_MotePulse")
		{
			return typeof(Graphic_MoteWithAgeSecs);
		}
		if (node != null && (providedClassName == "ThingWithComps" || providedClassName == "Verse.ThingWithComps"))
		{
			XmlElement xmlElement = node["def"];
			if (xmlElement != null)
			{
				if (xmlElement.InnerText == "PsychicShockLance")
				{
					return typeof(Apparel);
				}
				if (xmlElement.InnerText == "PsychicInsanityLance")
				{
					return typeof(Apparel);
				}
				if (xmlElement.InnerText == "OrbitalTargeterBombardment")
				{
					return typeof(Apparel);
				}
				if (xmlElement.InnerText == "OrbitalTargeterPowerBeam")
				{
					return typeof(Apparel);
				}
				if (xmlElement.InnerText == "OrbitalTargeterMechCluster")
				{
					return typeof(Apparel);
				}
				if (xmlElement.InnerText == "TornadoGenerator")
				{
					return typeof(Apparel);
				}
			}
		}
		if (providedClassName == "Building_AncientUltraDiabolusRemains")
		{
			return typeof(Building_AncientMechRemains);
		}
		return null;
	}

	public override void PostExposeData(object obj)
	{
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			int loadedGameVersionBuild = ScribeMetaHeaderUtility.loadedGameVersionBuild;
			if (obj is Pawn_RoyaltyTracker pawn_RoyaltyTracker && loadedGameVersionBuild <= 2575)
			{
				foreach (RoyalTitle item in pawn_RoyaltyTracker.AllTitlesForReading)
				{
					item.conceited = RoyalTitleUtility.ShouldBecomeConceitedOnNewTitle(pawn_RoyaltyTracker.pawn);
				}
			}
			if (loadedGameVersionBuild < 3167)
			{
				MealRestrictionsReworkBackCompat(obj);
			}
			if (loadedGameVersionBuild < 3156)
			{
				BiosculpterReworkBackCompat(obj);
			}
			ApplyLampColor(obj);
			if (obj is Pawn_NeedsTracker pawn_NeedsTracker)
			{
				pawn_NeedsTracker.AllNeeds.RemoveAll((Need n) => n.def.defName == "Authority");
			}
			if (obj is History { historyEventsManager: null } history)
			{
				history.historyEventsManager = new HistoryEventsManager();
			}
		}
		if (obj is Pawn pawn)
		{
			if (pawn.abilities == null && pawn.RaceProps.ShouldHaveAbilityTracker)
			{
				pawn.abilities = new Pawn_AbilityTracker(pawn);
			}
			Ability ability = pawn.abilities?.abilities.FirstOrFallback((Ability x) => x.def != null && x.def.defName == "AnimaTreeLinking");
			if (ability != null)
			{
				pawn.abilities.RemoveAbility(ability.def);
			}
			if (pawn.RaceProps.Humanlike)
			{
				if (pawn.surroundings == null)
				{
					pawn.surroundings = new Pawn_SurroundingsTracker(pawn);
				}
				if (ModsConfig.IdeologyActive)
				{
					if (pawn.styleObserver == null)
					{
						pawn.styleObserver = new Pawn_StyleObserverTracker(pawn);
					}
					if (pawn.connections == null)
					{
						pawn.connections = new Pawn_ConnectionsTracker(pawn);
					}
				}
			}
			if (pawn.health != null)
			{
				if (pawn.health.hediffSet.hediffs.RemoveAll((Hediff x) => x == null) != 0)
				{
					Log.Error(pawn.ToStringSafe() + " had some null hediffs.");
				}
				Hediff hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.PsychicHangover);
				if (hediff != null)
				{
					pawn.health.hediffSet.hediffs.Remove(hediff);
				}
				Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.WakeUpTolerance);
				if (firstHediffOfDef != null)
				{
					pawn.health.hediffSet.hediffs.Remove(firstHediffOfDef);
				}
				Hediff firstHediffOfDef2 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.GoJuiceTolerance);
				if (firstHediffOfDef2 != null)
				{
					pawn.health.hediffSet.hediffs.Remove(firstHediffOfDef2);
				}
				if (pawn.mechanitor != null)
				{
					Hediff firstHediffOfDef3 = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BandNode);
					if (firstHediffOfDef3 != null && !(firstHediffOfDef3 is Hediff_BandNode))
					{
						pawn.health.RemoveHediff(firstHediffOfDef3);
						pawn.health.AddHediff(HediffDefOf.BandNode, pawn.health.hediffSet.GetBrain(), null);
					}
				}
				if (!pawn.Dead)
				{
					if (pawn.thinker == null)
					{
						pawn.thinker = new Pawn_Thinker(pawn);
					}
					if (pawn.jobs == null)
					{
						pawn.jobs = new Pawn_JobTracker(pawn);
					}
					if (pawn.stances == null)
					{
						pawn.stances = new Pawn_StanceTracker(pawn);
					}
					if (ModsConfig.AnomalyActive && pawn.RaceProps.Humanlike && pawn.infectionVectors == null)
					{
						pawn.infectionVectors = new Pawn_InfectionVectorTracker(pawn);
					}
				}
			}
			if (pawn.equipment != null && pawn.apparel != null && pawn.inventory != null)
			{
				List<ThingWithComps> list = null;
				for (int i = 0; i < pawn.equipment.AllEquipmentListForReading.Count; i++)
				{
					ThingWithComps thingWithComps = pawn.equipment.AllEquipmentListForReading[i];
					if (thingWithComps.def.defName == "OrbitalTargeterBombardment" || thingWithComps.def.defName == "OrbitalTargeterPowerBeam" || thingWithComps.def.defName == "OrbitalTargeterMechCluster" || thingWithComps.def.defName == "TornadoGenerator")
					{
						list = list ?? new List<ThingWithComps>();
						list.Add(thingWithComps);
					}
				}
				if (list != null)
				{
					foreach (Apparel item2 in list)
					{
						pawn.equipment.Remove(item2);
						ResetVerbs(item2);
						if (pawn.apparel.CanWearWithoutDroppingAnything(item2.def))
						{
							pawn.apparel.Wear(item2);
						}
						else
						{
							pawn.inventory.innerContainer.TryAdd(item2);
						}
					}
				}
			}
			if (ModsConfig.BiotechActive && pawn.RaceProps.IsMechanoid && pawn.Faction == Faction.OfPlayer && pawn.Name == null && Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				pawn.GenerateNecessaryName();
			}
			if (!ModsConfig.BiotechActive || pawn.equipment == null || pawn.def != ThingDefOf.Mech_Constructoid)
			{
				return;
			}
			List<ThingWithComps> allEquipmentListForReading = pawn.equipment.AllEquipmentListForReading;
			for (int j = 0; j < allEquipmentListForReading.Count; j++)
			{
				if (allEquipmentListForReading[j].def.defName == "Gun_Slugthrower")
				{
					pawn.equipment.Remove(allEquipmentListForReading[j]);
					break;
				}
			}
		}
		else
		{
			if (Scribe.mode != LoadSaveMode.LoadingVars)
			{
				return;
			}
			SaveLampColor(obj);
			if (obj is Map map)
			{
				if (map.temporaryThingDrawer == null)
				{
					map.temporaryThingDrawer = new TemporaryThingDrawer();
				}
				if (map.flecks == null)
				{
					map.flecks = new FleckManager(map);
				}
				if (map.autoSlaughterManager == null)
				{
					map.autoSlaughterManager = new AutoSlaughterManager(map);
				}
				if (map.treeDestructionTracker == null)
				{
					map.treeDestructionTracker = new TreeDestructionTracker(map);
				}
				if (map.gasGrid == null)
				{
					map.gasGrid = new GasGrid(map);
				}
				if (ModsConfig.BiotechActive && map.pollutionGrid == null)
				{
					map.pollutionGrid = new PollutionGrid(map);
				}
				if (map.deferredSpawner == null)
				{
					map.deferredSpawner = new DeferredSpawner(map);
				}
				if (map.storageGroups == null)
				{
					map.storageGroups = new StorageGroupManager(map);
				}
				if (map.terrainGrid.colorGrid == null)
				{
					map.terrainGrid.colorGrid = new ColorDef[map.cellIndices.NumGridCells];
				}
			}
			else if (obj is Game game)
			{
				if (game.transportShipManager == null)
				{
					game.transportShipManager = new TransportShipManager();
				}
				if (game.studyManager == null)
				{
					game.studyManager = new StudyManager();
				}
				if (game.hiddenItemsManager == null)
				{
					game.hiddenItemsManager = new HiddenItemsManager();
				}
				if (ModsConfig.BiotechActive && game.customXenogermDatabase == null)
				{
					game.customXenogermDatabase = new CustomXenogermDatabase();
				}
				if (ModsConfig.BiotechActive && game.customXenotypeDatabase == null)
				{
					game.customXenotypeDatabase = new CustomXenotypeDatabase();
				}
			}
		}
	}

	private void ResetVerbs(ThingWithComps t)
	{
		(t as IVerbOwner)?.VerbTracker?.VerbsNeedReinitOnLoad();
		foreach (ThingComp allComp in t.AllComps)
		{
			(allComp as IVerbOwner)?.VerbTracker?.VerbsNeedReinitOnLoad();
		}
	}

	public override int GetBackCompatibleBodyPartIndex(BodyDef body, int index)
	{
		if (body == BodyDefOf.Human && ScribeMetaHeaderUtility.loadedGameVersionBuild <= 3094 && index >= 22)
		{
			return index + 1;
		}
		return index;
	}

	private void MealRestrictionsReworkBackCompat(object obj)
	{
		if (obj is FoodRestrictionDatabase foodRestrictionDatabase)
		{
			foodRestrictionDatabase.CreateIdeologyFoodRestrictions();
		}
	}

	private void BiosculpterReworkBackCompat(object obj)
	{
		if (obj is JobDriver_CarryToBiosculpterPod jobDriver_CarryToBiosculpterPod)
		{
			jobDriver_CarryToBiosculpterPod.EndJobWith(JobCondition.Incompletable);
		}
		if (obj is JobDriver_EnterBiosculpterPod jobDriver_EnterBiosculpterPod)
		{
			jobDriver_EnterBiosculpterPod.EndJobWith(JobCondition.Incompletable);
		}
		if (!(obj is Building thing))
		{
			return;
		}
		CompBiosculpterPod compBiosculpterPod = thing.TryGetComp<CompBiosculpterPod>();
		if (compBiosculpterPod != null)
		{
			if (compBiosculpterPod.Occupant == null)
			{
				compBiosculpterPod.ClearCycle();
			}
			compBiosculpterPod.autoLoadNutrition = true;
		}
	}

	private void SaveLampColor(object obj)
	{
		if (obj is Building key)
		{
			ColorInt? colorInt = null;
			switch (Scribe.loader?.curXmlParent?["def"]?.InnerText)
			{
			case "StandingLamp_Red":
				colorInt = new ColorInt(217, 80, 80);
				break;
			case "StandingLamp_Green":
				colorInt = new ColorInt(80, 217, 80);
				break;
			case "StandingLamp_Blue":
				colorInt = new ColorInt(80, 80, 217);
				break;
			case "Darklamp":
				colorInt = new ColorInt(78, 226, 229);
				break;
			}
			if (colorInt.HasValue)
			{
				lampsToColors[key] = colorInt.Value;
			}
		}
	}

	private void ApplyLampColor(object obj)
	{
		CompGlower comp;
		if (obj is Building building && (comp = building.GetComp<CompGlower>()) != null && lampsToColors.TryGetValue(building, out var value))
		{
			Color.RGBToHSV(value.ToColor, out var H, out var S, out var V);
			Color.RGBToHSV(comp.GlowColor.ToColor, out V, out var _, out var V2);
			comp.GlowColor = new ColorInt(Color.HSVToRGB(H, S, V2));
		}
	}

	public override void PreLoadSavegame(string loadingVersion)
	{
		lampsToColors.Clear();
	}
}
