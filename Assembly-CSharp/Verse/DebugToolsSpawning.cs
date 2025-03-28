using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse.AI.Group;

namespace Verse;

public static class DebugToolsSpawning
{
	private static readonly List<Def> excluded = new List<Def>();

	private static readonly List<Def> requires = new List<Def>();

	private static readonly float[] MarketValues = new float[3] { 1000f, 10000f, 100000f };

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> SpawnPawn()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (PawnKindDef item in from x in DefDatabase<PawnKindDef>.AllDefs
			where x.showInDebugSpawner
			select x into kd
			orderby kd.defName
			select kd)
		{
			PawnKindDef localKindDef = item;
			list.Add(new DebugActionNode(localKindDef.defName, DebugActionType.ToolMap)
			{
				category = GetCategoryForPawnKind(localKindDef),
				action = delegate
				{
					Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionType);
					Pawn pawn = PawnGenerator.GeneratePawn(localKindDef, faction);
					GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
					PostPawnSpawn(pawn);
				}
			});
		}
		return list;
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> SpawnPawnWithLifestage()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (PawnKindDef item in from x in DefDatabase<PawnKindDef>.AllDefs
			where x.showInDebugSpawner
			select x into kd
			orderby kd.defName
			select kd)
		{
			PawnKindDef localKindDef = item;
			DebugActionNode debugActionNode = new DebugActionNode(localKindDef.defName, DebugActionType.ToolMap)
			{
				category = GetCategoryForPawnKind(localKindDef)
			};
			for (int i = 0; i < localKindDef.RaceProps.lifeStageAges.Count; i++)
			{
				LifeStageAge raceStage = localKindDef.RaceProps.lifeStageAges[i];
				debugActionNode.AddChild(new DebugActionNode(raceStage.def.defName, DebugActionType.ToolMap)
				{
					action = delegate
					{
						Pawn pawn = PawnGenerator.GeneratePawn(localKindDef, FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionType));
						pawn.ageTracker.AgeBiologicalTicks = (long)(raceStage.minAge * 3600000f);
						GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
						PostPawnSpawn(pawn);
					}
				});
			}
			list.Add(debugActionNode);
		}
		return list;
	}

	[DebugAction("Anomaly", "Generate CreepJoiner...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
	private static void GenerateCreeperJoiner()
	{
		requires.Clear();
		excluded.Clear();
		Slate slate = new Slate();
		ShowOptions(slate, "form", DefDatabase<CreepJoinerFormKindDef>.AllDefs, requires, excluded, delegate
		{
			ShowOptions(slate, "benefit", DefDatabase<CreepJoinerBenefitDef>.AllDefs, requires, excluded, delegate
			{
				ShowOptions(slate, "downside", DefDatabase<CreepJoinerDownsideDef>.AllDefs, requires, excluded, delegate
				{
					ShowOptions(slate, "aggressive", DefDatabase<CreepJoinerAggressiveDef>.AllDefs, requires, excluded, delegate
					{
						ShowOptions(slate, "rejection", DefDatabase<CreepJoinerRejectionDef>.AllDefs, requires, excluded, delegate
						{
							Quest quest = QuestGen.Generate(QuestScriptDefOf.CreepJoinerArrival, slate);
							Find.QuestManager.Add(quest);
							requires.Clear();
							excluded.Clear();
						});
					});
				});
			});
		});
	}

	private static void ShowOptions<T>(Slate slate, string key, IEnumerable<T> defs, List<Def> requires, List<Def> excluded, Action selected) where T : Def, ICreepJoinerDef
	{
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		float num = StorytellerUtility.DefaultThreatPointsNow(Find.CurrentMap);
		foreach (T def in defs)
		{
			DebugMenuOption item = new DebugMenuOption(def.label, DebugMenuOptionMode.Action, delegate
			{
				if (!excluded.Contains(def))
				{
					slate.Set(key, def);
					excluded.AddRange(def.Excludes);
					requires.AddRange(def.Requires);
					selected?.Invoke();
				}
			});
			if (excluded.Contains(def))
			{
				item.label += " [NO]";
			}
			else if (num < def.MinCombatPoints)
			{
				item.label += " [PNTS LOW]";
			}
			else if (requires.Contains(def))
			{
				item.label += " [REQ]";
			}
			list.Add(item);
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list, "Choose " + key));
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000, requiresBiotech = true)]
	private static List<DebugActionNode> SpawnNewborn()
	{
		return SpawnAtDevelopmentalStages(DevelopmentalStage.Newborn);
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000, requiresBiotech = true)]
	private static List<DebugActionNode> SpawnChild()
	{
		return SpawnAtDevelopmentalStages(DevelopmentalStage.Child);
	}

	private static List<DebugActionNode> SpawnAtDevelopmentalStages(DevelopmentalStage stages)
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (PawnKindDef item in DefDatabase<PawnKindDef>.AllDefs.OrderBy((PawnKindDef kd) => kd.defName))
		{
			PawnKindDef localKindDef = item;
			list.Add(new DebugActionNode(localKindDef.defName, DebugActionType.ToolMap)
			{
				category = GetCategoryForPawnKind(localKindDef),
				action = delegate
				{
					Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionType);
					Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(localKindDef, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, stages, null, null, null));
					GenSpawn.Spawn(pawn, UI.MouseCell(), Find.CurrentMap);
					PostPawnSpawn(pawn);
				}
			});
		}
		return list;
	}

	public static string GetCategoryForPawnKind(PawnKindDef kindDef)
	{
		if (!kindDef.overrideDebugActionCategory.NullOrEmpty())
		{
			return kindDef.overrideDebugActionCategory;
		}
		if (kindDef.RaceProps.Humanlike)
		{
			return "Humanlike";
		}
		if (kindDef.RaceProps.Insect)
		{
			return "Insect";
		}
		if (kindDef.RaceProps.IsMechanoid)
		{
			return "Mechanoid";
		}
		if (kindDef.RaceProps.Animal)
		{
			return "Animal";
		}
		return "Other";
	}

	private static void PostPawnSpawn(Pawn pawn)
	{
		if (pawn.Spawned && pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
		{
			Lord lord = null;
			if (pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Any((Pawn p) => p != pawn))
			{
				lord = ((Pawn)GenClosest.ClosestThing_Global(pawn.Position, pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction), 99999f, (Thing p) => p != pawn && ((Pawn)p).GetLord() != null)).GetLord();
			}
			if (lord == null || !lord.CanAddPawn(pawn))
			{
				lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_DefendPoint(pawn.Position, null, null), Find.CurrentMap);
			}
			if (lord != null && lord.LordJob.CanAutoAddPawns)
			{
				lord.AddPawn(pawn);
			}
		}
		pawn.Rotation = Rot4.South;
	}

	[DebugAction("Spawning", "Spawn thing", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> TryPlaceNearThing()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsForStackCount(1, direct: false)
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", "Spawn thing with style", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> TryPlaceNearThingWithStyle()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.OrderBy((ThingDef d) => d.defName))
		{
			ThingDef localDef = item;
			if (localDef.randomStyle.NullOrEmpty() && !DefDatabase<StyleCategoryDef>.AllDefs.Any((StyleCategoryDef s) => s.GetStyleForThingDef(localDef) != null))
			{
				continue;
			}
			DebugActionNode debugActionNode = new DebugActionNode(localDef.LabelCap);
			debugActionNode.AddChild(new DebugActionNode("Standard", DebugActionType.ToolMap, delegate
			{
				DebugThingPlaceHelper.DebugSpawn(localDef, UI.MouseCell(), -1, direct: false, null, canBeMinified: true, null);
			}));
			foreach (StyleCategoryDef cat in DefDatabase<StyleCategoryDef>.AllDefs.Where((StyleCategoryDef x) => x.thingDefStyles.Any((ThingDefStyle y) => y.ThingDef == localDef)))
			{
				StyleCategoryDef styleCategoryDef = cat;
				debugActionNode.AddChild(new DebugActionNode(styleCategoryDef.defName, DebugActionType.ToolMap, delegate
				{
					DebugThingPlaceHelper.DebugSpawn(localDef, UI.MouseCell(), -1, direct: false, cat.GetStyleForThingDef(localDef), canBeMinified: true, null);
				}));
			}
			if (localDef.randomStyle != null)
			{
				foreach (ThingStyleChance item2 in localDef.randomStyle)
				{
					ThingStyleChance style = item2;
					debugActionNode.AddChild(new DebugActionNode(style.StyleDef.defName, DebugActionType.ToolMap, delegate
					{
						DebugThingPlaceHelper.DebugSpawn(localDef, UI.MouseCell(), -1, direct: false, style.StyleDef, canBeMinified: true, null);
					}));
				}
			}
			list.Add(debugActionNode);
		}
		if (list.Count == 0)
		{
			list.Add(new DebugActionNode("No styleable things", DebugActionType.Action, delegate
			{
			}));
		}
		return list;
	}

	[DebugAction("Spawning", "Spawn unminified thing", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> TryPlaceMinifiedThing()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsUnminified()
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", "Spawn full thing stack", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> TryPlaceNearFullStack()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsForStackCount(-1, direct: false)
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", "Spawn stack of 25", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static List<DebugActionNode> TryPlaceNearStacksOf25()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsForStackCount(25, direct: false)
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", "Spawn stack of 75", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static List<DebugActionNode> TryPlaceNearStacksOf75()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsForStackCount(75, direct: false)
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", "Try place direct thing", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static List<DebugActionNode> TryPlaceDirectThing()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsForStackCount(1, direct: true)
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", "Try place direct full stack", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static List<DebugActionNode> TryPlaceDirectFullStack()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsForStackCount(-1, direct: true)
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", "Try place direct stack of 25", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static List<DebugActionNode> TryPlaceDirectStackOf25()
	{
		return (from x in DebugThingPlaceHelper.TryPlaceOptionsForStackCount(25, direct: true)
			orderby x.label
			select x).ToList();
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> SpawnWeapon()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (ThingDef item in from def in DefDatabase<ThingDef>.AllDefs
			where def.equipmentType == EquipmentType.Primary
			select def into d
			orderby d.defName
			select d)
		{
			ThingDef localDef = item;
			list.Add(new DebugActionNode(localDef.defName, DebugActionType.ToolMap, delegate
			{
				DebugThingPlaceHelper.DebugSpawn(localDef, UI.MouseCell(), -1, direct: false, null, canBeMinified: true, null);
			}));
		}
		return list;
	}

	[DebugAction("Spawning", "Spawn apparel", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> SpawnApparel()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (ThingDef item in from def in DefDatabase<ThingDef>.AllDefs
			where def.IsApparel
			select def into d
			orderby d.defName
			select d)
		{
			ThingDef localDef = item;
			list.Add(new DebugActionNode(localDef.defName, DebugActionType.ToolMap, delegate
			{
				DebugThingPlaceHelper.DebugSpawn(localDef, UI.MouseCell(), -1, direct: false, null, canBeMinified: true, null);
			}));
		}
		return list;
	}

	[DebugAction("Spawning", "Try spawn stack of market value...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> TryPlaceNearMarketValue()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		float[] marketValues = MarketValues;
		foreach (float num in marketValues)
		{
			DebugActionNode debugActionNode = new DebugActionNode(num.ToStringMoney());
			foreach (DebugActionNode item in DebugThingPlaceHelper.TryPlaceOptionsForBaseMarketValue(num, direct: false))
			{
				debugActionNode.AddChild(item);
			}
			list.Add(debugActionNode);
		}
		return list;
	}

	[DebugAction("Spawning", "Spawn meal with specifics...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void CreateMealWithSpecifics()
	{
		IEnumerable<ThingDef> enumerable = DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.IsNutritionGivingIngestible && x.ingestible.IsMeal);
		IEnumerable<ThingDef> ingredientDefs = DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.IsNutritionGivingIngestible && x.ingestible.HumanEdible && !x.ingestible.IsMeal && !x.IsCorpse);
		ThingDef mealDef = null;
		List<ThingDef> ingredients = new List<ThingDef>();
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		foreach (ThingDef d in enumerable)
		{
			list.Add(new DebugMenuOption(d.defName, DebugMenuOptionMode.Action, delegate
			{
				mealDef = d;
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(GetIngredientOptions()));
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		IEnumerable<DebugMenuOption> GetIngredientOptions()
		{
			yield return new DebugMenuOption("[Finish and place " + ingredients.Count + " / " + 3 + "]", DebugMenuOptionMode.Tool, delegate
			{
				Thing thing = ThingMaker.MakeThing(mealDef);
				CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
				for (int i = 0; i < ingredients.Count; i++)
				{
					compIngredients.RegisterIngredient(ingredients[i]);
				}
				GenPlace.TryPlaceThing(thing, UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
			});
			yield return new DebugMenuOption("[Clear selections]", DebugMenuOptionMode.Action, delegate
			{
				ingredients.Clear();
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(GetIngredientOptions()));
			});
			foreach (ThingDef item in ingredientDefs)
			{
				ThingDef ingredient = item;
				if (ingredients.Count < 3 && FoodUtility.MealCanBeMadeFrom(mealDef, ingredient))
				{
					string label = (ingredients.Contains(ingredient) ? (ingredient.defName + " ✓") : ingredient.defName);
					yield return new DebugMenuOption(label, DebugMenuOptionMode.Action, delegate
					{
						if (ingredients.Contains(ingredient))
						{
							ingredients.Remove(ingredient);
						}
						else
						{
							ingredients.Add(ingredient);
						}
						Find.WindowStack.Add(new Dialog_DebugOptionListLister(GetIngredientOptions()));
					});
				}
			}
		}
	}

	[DebugAction("Spawning", "Spawn thing with wipe mode", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static DebugActionNode SpawnThingWithWipeMode()
	{
		DebugActionNode debugActionNode = new DebugActionNode();
		WipeMode[] array = (WipeMode[])Enum.GetValues(typeof(WipeMode));
		for (int i = 0; i < array.Length; i++)
		{
			WipeMode wipeMode = array[i];
			WipeMode localWipeMode = wipeMode;
			DebugActionNode debugActionNode2 = new DebugActionNode(wipeMode.ToString());
			debugActionNode2.childGetter = () => DebugThingPlaceHelper.SpawnOptions(localWipeMode);
			debugActionNode.AddChild(debugActionNode2);
		}
		return debugActionNode;
	}

	private static IEnumerable<float> PointsMechCluster()
	{
		for (float points = 50f; points <= 10000f; points += 50f)
		{
			yield return points;
		}
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, requiresRoyalty = true)]
	private static DebugActionNode SpawnMechCluster()
	{
		DebugActionNode debugActionNode = new DebugActionNode();
		foreach (float item in PointsMechCluster())
		{
			float localPoints = item;
			DebugActionNode debugActionNode2 = new DebugActionNode(localPoints + " points");
			debugActionNode2.AddChild(new DebugActionNode("In pods, click place", DebugActionType.ToolMap, delegate
			{
				MechClusterSketch sketch = MechClusterGenerator.GenerateClusterSketch(localPoints, Find.CurrentMap);
				MechClusterUtility.SpawnCluster(UI.MouseCell(), Find.CurrentMap, sketch);
			}));
			debugActionNode2.AddChild(new DebugActionNode("In pods, autoplace", DebugActionType.ToolMap, delegate
			{
				MechClusterSketch sketch2 = MechClusterGenerator.GenerateClusterSketch(localPoints, Find.CurrentMap);
				MechClusterUtility.SpawnCluster(MechClusterUtility.FindClusterPosition(Find.CurrentMap, sketch2), Find.CurrentMap, sketch2);
			}));
			debugActionNode2.AddChild(new DebugActionNode("Direct spawn, click place", DebugActionType.ToolMap, delegate
			{
				MechClusterSketch sketch3 = MechClusterGenerator.GenerateClusterSketch(localPoints, Find.CurrentMap);
				MechClusterUtility.SpawnCluster(UI.MouseCell(), Find.CurrentMap, sketch3, dropInPods: false);
			}));
			debugActionNode2.AddChild(new DebugActionNode("Direct spawn, autoplace", DebugActionType.Action, delegate
			{
				MechClusterSketch sketch4 = MechClusterGenerator.GenerateClusterSketch(localPoints, Find.CurrentMap);
				MechClusterUtility.SpawnCluster(MechClusterUtility.FindClusterPosition(Find.CurrentMap, sketch4), Find.CurrentMap, sketch4, dropInPods: false);
			}));
			debugActionNode.AddChild(debugActionNode2);
		}
		debugActionNode.visibilityGetter = () => Faction.OfMechanoids != null;
		return debugActionNode;
	}

	[DebugAction("Spawning", "Make filth x100", false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static void MakeFilthx100()
	{
		for (int i = 0; i < 100; i++)
		{
			IntVec3 c = UI.MouseCell() + GenRadial.RadialPattern[i];
			if (c.InBounds(Find.CurrentMap) && c.WalkableByAny(Find.CurrentMap))
			{
				FilthMaker.TryMakeFilth(c, Find.CurrentMap, ThingDefOf.Filth_Dirt, 2);
				FleckMaker.ThrowMetaPuff(c.ToVector3Shifted(), Find.CurrentMap);
			}
		}
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void SpawnFactionLeader()
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (Faction allFaction in Find.FactionManager.AllFactions)
		{
			Faction localFac = allFaction;
			if (localFac.leader != null)
			{
				list.Add(new FloatMenuOption(localFac.Name + " - " + localFac.leader.Name.ToStringFull, delegate
				{
					GenSpawn.Spawn(localFac.leader, UI.MouseCell(), Find.CurrentMap);
				}));
			}
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	[DebugAction("Spawning", "Spawn world pawn...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void SpawnWorldPawn()
	{
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		Action<Pawn> act = delegate(Pawn p)
		{
			List<DebugMenuOption> list2 = new List<DebugMenuOption>();
			foreach (PawnKindDef item in DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef x) => x.race == p.def))
			{
				PawnKindDef kLocal = item;
				list2.Add(new DebugMenuOption(kLocal.defName, DebugMenuOptionMode.Tool, delegate
				{
					PawnGenerationRequest request = new PawnGenerationRequest(kLocal, p.Faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null);
					PawnGenerator.RedressPawn(p, request);
					GenSpawn.Spawn(p, UI.MouseCell(), Find.CurrentMap);
					DebugTools.curTool = null;
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
		};
		foreach (Pawn item2 in Find.WorldPawns.AllPawnsAlive)
		{
			Pawn pLocal = item2;
			list.Add(new DebugMenuOption(item2.LabelShort, DebugMenuOptionMode.Action, delegate
			{
				act(pLocal);
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	[DebugAction("Spawning", "Spawn thing set", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> SpawnThingSet()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (ThingSetMakerDef allDef in DefDatabase<ThingSetMakerDef>.AllDefs)
		{
			ThingSetMakerDef localGen = allDef;
			list.Add(new DebugActionNode(localGen.defName, DebugActionType.ToolMap, delegate
			{
				if (UI.MouseCell().InBounds(Find.CurrentMap))
				{
					StringBuilder stringBuilder = new StringBuilder();
					string nonNullFieldsDebugInfo = Gen.GetNonNullFieldsDebugInfo(localGen.debugParams);
					List<Thing> list2 = localGen.root.Generate(localGen.debugParams);
					stringBuilder.Append(localGen.defName + " generated " + list2.Count + " things");
					if (!nonNullFieldsDebugInfo.NullOrEmpty())
					{
						stringBuilder.Append(" (used custom debug params: " + nonNullFieldsDebugInfo + ")");
					}
					stringBuilder.AppendLine(":");
					float num = 0f;
					float num2 = 0f;
					for (int i = 0; i < list2.Count; i++)
					{
						stringBuilder.AppendLine("   - " + list2[i].LabelCap);
						num += list2[i].MarketValue * (float)list2[i].stackCount;
						if (!(list2[i] is Pawn))
						{
							num2 += list2[i].GetStatValue(StatDefOf.Mass) * (float)list2[i].stackCount;
						}
						if (!GenPlace.TryPlaceThing(list2[i], UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near))
						{
							list2[i].Destroy();
						}
					}
					stringBuilder.AppendLine("Total market value: " + num.ToString("0.##"));
					stringBuilder.AppendLine("Total mass: " + num2.ToStringMass());
					Log.Message(stringBuilder.ToString());
				}
			}));
		}
		return list;
	}

	[DebugAction("Spawning", "Trigger effecter...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> TriggerEffecter()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (EffecterDef allDef in DefDatabase<EffecterDef>.AllDefs)
		{
			EffecterDef localDef = allDef;
			list.Add(new DebugActionNode(localDef.defName, DebugActionType.ToolMap, delegate
			{
				Effecter effecter = localDef.Spawn();
				effecter.Trigger(new TargetInfo(UI.MouseCell(), Find.CurrentMap), new TargetInfo(UI.MouseCell(), Find.CurrentMap));
				effecter.Cleanup();
			}));
		}
		return list;
	}

	[DebugAction("Spawning", "Trigger Maintained Effecter...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> TriggerMaintainedEffecter()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (EffecterDef allDef in DefDatabase<EffecterDef>.AllDefs)
		{
			EffecterDef localDef = allDef;
			list.Add(new DebugActionNode(localDef.defName, DebugActionType.ToolMap, delegate
			{
				TargetInfo targetInfo = new TargetInfo(UI.MouseCell(), Find.CurrentMap);
				TargetInfo b = targetInfo;
				localDef.SpawnMaintained(targetInfo, b);
			}));
		}
		return list;
	}

	[DebugAction("Spawning", "Trigger Maintained Effecter (5s)...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> TriggerMaintainedEffecter5S()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (EffecterDef allDef in DefDatabase<EffecterDef>.AllDefs)
		{
			EffecterDef localDef = allDef;
			list.Add(new DebugActionNode(localDef.defName, DebugActionType.ToolMap, delegate
			{
				Effecter effecter = localDef.Spawn();
				IntVec3 cell = UI.MouseCell();
				TargetInfo targetInfo = new TargetInfo(cell, Find.CurrentMap);
				TargetInfo b = targetInfo;
				effecter.Trigger(targetInfo, b);
				Find.CurrentMap.effecterMaintainer.AddEffecterToMaintain(effecter, targetInfo, b, 300);
			}));
		}
		return list;
	}

	[DebugAction("Spawning", "Trigger Maintained Effecter (12s)...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> TriggerMaintainedEffecter12S()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (EffecterDef allDef in DefDatabase<EffecterDef>.AllDefs)
		{
			EffecterDef localDef = allDef;
			list.Add(new DebugActionNode(localDef.defName, DebugActionType.ToolMap, delegate
			{
				Effecter effecter = localDef.Spawn();
				IntVec3 cell = UI.MouseCell();
				TargetInfo targetInfo = new TargetInfo(cell, Find.CurrentMap);
				TargetInfo b = targetInfo;
				effecter.Trigger(targetInfo, b);
				Find.CurrentMap.effecterMaintainer.AddEffecterToMaintain(effecter, targetInfo, b, 720);
			}));
		}
		return list;
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static DebugActionNode SpawnShuttle()
	{
		DebugActionNode debugActionNode = new DebugActionNode();
		debugActionNode.AddChild(new DebugActionNode("Incoming", DebugActionType.ToolMap, delegate
		{
			GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(ThingDefOf.ShuttleIncoming, ThingMaker.MakeThing(ThingDefOf.Shuttle)), UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
		}));
		debugActionNode.AddChild(new DebugActionNode("Crashing", DebugActionType.ToolMap, delegate
		{
			GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(ThingDefOf.ShuttleCrashing, ThingMaker.MakeThing(ThingDefOf.ShuttleCrashed)), UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
		}));
		debugActionNode.AddChild(new DebugActionNode("Stationary", DebugActionType.ToolMap, delegate
		{
			GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.Shuttle), UI.MouseCell(), Find.CurrentMap, ThingPlaceMode.Near);
		}));
		return debugActionNode;
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void SpawnRandomCaravan()
	{
		int num = GenWorld.MouseTile();
		if (Find.WorldGrid[num].biome.impassable)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		int num2 = Rand.RangeInclusive(1, 10);
		for (int i = 0; i < num2; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(Faction.OfPlayer.def.basicMemberKind, Faction.OfPlayer);
			list.Add(pawn);
			if (!pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				ThingDef thingDef = DefDatabase<ThingDef>.AllDefs.Where((ThingDef def) => def.IsWeapon && !def.weaponTags.NullOrEmpty() && (def.weaponTags.Contains("SimpleGun") || def.weaponTags.Contains("IndustrialGunAdvanced") || def.weaponTags.Contains("SpacerGun") || def.weaponTags.Contains("MedievalMeleeAdvanced") || def.weaponTags.Contains("NeolithicRangedBasic") || def.weaponTags.Contains("NeolithicRangedDecent") || def.weaponTags.Contains("NeolithicRangedHeavy"))).RandomElementWithFallback();
				pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(thingDef, GenStuff.RandomStuffFor(thingDef)));
			}
		}
		int num3 = Rand.RangeInclusive(-4, 10);
		for (int j = 0; j < num3; j++)
		{
			Pawn item = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef d) => d.RaceProps.Animal && d.RaceProps.wildness < 1f).RandomElement(), Faction.OfPlayer);
			list.Add(item);
		}
		Caravan caravan = CaravanMaker.MakeCaravan(list, Faction.OfPlayer, num, addToWorldPawnsIfNotAlready: true);
		List<Thing> list2 = ThingSetMakerDefOf.DebugCaravanInventory.root.Generate();
		for (int k = 0; k < list2.Count; k++)
		{
			Thing thing = list2[k];
			if (!(thing.GetStatValue(StatDefOf.Mass) * (float)thing.stackCount > caravan.MassCapacity - caravan.MassUsage))
			{
				CaravanInventoryUtility.GiveThing(caravan, thing);
				continue;
			}
			break;
		}
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void SpawnRandomFactionBase()
	{
		if (Find.FactionManager.AllFactions.Where((Faction x) => !x.IsPlayer && !x.Hidden).TryRandomElement(out var result))
		{
			int num = GenWorld.MouseTile();
			if (!Find.WorldGrid[num].biome.impassable)
			{
				Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
				settlement.SetFaction(result);
				settlement.Tile = num;
				settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
				Find.WorldObjects.Add(settlement);
			}
		}
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void SpawnSite()
	{
		int tile = GenWorld.MouseTile();
		if (tile < 0 || Find.World.Impassable(tile))
		{
			Messages.Message("Impassable", MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		List<SitePartDef> parts = new List<SitePartDef>();
		Action addPart = null;
		addPart = delegate
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>
			{
				new DebugMenuOption("-Done (" + parts.Count + " parts)-", DebugMenuOptionMode.Action, delegate
				{
					Site site = SiteMaker.TryMakeSite(parts, tile, disallowNonHostileFactions: true, null, ifHostileThenMustRemainHostile: true, null);
					if (site == null)
					{
						Messages.Message("Could not find any valid faction for this site.", MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						Find.WorldObjects.Add(site);
					}
				})
			};
			foreach (SitePartDef allDef in DefDatabase<SitePartDef>.AllDefs)
			{
				SitePartDef localPart = allDef;
				list.Add(new DebugMenuOption(allDef.defName, DebugMenuOptionMode.Action, delegate
				{
					parts.Add(localPart);
					addPart();
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};
		addPart();
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void DestroySite()
	{
		int tileID = GenWorld.MouseTile();
		foreach (WorldObject item in Find.WorldObjects.ObjectsAt(tileID).ToList())
		{
			item.Destroy();
		}
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static void SpawnSiteWithPoints()
	{
		int tile = GenWorld.MouseTile();
		if (tile < 0 || Find.World.Impassable(tile))
		{
			Messages.Message("Impassable", MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		List<SitePartDef> parts = new List<SitePartDef>();
		Action addPart = null;
		addPart = delegate
		{
			List<DebugMenuOption> list2 = new List<DebugMenuOption>
			{
				new DebugMenuOption("-Done (" + parts.Count + " parts)-", DebugMenuOptionMode.Action, delegate
				{
					List<DebugMenuOption> list = new List<DebugMenuOption>();
					foreach (float item in DebugActionsUtility.PointsOptions(extended: true))
					{
						float localPoints = item;
						list.Add(new DebugMenuOption(item.ToString("F0"), DebugMenuOptionMode.Action, delegate
						{
							Site site = SiteMaker.TryMakeSite(parts, tile, disallowNonHostileFactions: true, null, ifHostileThenMustRemainHostile: true, localPoints);
							if (site == null)
							{
								Messages.Message("Could not find any valid faction for this site.", MessageTypeDefOf.RejectInput, historical: false);
							}
							else
							{
								Find.WorldObjects.Add(site);
							}
						}));
					}
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
				})
			};
			foreach (SitePartDef allDef in DefDatabase<SitePartDef>.AllDefs)
			{
				SitePartDef localPart = allDef;
				list2.Add(new DebugMenuOption(allDef.defName, DebugMenuOptionMode.Action, delegate
				{
					parts.Add(localPart);
					addPart();
				}));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
		};
		addPart();
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static List<DebugActionNode> SpawnWorldObject()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (WorldObjectDef allDef in DefDatabase<WorldObjectDef>.AllDefs)
		{
			WorldObjectDef localDef = allDef;
			list.Add(new DebugActionNode(localDef.defName, DebugActionType.ToolWorld, delegate
			{
				int num = GenWorld.MouseTile();
				if (num < 0 || Find.World.Impassable(num))
				{
					Messages.Message("Impassable", MessageTypeDefOf.RejectInput, historical: false);
				}
				else
				{
					WorldObject worldObject = WorldObjectMaker.MakeWorldObject(localDef);
					worldObject.Tile = num;
					Find.WorldObjects.Add(worldObject);
				}
			}));
		}
		return list;
	}

	[DebugAction("General", "Change camera config", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	private static List<DebugActionNode> ChangeCameraConfigWorld()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (Type item in typeof(WorldCameraConfig).AllSubclasses())
		{
			Type localType = item;
			string text = localType.Name;
			if (text.StartsWith("WorldCameraConfig_"))
			{
				text = text.Substring("WorldCameraConfig_".Length);
			}
			list.Add(new DebugActionNode(text, DebugActionType.Action, delegate
			{
				Find.WorldCameraDriver.config = (WorldCameraConfig)Activator.CreateInstance(localType);
			}));
		}
		return list;
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, requiresBiotech = true)]
	private static List<DebugActionNode> SpawnBossgroup()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (BossgroupDef def in DefDatabase<BossgroupDef>.AllDefs)
		{
			BossgroupDef localDef = def;
			string text = localDef.defName;
			if (!PawnsFinder.AllMaps_FreeColonists.TryRandomElement(out var caller) || !localDef.Worker.CanResolve(caller))
			{
				text += " [NO]";
			}
			DebugActionNode debugActionNode = new DebugActionNode(text);
			debugActionNode.childGetter = delegate
			{
				List<DebugActionNode> list2 = new List<DebugActionNode>();
				int currentWave = 0;
				GameComponent_Bossgroup component = Current.Game.GetComponent<GameComponent_Bossgroup>();
				if (component != null)
				{
					currentWave = component.NumTimesCalledBossgroup(def);
				}
				list2.Add(new DebugActionNode("*Current (times called: " + currentWave + ")", DebugActionType.Action, delegate
				{
					if (caller == null)
					{
						Messages.Message("No colonist found to call bossgroup.", MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						localDef.Worker.Resolve(caller.Map, currentWave);
					}
				}));
				for (int i = 0; i < def.waves.Count; i++)
				{
					int index = i;
					list2.Add(new DebugActionNode("Wave " + index, DebugActionType.Action, delegate
					{
						if (caller == null)
						{
							Messages.Message("No colonist found to call bossgroup.", MessageTypeDefOf.RejectInput, historical: false);
						}
						else
						{
							localDef.Worker.Resolve(caller.Map, index);
						}
					}));
				}
				return list2;
			};
			list.Add(debugActionNode);
		}
		return list;
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
	public static List<DebugActionNode> AbandonThing()
	{
		return (from t in DebugThingPlaceHelper.TryAbandonOptionsForStackCount()
			orderby t.label
			select t).ToList();
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void TestDeferredSpawner()
	{
		IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
		incidentParms.forced = true;
		incidentParms.points = 200f;
		incidentParms.faction = Faction.OfHoraxCult;
		List<Pawn> list = new List<Pawn>(PawnGroupMakerUtility.GeneratePawns(IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, incidentParms)));
		List<IntVec3> list2 = new List<IntVec3>();
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(CellFinder.RandomSpawnCellForPawnNear(Find.CurrentMap.Center, Find.CurrentMap));
		}
		SpawnRequest spawnRequest = new SpawnRequest(list.Cast<Thing>().ToList(), list2, 1, 2f);
		spawnRequest.initialDelay = 500;
		spawnRequest.preSpawnEffecterOffsetTicks = -120;
		spawnRequest.preSpawnEffect = EffecterDefOf.WaterMist;
		Find.CurrentMap.deferredSpawner.AddRequest(spawnRequest);
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
	private static void CreateColonistDuplicate()
	{
		Map currentMap = Find.CurrentMap;
		GenSpawn.Spawn(Find.PawnDuplicator.Duplicate(currentMap.mapPawns.FreeColonistsSpawned[0]), CellFinder.RandomSpawnCellForPawnNear(currentMap.Center, currentMap), currentMap);
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
	private static void Duplicate()
	{
		Map currentMap = Find.CurrentMap;
		foreach (Thing item in currentMap.thingGrid.ThingsAt(UI.MouseCell()).ToList())
		{
			if (item is Pawn pawn && pawn.RaceProps.Humanlike)
			{
				GenSpawn.Spawn(Find.PawnDuplicator.Duplicate(pawn), CellFinder.RandomSpawnCellForPawnNear(pawn.PositionHeld, currentMap), currentMap);
			}
		}
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> WaterEmergePawn()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (PawnKindDef item in from x in DefDatabase<PawnKindDef>.AllDefs
			where x.showInDebugSpawner
			select x into kd
			orderby kd.defName
			select kd)
		{
			PawnKindDef localKindDef = item;
			DebugActionNode debugActionNode = new DebugActionNode(localKindDef.defName, DebugActionType.ToolMap)
			{
				category = GetCategoryForPawnKind(localKindDef)
			};
			for (int i = 1; i <= 3; i++)
			{
				int delaySeconds = i;
				debugActionNode.AddChild(new DebugActionNode($"{i}s delay", DebugActionType.ToolMap)
				{
					action = delegate
					{
						Faction faction = FactionUtility.DefaultFactionFrom(localKindDef.defaultFactionType);
						DelayedEffecterSpawner.Spawn(PawnGenerator.GeneratePawn(localKindDef, faction), delayTicks: delaySeconds * 60, pos: UI.MouseCell(), map: Find.CurrentMap, emergeEffect: EffecterDefOf.PawnEmergeFromWaterLarge, preEmergeEffect: EffecterDefOf.WaterMist, emergeSound: SoundDefOf.EmergeFromWater);
					}
				});
			}
			list.Add(debugActionNode);
		}
		return list;
	}

	[DebugAction("Spawning", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000, requiresAnomaly = true)]
	private static List<DebugActionNode> SpawnFlare()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		int[] array = new int[7] { 1, 2, 3, 4, 5, 15, 30 };
		foreach (int delaySeconds in array)
		{
			list.Add(new DebugActionNode($"{delaySeconds}s duration", DebugActionType.ToolMap)
			{
				action = delegate
				{
					int num = delaySeconds * 60;
					ThingWithComps obj = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.DisruptorFlare);
					GenSpawn.Spawn(obj, UI.MouseCell(), Find.CurrentMap);
					CompDestroyAfterDelay comp = obj.GetComp<CompDestroyAfterDelay>();
					comp.spawnTick = GenTicks.TicksGame + num - comp.Props.delayTicks;
					EffecterDefOf.DisruptorFlareLanded.Spawn(UI.MouseCell(), Find.CurrentMap).Cleanup();
				}
			});
		}
		return list;
	}
}
