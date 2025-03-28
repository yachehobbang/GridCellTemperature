using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.SketchGen;
using UnityEngine;

namespace Verse;

public static class DebugActionsMapManagement
{
	private static Map mapLeak;

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> UseScatterer()
	{
		return DebugTools_MapGen.Options_Scatterers();
	}

	[DebugAction("Map", "BaseGen", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> BaseGen()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (string item in DefDatabase<RuleDef>.AllDefs.Select((RuleDef x) => x.symbol).Distinct())
		{
			string localSymbol = item;
			list.Add(new DebugActionNode(localSymbol)
			{
				action = delegate
				{
					DebugTool tool = null;
					IntVec3 firstCorner;
					tool = new DebugTool("first corner...", delegate
					{
						firstCorner = UI.MouseCell();
						DebugTools.curTool = new DebugTool("second corner...", delegate
						{
							IntVec3 second = UI.MouseCell();
							CellRect rect = CellRect.FromLimits(firstCorner, second).ClipInsideMap(Find.CurrentMap);
							RimWorld.BaseGen.BaseGen.globalSettings.map = Find.CurrentMap;
							RimWorld.BaseGen.BaseGen.symbolStack.Push(localSymbol, rect);
							RimWorld.BaseGen.BaseGen.Generate();
							DebugTools.curTool = tool;
						}, firstCorner);
					});
					DebugTools.curTool = tool;
				}
			});
		}
		return list;
	}

	[DebugAction("Map", "SketchGen", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
	private static List<DebugActionNode> SketchGen()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (SketchResolverDef item in DefDatabase<SketchResolverDef>.AllDefs.Where((SketchResolverDef x) => x.isRoot))
		{
			SketchResolverDef localResolver = item;
			DebugActionNode debugActionNode = new DebugActionNode(localResolver.defName);
			if (localResolver == SketchResolverDefOf.Monument || localResolver == SketchResolverDefOf.MonumentRuin)
			{
				new List<DebugMenuOption>();
				for (int i = 1; i <= 60; i++)
				{
					int localIndex = i;
					debugActionNode.AddChild(new DebugActionNode(localIndex.ToString(), DebugActionType.ToolMap)
					{
						action = delegate
						{
							RimWorld.SketchGen.ResolveParams parms = default(RimWorld.SketchGen.ResolveParams);
							parms.sketch = new Sketch();
							parms.monumentSize = new IntVec2(localIndex, localIndex);
							RimWorld.SketchGen.SketchGen.Generate(localResolver, parms).Spawn(Find.CurrentMap, UI.MouseCell(), null, Sketch.SpawnPosType.Unchanged, Sketch.SpawnMode.Normal, wipeIfCollides: false, clearEdificeWhereFloor: false, null, dormant: false, buildRoofsInstantly: true);
						}
					});
				}
			}
			else
			{
				debugActionNode.actionType = DebugActionType.ToolMap;
				debugActionNode.action = delegate
				{
					RimWorld.SketchGen.ResolveParams parms2 = default(RimWorld.SketchGen.ResolveParams);
					parms2.sketch = new Sketch();
					RimWorld.SketchGen.SketchGen.Generate(localResolver, parms2).Spawn(Find.CurrentMap, UI.MouseCell(), null, Sketch.SpawnPosType.Unchanged, Sketch.SpawnMode.Normal, wipeIfCollides: false, clearEdificeWhereFloor: false, null, dormant: false, buildRoofsInstantly: true);
				};
			}
			list.Add(debugActionNode);
		}
		return list;
	}

	[DebugAction("Map", "Set terrain (rect)", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100)]
	private static List<DebugActionNode> SetTerrainRect()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (TerrainDef allDef in DefDatabase<TerrainDef>.AllDefs)
		{
			TerrainDef localDef = allDef;
			list.Add(new DebugActionNode(localDef.defName)
			{
				action = delegate
				{
					DebugToolsGeneral.GenericRectTool(localDef.defName, delegate(CellRect rect)
					{
						foreach (IntVec3 item in rect)
						{
							Find.CurrentMap.terrainGrid.SetTerrain(item, localDef);
						}
					});
				}
			});
		}
		return list;
	}

	[DebugAction("Map", "Pollute (rect)", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100, requiresBiotech = true)]
	private static void PolluteRect()
	{
		DebugToolsGeneral.GenericRectTool("Pollute", delegate(CellRect rect)
		{
			foreach (IntVec3 item in rect)
			{
				Find.CurrentMap.pollutionGrid.SetPolluted(item, isPolluted: true);
			}
		});
	}

	[DebugAction("Map", "Unpollute (rect)", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100, requiresBiotech = true)]
	private static void UnpolluteRect()
	{
		DebugToolsGeneral.GenericRectTool("Unpollute", delegate(CellRect rect)
		{
			foreach (IntVec3 item in rect)
			{
				Find.CurrentMap.pollutionGrid.SetPolluted(item, isPolluted: false);
			}
		});
	}

	[DebugAction("Map", "Make rock (rect)", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 100)]
	private static void MakeRock()
	{
		DebugToolsGeneral.GenericRectTool("Make rock", delegate(CellRect rect)
		{
			foreach (IntVec3 item in rect)
			{
				GenSpawn.Spawn(ThingDefOf.Granite, item, Find.CurrentMap);
			}
		});
	}

	[DebugAction("Map", "Grow pollution (x10 cell)", false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -200, requiresBiotech = true)]
	private static void PolluteCellTen()
	{
		PollutionUtility.GrowPollutionAt(UI.MouseCell(), Find.CurrentMap, 10);
	}

	[DebugAction("Map", "Grow pollution (x100 cell)", false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -200, requiresBiotech = true)]
	private static void PolluteCellHundred()
	{
		PollutionUtility.GrowPollutionAt(UI.MouseCell(), Find.CurrentMap, 100);
	}

	[DebugAction("Map", "Grow pollution (x1000 cell)", false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -200, requiresBiotech = true)]
	private static void PolluteCellThousand()
	{
		PollutionUtility.GrowPollutionAt(UI.MouseCell(), Find.CurrentMap, 1000);
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static List<DebugActionNode> AddGameCondition()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (GameConditionDef allDef in DefDatabase<GameConditionDef>.AllDefs)
		{
			GameConditionDef localDef = allDef;
			DebugActionNode debugActionNode = new DebugActionNode(localDef.LabelCap);
			debugActionNode.AddChild(new DebugActionNode("Permanent")
			{
				action = delegate
				{
					GameCondition gameCondition = GameConditionMaker.MakeCondition(localDef);
					gameCondition.Permanent = true;
					Find.CurrentMap.GameConditionManager.RegisterCondition(gameCondition);
				}
			});
			for (int i = 2500; i <= 60000; i += 2500)
			{
				int localTicks = i;
				debugActionNode.AddChild(new DebugActionNode(localTicks.ToStringTicksToPeriod() ?? "")
				{
					action = delegate
					{
						GameCondition gameCondition2 = GameConditionMaker.MakeCondition(localDef);
						gameCondition2.Duration = localTicks;
						Find.CurrentMap.GameConditionManager.RegisterCondition(gameCondition2);
					}
				});
			}
			list.Add(debugActionNode);
		}
		return list;
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static List<DebugActionNode> RemoveGameCondition()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (GameConditionDef allDef in DefDatabase<GameConditionDef>.AllDefs)
		{
			GameConditionDef localDef = allDef;
			list.Add(new DebugActionNode(localDef.LabelCap)
			{
				action = delegate
				{
					GameCondition activeCondition = Find.CurrentMap.gameConditionManager.GetActiveCondition(localDef);
					if (activeCondition != null)
					{
						activeCondition.Duration = 0;
					}
				},
				visibilityGetter = () => Find.CurrentMap != null && Find.CurrentMap.gameConditionManager.ConditionIsActive(localDef)
			});
		}
		return list;
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void RefogMap()
	{
		FloodFillerFog.DebugRefogMap(Find.CurrentMap);
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> UseGenStep()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (Type item in typeof(GenStep).AllSubclassesNonAbstract())
		{
			Type localGenStep = item;
			list.Add(new DebugActionNode(localGenStep.Name)
			{
				action = delegate
				{
					((GenStep)Activator.CreateInstance(localGenStep)).Generate(Find.CurrentMap, default(GenStepParams));
				}
			});
		}
		return list;
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void RegenSection()
	{
		Find.CurrentMap.mapDrawer.SectionAt(UI.MouseCell()).RegenerateAllLayers();
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void RegenAllMapMeshSections()
	{
		Find.CurrentMap.mapDrawer.RegenerateEverythingNow();
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void AddSnow()
	{
		SnowUtility.AddSnowRadial(UI.MouseCell(), Find.CurrentMap, 5f, 1f);
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void RemoveSnow()
	{
		SnowUtility.AddSnowRadial(UI.MouseCell(), Find.CurrentMap, 5f, -1f);
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void ClearAllSnow()
	{
		foreach (IntVec3 allCell in Find.CurrentMap.AllCells)
		{
			Find.CurrentMap.snowGrid.SetDepth(allCell, 0f);
		}
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing, hideInSubMenu = true)]
	private static void GenerateMap()
	{
		MapParent mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
		mapParent.Tile = TileFinder.RandomStartingTile();
		mapParent.SetFaction(Faction.OfPlayer);
		Find.WorldObjects.Add(mapParent);
		GetOrGenerateMapUtility.GetOrGenerateMap(mapParent.Tile, new IntVec3(50, 1, 50), null);
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static void DestroyMap()
	{
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			Map map = maps[i];
			list.Add(new DebugMenuOption(map.ToString(), DebugMenuOptionMode.Action, delegate
			{
				Current.Game.DeinitAndRemoveMap(map, notifyPlayer: true);
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing, hideInSubMenu = true)]
	private static void LeakMap()
	{
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			Map map = maps[i];
			list.Add(new DebugMenuOption(map.ToString(), DebugMenuOptionMode.Action, delegate
			{
				mapLeak = map;
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing, hideInSubMenu = true)]
	private static void PrintLeakedMap()
	{
		Log.Message($"Leaked map {mapLeak}");
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing, actionType = DebugActionType.ToolMap)]
	private static void Transfer()
	{
		List<Thing> toTransfer = Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).ToList();
		if (!toTransfer.Any())
		{
			return;
		}
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			Map map = maps[i];
			if (map == Find.CurrentMap)
			{
				continue;
			}
			list.Add(new DebugMenuOption(map.ToString(), DebugMenuOptionMode.Action, delegate
			{
				for (int j = 0; j < toTransfer.Count; j++)
				{
					if (CellFinder.TryFindRandomCellNear(map.Center, map, Mathf.Max(map.Size.x, map.Size.z), (IntVec3 x) => !x.Fogged(map) && x.Standable(map), out var result))
					{
						toTransfer[j].DeSpawn();
						GenPlace.TryPlaceThing(toTransfer[j], result, map, ThingPlaceMode.Near);
					}
					else
					{
						Log.Error("Could not find spawn cell.");
					}
				}
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static void ChangeMap()
	{
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		List<Map> maps = Find.Maps;
		for (int i = 0; i < maps.Count; i++)
		{
			Map map = maps[i];
			if (map != Find.CurrentMap)
			{
				list.Add(new DebugMenuOption(map.ToString(), DebugMenuOptionMode.Action, delegate
				{
					Current.Game.CurrentMap = map;
				}));
			}
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static void RegenerateCurrentMap()
	{
		RememberedCameraPos rememberedCameraPos = Find.CurrentMap.rememberedCameraPos;
		int tile = Find.CurrentMap.Tile;
		MapParent parent = Find.CurrentMap.Parent;
		IntVec3 size = Find.CurrentMap.Size;
		Current.Game.DeinitAndRemoveMap(Find.CurrentMap, notifyPlayer: true);
		Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(tile, size, parent.def);
		Current.Game.CurrentMap = orGenerateMap;
		Find.World.renderer.wantedMode = WorldRenderMode.None;
		Find.CameraDriver.SetRootPosAndSize(rememberedCameraPos.rootPos, rememberedCameraPos.rootSize);
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static void GenerateMapWithCaves()
	{
		int tile = TileFinder.RandomSettlementTileFor(Faction.OfPlayer, mustBeAutoChoosable: false, (int x) => Find.World.HasCaves(x));
		if (Find.CurrentMap != null)
		{
			Find.CurrentMap.Parent.Destroy();
		}
		MapParent mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
		mapParent.Tile = tile;
		mapParent.SetFaction(Faction.OfPlayer);
		Find.WorldObjects.Add(mapParent);
		Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(tile, Find.World.info.initialMapSize, null);
		Current.Game.CurrentMap = orGenerateMap;
		Find.World.renderer.wantedMode = WorldRenderMode.None;
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static List<DebugActionNode> RunMapGenerator()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (MapGeneratorDef item in DefDatabase<MapGeneratorDef>.AllDefsListForReading)
		{
			MapGeneratorDef defLocal = item;
			list.Add(new DebugActionNode(defLocal.defName)
			{
				action = delegate
				{
					MapParent mapParent = (MapParent)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
					mapParent.Tile = (from tile in Enumerable.Range(0, Find.WorldGrid.TilesCount)
						where Find.WorldGrid[tile].biome.canBuildBase
						select tile).RandomElement();
					mapParent.SetFaction(Faction.OfPlayer);
					Find.WorldObjects.Add(mapParent);
					Map currentMap = MapGenerator.GenerateMap(Find.World.info.initialMapSize, mapParent, defLocal);
					Current.Game.CurrentMap = currentMap;
				}
			});
		}
		return list;
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static List<DebugActionNode> GeneratePocketMap()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (MapGeneratorDef item in DefDatabase<MapGeneratorDef>.AllDefsListForReading)
		{
			MapGeneratorDef defLocal = item;
			list.Add(new DebugActionNode(defLocal.defName)
			{
				action = delegate
				{
					Map currentMap = PocketMapUtility.GeneratePocketMap(new IntVec3(100, 1, 100), defLocal, null, Find.CurrentMap);
					Current.Game.CurrentMap = currentMap;
				}
			});
		}
		return list;
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static void ForceReformInCurrentMap()
	{
		if (Find.CurrentMap == null)
		{
			return;
		}
		MapParent mapParent = Find.CurrentMap.Parent;
		List<Pawn> list = new List<Pawn>();
		if (Dialog_FormCaravan.AllSendablePawns(mapParent.Map, reform: true).Any((Pawn x) => x.IsColonist))
		{
			Messages.Message("MessageYouHaveToReformCaravanNow".Translate(), new GlobalTargetInfo(mapParent.Tile), MessageTypeDefOf.NeutralEvent);
			Current.Game.CurrentMap = mapParent.Map;
			Dialog_FormCaravan window = new Dialog_FormCaravan(mapParent.Map, reform: true, delegate
			{
				if (mapParent.HasMap)
				{
					mapParent.Destroy();
				}
			}, mapAboutToBeRemoved: true, null);
			Find.WindowStack.Add(window);
			return;
		}
		list.Clear();
		list.AddRange(mapParent.Map.mapPawns.AllPawns.Where((Pawn x) => x.Faction == Faction.OfPlayer || x.HostFaction == Faction.OfPlayer));
		if (list.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer)))
		{
			CaravanExitMapUtility.ExitMapAndCreateCaravan(list, Faction.OfPlayer, mapParent.Tile, mapParent.Tile, -1);
		}
		list.Clear();
		mapParent.Destroy();
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
	private static void FillMapWithTrees()
	{
		Map currentMap = Find.CurrentMap;
		foreach (IntVec3 allCell in currentMap.AllCells)
		{
			if (allCell.Standable(currentMap))
			{
				GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Plant_TreeOak), allCell, currentMap);
			}
		}
	}

	[DebugAction("Map", null, false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, requiresBiotech = true)]
	private static void LogMapPollution()
	{
		Log.Message("Polluted (of all possible pollutable cells): " + Find.CurrentMap.pollutionGrid.TotalPollutionPercent.ToStringPercent());
	}
}
