using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;

namespace Verse;

public static class DebugActionsIncidents
{
	[DebugAction("Incidents", "Do trade caravan arrival...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void DoTradeCaravanSpecific()
	{
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		IncidentDef incidentDef = IncidentDefOf.TraderCaravanArrival;
		Map target = Find.CurrentMap;
		foreach (Faction faction in Find.FactionManager.AllFactions)
		{
			if (faction.def.caravanTraderKinds == null || !faction.def.caravanTraderKinds.Any())
			{
				continue;
			}
			list.Add(new DebugMenuOption(faction.Name, DebugMenuOptionMode.Action, delegate
			{
				List<DebugMenuOption> list2 = new List<DebugMenuOption>();
				foreach (TraderKindDef traderKind in faction.def.caravanTraderKinds)
				{
					string text = traderKind.label;
					IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, target);
					parms.faction = faction;
					parms.traderKind = traderKind;
					if (!incidentDef.Worker.CanFireNow(parms))
					{
						text += " [NO]";
					}
					list2.Add(new DebugMenuOption(text, DebugMenuOptionMode.Action, delegate
					{
						IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(incidentDef.category, target);
						incidentParms.forced = true;
						if (incidentDef.pointsScaleable)
						{
							incidentParms = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain).GenerateParms(incidentDef.category, parms.target);
						}
						incidentParms.faction = faction;
						incidentParms.traderKind = traderKind;
						incidentDef.Worker.TryExecute(incidentParms);
					}));
				}
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
			}));
		}
		if (list.Count == 0)
		{
			Messages.Message("No valid factions found for trade caravans", MessageTypeDefOf.RejectInput, historical: false);
		}
		else
		{
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}
	}

	[DebugAction("Incidents", "Execute raid with points...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static List<DebugActionNode> ExecuteRaidWithPoints()
	{
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (float item in DebugActionsUtility.PointsOptions(extended: true))
		{
			float localP = item;
			DebugActionNode debugActionNode = new DebugActionNode(localP + " points");
			debugActionNode.action = delegate
			{
				IncidentParms parms = new IncidentParms
				{
					target = Find.CurrentMap,
					points = localP,
					forced = true
				};
				IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
			};
			list.Add(debugActionNode);
		}
		return list;
	}

	[DebugAction("Incidents", "Execute raid with faction...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void ExecuteRaidWithFaction()
	{
		StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
		IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
		parms.forced = true;
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		foreach (Faction allFaction in Find.FactionManager.AllFactions)
		{
			Faction localFac = allFaction;
			list.Add(new DebugMenuOption(localFac.Name + " (" + localFac.def.defName + ")", DebugMenuOptionMode.Action, delegate
			{
				parms.faction = localFac;
				List<DebugMenuOption> list2 = new List<DebugMenuOption>();
				foreach (float item in DebugActionsUtility.PointsOptions(extended: true))
				{
					float localPoints = item;
					list2.Add(new DebugMenuOption(item + " points", DebugMenuOptionMode.Action, delegate
					{
						parms.points = localPoints;
						List<RaidStrategyDef> source = DefDatabase<RaidStrategyDef>.AllDefs.Where((RaidStrategyDef s) => s.Worker.CanUseWith(parms, PawnGroupKindDefOf.Combat)).ToList();
						parms.raidStrategy = source.RandomElement();
						if (parms.raidStrategy != null)
						{
							List<PawnsArrivalModeDef> source2 = DefDatabase<PawnsArrivalModeDef>.AllDefs.Where((PawnsArrivalModeDef a) => a.Worker.CanUseWith(parms) && parms.raidStrategy.arriveModes.Contains(a)).ToList();
							parms.raidArrivalMode = source2.RandomElement();
						}
						DoRaid(parms);
					}));
				}
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	[DebugAction("Incidents", "Psychic ritual siege...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap, requiresAnomaly = true)]
	private static List<DebugActionNode> RitualSiegeWithSpecifics()
	{
		StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
		IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
		parms.forced = true;
		List<DebugActionNode> list = new List<DebugActionNode>();
		foreach (PsychicRitualDef def in DefDatabase<PsychicRitualDef>.AllDefs.Where((PsychicRitualDef x) => x.aiCastable))
		{
			DebugActionNode item = new DebugActionNode(def.label, DebugActionType.Action, delegate
			{
				parms.psychicRitualDef = def;
				IncidentDefOf.PsychicRitualSiege.Worker.TryExecute(parms);
			});
			list.Add(item);
		}
		return list;
	}

	[DebugAction("Incidents", "Execute raid with specifics...", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void ExecuteRaidWithSpecifics()
	{
		StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
		IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
		parms.forced = true;
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		foreach (Faction allFaction in Find.FactionManager.AllFactions)
		{
			Faction localFac = allFaction;
			list.Add(new DebugMenuOption(localFac.Name + " (" + localFac.def.defName + ")", DebugMenuOptionMode.Action, delegate
			{
				parms.faction = localFac;
				List<DebugMenuOption> list2 = new List<DebugMenuOption>();
				foreach (float item in DebugActionsUtility.PointsOptions(extended: true))
				{
					float localPoints = item;
					list2.Add(new DebugMenuOption(item + " points", DebugMenuOptionMode.Action, delegate
					{
						parms.points = localPoints;
						List<DebugMenuOption> list3 = new List<DebugMenuOption>();
						foreach (RaidStrategyDef allDef in DefDatabase<RaidStrategyDef>.AllDefs)
						{
							RaidStrategyDef localStrat = allDef;
							string text = localStrat.defName;
							if (!localStrat.Worker.CanUseWith(parms, PawnGroupKindDefOf.Combat))
							{
								text += " [NO]";
							}
							list3.Add(new DebugMenuOption(text, DebugMenuOptionMode.Action, delegate
							{
								parms.raidStrategy = localStrat;
								List<DebugMenuOption> list4 = new List<DebugMenuOption>
								{
									new DebugMenuOption("-Random-", DebugMenuOptionMode.Action, delegate
									{
										DoRaid(parms);
									})
								};
								foreach (PawnsArrivalModeDef allDef2 in DefDatabase<PawnsArrivalModeDef>.AllDefs)
								{
									PawnsArrivalModeDef localArrival = allDef2;
									string text2 = localArrival.defName;
									if (!localArrival.Worker.CanUseWith(parms) || !localStrat.arriveModes.Contains(localArrival))
									{
										text2 += " [NO]";
									}
									list4.Add(new DebugMenuOption(text2, DebugMenuOptionMode.Action, delegate
									{
										parms.raidArrivalMode = localArrival;
										if (ModsConfig.BiotechActive)
										{
											List<DebugMenuOption> list5 = new List<DebugMenuOption>
											{
												new DebugMenuOption("-Random-", DebugMenuOptionMode.Action, delegate
												{
													DoRaid(parms);
												})
											};
											foreach (RaidAgeRestrictionDef allDef3 in DefDatabase<RaidAgeRestrictionDef>.AllDefs)
											{
												RaidAgeRestrictionDef localAgeRestriction = allDef3;
												string text3 = localAgeRestriction.defName;
												if (!localAgeRestriction.Worker.CanUseWith(parms))
												{
													text3 += " [NO]";
												}
												list5.Add(new DebugMenuOption(text3, DebugMenuOptionMode.Action, delegate
												{
													parms.raidAgeRestriction = localAgeRestriction;
													DoRaid(parms);
												}));
											}
											Find.WindowStack.Add(new Dialog_DebugOptionListLister(list5));
										}
										else
										{
											DoRaid(parms);
										}
									}));
								}
								Find.WindowStack.Add(new Dialog_DebugOptionListLister(list4));
							}));
						}
						Find.WindowStack.Add(new Dialog_DebugOptionListLister(list3));
					}));
				}
				Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	[DebugActionYielder]
	private static IEnumerable<DebugActionNode> IncidentsYielder()
	{
		yield return GetIncidentDebugAction("Do incident", 1);
		yield return GetIncidentDebugAction("Do incident x10", 10);
		yield return GetIncidentWithPointsDebugAction();
	}

	private static DebugActionNode GetIncidentDebugAction(string name, int iterations)
	{
		return new DebugActionNode(name)
		{
			category = "Incidents",
			labelGetter = () => name + " (" + GetIncidentTargetLabel() + ")...",
			visibilityGetter = YielderOptionVisible,
			childGetter = delegate
			{
				List<DebugActionNode> list = new List<DebugActionNode>();
				foreach (IncidentDef item in DefDatabase<IncidentDef>.AllDefs.OrderBy((IncidentDef x) => x.defName))
				{
					IncidentDef localDef = item;
					DebugActionNode child = new DebugActionNode(localDef.defName, DebugActionType.Action, delegate
					{
						IIncidentTarget target = GetTarget();
						if (target == null || !localDef.TargetAllowed(target))
						{
							Log.Warning("Incident target is null or not allowed.");
						}
						else
						{
							IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(localDef.category, target);
							incidentParms.forced = true;
							if (localDef.pointsScaleable)
							{
								incidentParms = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain).GenerateParms(localDef.category, incidentParms.target);
							}
							for (int i = 0; i < iterations; i++)
							{
								localDef.Worker.TryExecute(incidentParms);
							}
						}
					});
					child.labelGetter = delegate
					{
						string text = child.label;
						IIncidentTarget target2 = GetTarget();
						if (target2 != null)
						{
							IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, target2);
							if (!localDef.TargetAllowed(target2) || !localDef.Worker.CanFireNow(parms))
							{
								text += " [NO]";
							}
						}
						return text;
					};
					list.Add(child);
				}
				return list;
			}
		};
	}

	private static DebugActionNode GetIncidentWithPointsDebugAction()
	{
		return new DebugActionNode("Do incident w/ points")
		{
			category = "Incidents",
			labelGetter = () => "Do incident w/ points (" + GetIncidentTargetLabel() + ")...",
			visibilityGetter = YielderOptionVisible,
			childGetter = delegate
			{
				List<DebugActionNode> list = new List<DebugActionNode>();
				foreach (IncidentDef item in from x in DefDatabase<IncidentDef>.AllDefs
					where x.pointsScaleable
					select x into y
					orderby y.defName
					select y)
				{
					IncidentDef localDef = item;
					IIncidentTarget target = GetTarget();
					if (target == null || localDef.TargetAllowed(target))
					{
						DebugActionNode child = new DebugActionNode(localDef.defName);
						foreach (float item2 in DebugActionsUtility.PointsOptions(extended: true))
						{
							float localPoints = item2;
							DebugActionNode grandchild = new DebugActionNode(localPoints + " points", DebugActionType.Action, delegate
							{
								IIncidentTarget target2 = GetTarget();
								if (target2 != null && localDef.TargetAllowed(target2))
								{
									IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(localDef.category, target2);
									incidentParms.forced = true;
									incidentParms.points = localPoints;
									localDef.Worker.TryExecute(incidentParms);
								}
							});
							grandchild.labelGetter = delegate
							{
								string text = grandchild.label;
								IIncidentTarget target3 = GetTarget();
								if (target3 != null)
								{
									if (!localDef.TargetAllowed(target3))
									{
										text += "[NO]";
									}
									else
									{
										IncidentParms incidentParms2 = StorytellerUtility.DefaultParmsNow(localDef.category, target3);
										incidentParms2.points = localPoints;
										if (!localDef.Worker.CanFireNow(incidentParms2))
										{
											text += " [NO]";
										}
									}
								}
								return text;
							};
							child.AddChild(grandchild);
						}
						child.labelGetter = delegate
						{
							string text2 = child.label;
							IIncidentTarget target4 = GetTarget();
							if (target4 != null)
							{
								if (!localDef.TargetAllowed(target4))
								{
									text2 += " [NO]";
								}
								else
								{
									IncidentParms parms = StorytellerUtility.DefaultParmsNow(localDef.category, target4);
									if (!localDef.Worker.CanFireNow(parms))
									{
										text2 += " [NO]";
									}
								}
							}
							return text2;
						};
						list.Add(child);
					}
				}
				return list;
			}
		};
	}

	private static void DoRaid(IncidentParms parms)
	{
		IncidentDef incidentDef = ((!parms.faction.HostileTo(Faction.OfPlayer)) ? IncidentDefOf.RaidFriendly : IncidentDefOf.RaidEnemy);
		incidentDef.Worker.TryExecute(parms);
	}

	private static bool YielderOptionVisible()
	{
		if (Current.ProgramState != ProgramState.Playing)
		{
			return false;
		}
		IIncidentTarget target = GetTarget();
		if (target == null)
		{
			return false;
		}
		bool flag = target is World;
		bool flag2 = target is WorldObject;
		if (WorldRendererUtility.WorldRenderedNow)
		{
			return flag || flag2;
		}
		if (!flag)
		{
			return !flag2;
		}
		return false;
	}

	private static IIncidentTarget GetTarget()
	{
		IIncidentTarget incidentTarget = (WorldRendererUtility.WorldRenderedNow ? (Find.WorldSelector.SingleSelectedObject as IIncidentTarget) : null);
		if (incidentTarget == null && WorldRendererUtility.WorldRenderedNow)
		{
			incidentTarget = Find.World;
		}
		if (incidentTarget == null)
		{
			incidentTarget = Find.CurrentMap;
		}
		return incidentTarget;
	}

	private static string GetIncidentTargetLabel()
	{
		IIncidentTarget target = GetTarget();
		if (target == null)
		{
			return "null target";
		}
		if (target is Map)
		{
			return "Map";
		}
		if (target is World)
		{
			return "World";
		}
		if (target is Caravan)
		{
			return ((Caravan)target).LabelCap;
		}
		return target.ToString();
	}

	[DebugAction("Incidents", "Recalculate threat points", false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void RecalculateThreatPoints()
	{
		Find.CurrentMap.wealthWatcher.ForceRecount();
	}
}
