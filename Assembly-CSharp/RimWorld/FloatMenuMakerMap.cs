using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public static class FloatMenuMakerMap
{
	public static Pawn makingFor;

	private static List<Pawn> tmpPawns = new List<Pawn>();

	private static List<FloatMenuOption> tmpOpts = new List<FloatMenuOption>();

	private static readonly List<Thing> cachedThings = new List<Thing>();

	private static FloatMenuOption[] equivalenceGroupTempStorage;

	private static bool CanTakeOrder(Pawn pawn)
	{
		if (!pawn.IsColonistPlayerControlled && !pawn.IsColonyMech)
		{
			return pawn.IsColonyMutantPlayerControlled;
		}
		return true;
	}

	public static void TryMakeFloatMenu(Pawn pawn)
	{
		try
		{
			if (!CanTakeOrder(pawn))
			{
				return;
			}
			if (pawn.Downed)
			{
				Messages.Message("IsIncapped".Translate(pawn.LabelCap, pawn), pawn, MessageTypeDefOf.RejectInput, historical: false);
			}
			else if (ModsConfig.BiotechActive && pawn.Deathresting)
			{
				Messages.Message("IsDeathresting".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.RejectInput, historical: false);
			}
			else
			{
				if (pawn.Map != Find.CurrentMap)
				{
					return;
				}
				Lord lord;
				if ((lord = pawn.GetLord()) != null)
				{
					AcceptanceReport acceptanceReport = lord.AllowsFloatMenu(pawn);
					if (!acceptanceReport)
					{
						Messages.Message(acceptanceReport.Reason, pawn, MessageTypeDefOf.RejectInput, historical: false);
						return;
					}
				}
				List<FloatMenuOption> list = ChoicesAtFor(UI.MouseMapPosition(), pawn);
				if (list.Count == 0)
				{
					return;
				}
				bool flag = true;
				FloatMenuOption floatMenuOption = null;
				for (int i = 0; i < list.Count; i++)
				{
					try
					{
						if (list[i].Disabled || !list[i].autoTakeable)
						{
							flag = false;
							break;
						}
						if (floatMenuOption == null || list[i].autoTakeablePriority > floatMenuOption.autoTakeablePriority)
						{
							floatMenuOption = list[i];
						}
					}
					catch (Exception ex)
					{
						Log.Error("Error in FloatMenuMakerMap: " + ex);
					}
				}
				if (flag && floatMenuOption != null)
				{
					floatMenuOption.Chosen(colonistOrdering: true, null);
					return;
				}
				FloatMenuMap floatMenuMap = new FloatMenuMap(list, pawn.LabelCap, UI.MouseMapPosition());
				floatMenuMap.givesColonistOrders = true;
				Find.WindowStack.Add(floatMenuMap);
			}
		}
		catch (Exception ex2)
		{
			Log.Error(string.Concat("Error trying to make float menu for ", pawn, ": ", ex2));
		}
	}

	public static void TryMakeFloatMenu_NonPawn(Thing selectedThing)
	{
		try
		{
			if (selectedThing.Map != Find.CurrentMap)
			{
				return;
			}
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			IntVec3 c = IntVec3.FromVector3(UI.MouseMapPosition());
			if (!c.InBounds(Find.CurrentMap))
			{
				return;
			}
			foreach (Thing item in selectedThing.Map.thingGrid.ThingsAt(c))
			{
				if (item != selectedThing)
				{
					list.AddRange(item.GetFloatMenuOptions_NonPawn(selectedThing));
				}
			}
			if (list.Any())
			{
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}
		catch (Exception ex)
		{
			Log.Error(string.Concat("Error trying to make float menu for ", selectedThing, ": ", ex));
		}
	}

	public static bool TryMakeMultiSelectFloatMenu(List<Pawn> pawns)
	{
		try
		{
			tmpPawns.AddRange(pawns);
			tmpPawns.RemoveAll(InvalidPawnForMultiSelectOption);
			if (!tmpPawns.Any())
			{
				return false;
			}
			List<FloatMenuOption> list = ChoicesAtForMultiSelect(UI.MouseMapPosition(), tmpPawns);
			if (!list.Any())
			{
				tmpPawns.Clear();
				return false;
			}
			FloatMenu window = new FloatMenu(list)
			{
				givesColonistOrders = true
			};
			Find.WindowStack.Add(window);
		}
		catch (Exception ex)
		{
			Log.Error("Error trying to make multi-select float menu: " + ex);
		}
		finally
		{
			tmpPawns.Clear();
		}
		return true;
	}

	private static bool LordBlocksFloatMenu(Pawn pawn)
	{
		return !(pawn.GetLord()?.AllowsFloatMenu(pawn) ?? ((AcceptanceReport)true));
	}

	public static bool InvalidPawnForMultiSelectOption(Pawn pawn)
	{
		if (CanTakeOrder(pawn) && !pawn.Downed && pawn.Map == Find.CurrentMap)
		{
			return LordBlocksFloatMenu(pawn);
		}
		return true;
	}

	public static List<FloatMenuOption> ChoicesAtFor(Vector3 clickPos, Pawn pawn, bool suppressAutoTakeableGoto = false)
	{
		IntVec3 intVec = IntVec3.FromVector3(clickPos);
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		pawn.GetLord();
		if (!intVec.InBounds(pawn.Map) || !CanTakeOrder(pawn) || LordBlocksFloatMenu(pawn))
		{
			return list;
		}
		if (pawn.Map != Find.CurrentMap)
		{
			return list;
		}
		makingFor = pawn;
		try
		{
			if (intVec.Fogged(pawn.Map))
			{
				if (pawn.Drafted)
				{
					FloatMenuOption floatMenuOption = GotoLocationOption(intVec, pawn, suppressAutoTakeableGoto);
					if (floatMenuOption != null && !floatMenuOption.Disabled)
					{
						list.Add(floatMenuOption);
					}
				}
			}
			else
			{
				if (pawn.Drafted)
				{
					AddDraftedOrders(clickPos, pawn, list, suppressAutoTakeableGoto);
				}
				if (pawn.RaceProps.Humanlike && !pawn.IsMutant)
				{
					AddHumanlikeOrders(clickPos, pawn, list);
				}
				if (ModsConfig.AnomalyActive && pawn.IsMutant)
				{
					AddMutantOrders(clickPos, pawn, list);
				}
				if (!pawn.Drafted && (!pawn.RaceProps.IsMechanoid || DebugSettings.allowUndraftedMechOrders) && !pawn.IsMutant)
				{
					AddUndraftedOrders(clickPos, pawn, list);
				}
				foreach (FloatMenuOption item in pawn.GetExtraFloatMenuOptionsFor(intVec))
				{
					list.Add(item);
				}
				FloatMenuOption floatMenuOptFor = EnterPortalUtility.GetFloatMenuOptFor(pawn, intVec);
				if (floatMenuOptFor != null)
				{
					list.Add(floatMenuOptFor);
				}
			}
		}
		finally
		{
			makingFor = null;
		}
		return list;
	}

	public static List<FloatMenuOption> ChoicesAtForMultiSelect(Vector3 clickPos, List<Pawn> pawns)
	{
		try
		{
			tmpOpts.Clear();
			IntVec3 intVec = IntVec3.FromVector3(clickPos);
			Map map = pawns[0].Map;
			if (!intVec.InBounds(map) || map != Find.CurrentMap || intVec.Fogged(map))
			{
				return tmpOpts;
			}
			foreach (Thing item in map.thingGrid.ThingsAt(intVec))
			{
				foreach (FloatMenuOption multiSelectFloatMenuOption in item.GetMultiSelectFloatMenuOptions(pawns))
				{
					tmpOpts.Add(multiSelectFloatMenuOption);
				}
			}
			FloatMenuOption floatMenuOptFor = EnterPortalUtility.GetFloatMenuOptFor(tmpPawns, intVec);
			if (floatMenuOptFor != null)
			{
				tmpOpts.Add(floatMenuOptFor);
			}
			return tmpOpts;
		}
		catch (Exception ex)
		{
			Log.Error("Error trying to make multi-select float menu: " + ex);
			return tmpOpts;
		}
	}

	private static void AddDraftedOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool suppressAutoTakeableGoto = false)
	{
		IntVec3 clickCell = IntVec3.FromVector3(clickPos);
		foreach (LocalTargetInfo item6 in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), thingsOnly: true))
		{
			LocalTargetInfo attackTarg = item6;
			if (ModsConfig.BiotechActive && pawn.IsColonyMech && !MechanitorUtility.InMechanitorCommandRange(pawn, attackTarg))
			{
				continue;
			}
			if (pawn.equipment.Primary != null && !pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.IsMeleeAttack)
			{
				string failStr;
				Action rangedAct = FloatMenuUtility.GetRangedAttackAction(pawn, attackTarg, out failStr);
				string text = "FireAt".Translate(attackTarg.Thing.Label, attackTarg.Thing);
				MenuOptionPriority priority = ((!attackTarg.HasThing || !pawn.HostileTo(attackTarg.Thing)) ? MenuOptionPriority.VeryLow : MenuOptionPriority.AttackEnemy);
				FloatMenuOption floatMenuOption = new FloatMenuOption("", null, priority, null, item6.Thing);
				if (rangedAct == null)
				{
					text = text + ": " + failStr;
				}
				else
				{
					floatMenuOption.autoTakeable = !attackTarg.HasThing || attackTarg.Thing.HostileTo(Faction.OfPlayer);
					floatMenuOption.autoTakeablePriority = 40f;
					floatMenuOption.action = delegate
					{
						FleckMaker.Static(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, FleckDefOf.FeedbackShoot);
						rangedAct();
					};
				}
				floatMenuOption.Label = text;
				opts.Add(floatMenuOption);
			}
			string failStr2;
			Action meleeAct = FloatMenuUtility.GetMeleeAttackAction(pawn, attackTarg, out failStr2);
			string text2 = ((!(attackTarg.Thing is Pawn { Downed: not false })) ? ((string)"MeleeAttack".Translate(attackTarg.Thing.Label, attackTarg.Thing)) : ((string)"MeleeAttackToDeath".Translate(attackTarg.Thing.Label, attackTarg.Thing)));
			MenuOptionPriority priority2 = ((!attackTarg.HasThing || !pawn.HostileTo(attackTarg.Thing)) ? MenuOptionPriority.VeryLow : MenuOptionPriority.AttackEnemy);
			FloatMenuOption floatMenuOption2 = new FloatMenuOption("", null, priority2, null, attackTarg.Thing);
			if (meleeAct == null)
			{
				text2 = text2 + ": " + failStr2.CapitalizeFirst();
			}
			else
			{
				floatMenuOption2.autoTakeable = !attackTarg.HasThing || attackTarg.Thing.HostileTo(Faction.OfPlayer);
				floatMenuOption2.autoTakeablePriority = 30f;
				floatMenuOption2.action = delegate
				{
					FleckMaker.Static(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, FleckDefOf.FeedbackMelee);
					meleeAct();
				};
			}
			floatMenuOption2.Label = text2;
			opts.Add(floatMenuOption2);
		}
		if (!pawn.RaceProps.IsMechanoid && !pawn.IsMutant)
		{
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				foreach (LocalTargetInfo item7 in GenUI.TargetsAt(clickPos, TargetingParameters.ForCarry(pawn), thingsOnly: true))
				{
					LocalTargetInfo carryTarget = item7;
					FloatMenuOption item = (pawn.CanReach(carryTarget, PathEndMode.ClosestTouch, Danger.Deadly) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Carry".Translate(carryTarget.Thing), delegate
					{
						carryTarget.Thing.SetForbidden(value: false, warnOnFail: false);
						Job job = JobMaker.MakeJob(JobDefOf.CarryDownedPawnDrafted, carryTarget);
						job.count = 1;
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}), pawn, carryTarget) : new FloatMenuOption("CannotCarry".Translate(carryTarget.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					opts.Add(item);
				}
			}
			if (pawn.IsCarryingPawn())
			{
				Pawn carriedPawn = (Pawn)pawn.carryTracker.CarriedThing;
				if (!carriedPawn.IsPrisonerOfColony)
				{
					foreach (LocalTargetInfo item8 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryBed(carriedPawn, pawn, carriedPawn.GuestStatus), thingsOnly: true))
					{
						LocalTargetInfo destTarget = item8;
						FloatMenuOption item2 = (pawn.CanReach(destTarget, PathEndMode.ClosestTouch, Danger.Deadly) ? ((!pawn.HostileTo(carriedPawn)) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PlaceIn".Translate(carriedPawn, destTarget.Thing), delegate
						{
							destTarget.Thing.SetForbidden(value: false, warnOnFail: false);
							Job job2 = JobMaker.MakeJob(JobDefOf.TakeDownedPawnToBedDrafted, pawn.carryTracker.CarriedThing, destTarget);
							job2.count = 1;
							pawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
						}), pawn, destTarget) : new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, destTarget.Thing) + ": " + "CarriedPawnHostile".Translate().CapitalizeFirst(), null)) : new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, destTarget.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						opts.Add(item2);
					}
				}
				CompHoldingPlatformTarget comp;
				if (carriedPawn.CanBeCaptured())
				{
					foreach (LocalTargetInfo item9 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryBed(carriedPawn, pawn, GuestStatus.Prisoner), thingsOnly: true))
					{
						Building_Bed bed = (Building_Bed)item9.Thing;
						FloatMenuOption item3;
						if (!pawn.CanReach(bed, PathEndMode.ClosestTouch, Danger.Deadly))
						{
							item3 = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, bed) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
						}
						else
						{
							TaggedString taggedString = "Capture".Translate(carriedPawn.LabelCap, carriedPawn);
							if (!carriedPawn.guest.Recruitable)
							{
								taggedString += string.Format(" ({0})", "Unrecruitable".Translate());
							}
							if (carriedPawn.Faction != null && carriedPawn.Faction != Faction.OfPlayer && !carriedPawn.Faction.Hidden && !carriedPawn.Faction.HostileTo(Faction.OfPlayer) && !carriedPawn.IsPrisonerOfColony)
							{
								taggedString += string.Format(": {0}", "AngersFaction".Translate().CapitalizeFirst());
							}
							item3 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString, delegate
							{
								bed.SetForbidden(value: false, warnOnFail: false);
								Job job3 = JobMaker.MakeJob(JobDefOf.CarryToPrisonerBedDrafted, pawn.carryTracker.CarriedThing, bed);
								job3.count = 1;
								pawn.jobs.TryTakeOrderedJob(job3, JobTag.Misc);
							}), pawn, bed);
						}
						opts.Add(item3);
					}
				}
				else if (ModsConfig.AnomalyActive && carriedPawn.TryGetComp<CompHoldingPlatformTarget>(out comp) && comp.CanBeCaptured)
				{
					foreach (LocalTargetInfo item10 in GenUI.TargetsAt(clickPos, TargetingParameters.ForBuilding(), thingsOnly: true))
					{
						if (!item10.Thing.TryGetComp(out CompEntityHolder comp2) || !comp2.Available)
						{
							continue;
						}
						Thing thing = item10.Thing;
						FloatMenuOption item4;
						if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
						{
							item4 = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
						}
						else
						{
							TaggedString taggedString2 = "Capture".Translate(carriedPawn.LabelCap, carriedPawn);
							if (!item10.Thing.SafelyContains(carriedPawn))
							{
								float statValue = carriedPawn.GetStatValue(StatDefOf.MinimumContainmentStrength);
								taggedString2 += string.Format(" ({0} {1:F0}, {2} {3:F0})", "FloatMenuContainmentStrength".Translate().ToLower(), comp2.ContainmentStrength, "FloatMenuContainmentRequires".Translate(carriedPawn).ToLower(), statValue);
							}
							item4 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString2, delegate
							{
								thing.SetForbidden(value: false, warnOnFail: false);
								Job job4 = JobMaker.MakeJob(JobDefOf.CarryToEntityHolderAlreadyHolding, thing, pawn.carryTracker.CarriedThing);
								job4.count = 1;
								pawn.jobs.TryTakeOrderedJob(job4, JobTag.Misc);
							}), pawn, thing);
						}
						opts.Add(item4);
					}
				}
				foreach (LocalTargetInfo item11 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryTransporter(carriedPawn), thingsOnly: true))
				{
					Thing transporterThing = item11.Thing;
					if (transporterThing == null)
					{
						continue;
					}
					CompTransporter compTransporter = transporterThing.TryGetComp<CompTransporter>();
					if (compTransporter.Shuttle != null && !compTransporter.Shuttle.IsAllowedNow(carriedPawn))
					{
						continue;
					}
					if (!pawn.CanReach(transporterThing, PathEndMode.ClosestTouch, Danger.Deadly))
					{
						opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, transporterThing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						continue;
					}
					if (compTransporter.Shuttle == null && !compTransporter.LeftToLoadContains(carriedPawn))
					{
						opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, transporterThing) + ": " + "NotPartOfLaunchGroup".Translate(), null));
						continue;
					}
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PlaceIn".Translate(carriedPawn, transporterThing), delegate
					{
						if (!compTransporter.LoadingInProgressOrReadyToLaunch)
						{
							TransporterUtility.InitiateLoading(Gen.YieldSingle(compTransporter));
						}
						Job job5 = JobMaker.MakeJob(JobDefOf.HaulToTransporter, carriedPawn, transporterThing);
						job5.ignoreForbidden = true;
						job5.count = 1;
						pawn.jobs.TryTakeOrderedJob(job5, JobTag.Misc);
					}), pawn, transporterThing));
				}
				foreach (LocalTargetInfo item12 in GenUI.TargetsAt(clickPos, TargetingParameters.ForDraftedCarryCryptosleepCasket(pawn), thingsOnly: true))
				{
					Thing casket = item12.Thing;
					TaggedString taggedString3 = "PlaceIn".Translate(carriedPawn, casket);
					if (((Building_CryptosleepCasket)casket).HasAnyContents)
					{
						opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, casket) + ": " + "CryptosleepCasketOccupied".Translate(), null));
						continue;
					}
					if (carriedPawn.IsQuestLodger())
					{
						opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, casket) + ": " + "CryptosleepCasketGuestsNotAllowed".Translate(), null));
						continue;
					}
					if (carriedPawn.GetExtraHostFaction() != null)
					{
						opts.Add(new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, casket) + ": " + "CryptosleepCasketGuestPrisonersNotAllowed".Translate(), null));
						continue;
					}
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString3, delegate
					{
						Job job6 = JobMaker.MakeJob(JobDefOf.CarryToCryptosleepCasketDrafted, carriedPawn, casket);
						job6.count = 1;
						job6.playerForced = true;
						pawn.jobs.TryTakeOrderedJob(job6, JobTag.Misc);
					}), pawn, casket));
				}
			}
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.IsMutant)
			{
				foreach (LocalTargetInfo item13 in GenUI.TargetsAt(clickPos, TargetingParameters.ForTend(pawn), thingsOnly: true))
				{
					Pawn tendTarget = (Pawn)item13.Thing;
					if (!tendTarget.health.HasHediffsNeedingTend())
					{
						opts.Add(new FloatMenuOption("CannotTend".Translate(tendTarget) + ": " + "TendingNotRequired".Translate(tendTarget), null));
						continue;
					}
					if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
					{
						opts.Add(new FloatMenuOption("CannotTend".Translate(tendTarget) + ": " + "CannotPrioritizeWorkTypeDisabled".Translate(WorkTypeDefOf.Doctor.gerundLabel), null));
						continue;
					}
					if (!pawn.CanReach(tendTarget, PathEndMode.ClosestTouch, Danger.Deadly))
					{
						opts.Add(new FloatMenuOption("CannotTend".Translate(tendTarget) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						continue;
					}
					Thing medicine = HealthAIUtility.FindBestMedicine(pawn, tendTarget, onlyUseInventory: true);
					TaggedString taggedString4 = "Tend".Translate(tendTarget);
					Action action = delegate
					{
						Job job7 = JobMaker.MakeJob(JobDefOf.TendPatient, tendTarget, medicine);
						job7.count = 1;
						job7.draftedTend = true;
						pawn.jobs.TryTakeOrderedJob(job7, JobTag.Misc);
					};
					if (tendTarget == pawn && pawn.playerSettings != null && !pawn.playerSettings.selfTend)
					{
						action = null;
						taggedString4 = "CannotGenericWorkCustom".Translate("Tend".Translate(tendTarget).ToString().UncapitalizeFirst()) + ": " + "SelfTendDisabled".Translate().CapitalizeFirst();
					}
					else if (tendTarget.InAggroMentalState && !tendTarget.health.hediffSet.HasHediff(HediffDefOf.Scaria))
					{
						action = null;
						taggedString4 = "CannotGenericWorkCustom".Translate("Tend".Translate(tendTarget).ToString().UncapitalizeFirst()) + ": " + "PawnIsInAggroMentalState".Translate(tendTarget).CapitalizeFirst();
					}
					else if (medicine == null)
					{
						taggedString4 += " (" + "WithoutMedicine".Translate() + ")";
					}
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString4, action), pawn, tendTarget));
					if (medicine != null && action != null && pawn.CanReserve(tendTarget) && tendTarget.Spawned)
					{
						opts.Add(new FloatMenuOption("Tend".Translate(tendTarget) + " (" + "WithoutMedicine".Translate() + ")", delegate
						{
							Job job8 = JobMaker.MakeJob(JobDefOf.TendPatient, tendTarget, null);
							job8.count = 1;
							job8.draftedTend = true;
							pawn.jobs.TryTakeOrderedJob(job8, JobTag.Misc);
						}));
					}
				}
				foreach (LocalTargetInfo item14 in GenUI.TargetsAt(clickPos, TargetingParameters.ForHeldEntity()))
				{
					Building_HoldingPlatform holdingPlatform;
					if ((holdingPlatform = item14.Thing as Building_HoldingPlatform) == null)
					{
						continue;
					}
					Pawn heldPawn = holdingPlatform.HeldPawn;
					if (heldPawn == null)
					{
						continue;
					}
					if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
					{
						opts.Add(new FloatMenuOption("CannotTend".Translate(heldPawn) + ": " + "CannotPrioritizeWorkTypeDisabled".Translate(WorkTypeDefOf.Doctor.gerundLabel), null));
					}
					else if (!pawn.CanReach(holdingPlatform, PathEndMode.ClosestTouch, Danger.Deadly))
					{
						opts.Add(new FloatMenuOption("CannotTend".Translate(heldPawn) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					}
					else if (HealthAIUtility.ShouldBeTendedNowByPlayer(heldPawn) && pawn.CanReserveAndReach(holdingPlatform, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
					{
						Thing medicine2 = HealthAIUtility.FindBestMedicine(pawn, heldPawn);
						opts.Add(new FloatMenuOption("Tend".Translate(heldPawn.LabelShort), delegate
						{
							JobDef tendEntity = JobDefOf.TendEntity;
							LocalTargetInfo targetA = holdingPlatform;
							Thing thing2 = medicine2;
							Job job9 = JobMaker.MakeJob(tendEntity, targetA, (thing2 != null) ? ((LocalTargetInfo)thing2) : LocalTargetInfo.Invalid);
							job9.count = 1;
							job9.draftedTend = true;
							pawn.jobs.TryTakeOrderedJob(job9, JobTag.Misc);
						}));
					}
				}
				if (pawn.skills != null && !pawn.skills.GetSkill(SkillDefOf.Construction).TotallyDisabled)
				{
					foreach (LocalTargetInfo item15 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRepair(pawn), thingsOnly: true))
					{
						Thing repairTarget = item15.Thing;
						if (!pawn.CanReach(repairTarget, PathEndMode.Touch, Danger.Deadly))
						{
							opts.Add(new FloatMenuOption("CannotRepair".Translate(repairTarget) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						}
						else if (RepairUtility.PawnCanRepairNow(pawn, repairTarget))
						{
							FloatMenuOption item5 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RepairThing".Translate(repairTarget), delegate
							{
								pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Repair, repairTarget), JobTag.Misc);
							}), pawn, repairTarget);
							opts.Add(item5);
						}
					}
				}
			}
		}
		AddJobGiverWorkOrders(clickPos, pawn, opts, drafted: true);
		FloatMenuOption floatMenuOption3 = GotoLocationOption(clickCell, pawn, suppressAutoTakeableGoto);
		if (floatMenuOption3 != null)
		{
			opts.Add(floatMenuOption3);
		}
	}

	private static void AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
	{
		IntVec3 clickCell = IntVec3.FromVector3(clickPos);
		foreach (Thing thing8 in clickCell.GetThingList(pawn.Map))
		{
			if (!(thing8 is Pawn pawn2))
			{
				continue;
			}
			Lord lord = pawn2.GetLord();
			if (lord?.CurLordToil == null)
			{
				continue;
			}
			IEnumerable<FloatMenuOption> enumerable = lord.CurLordToil.ExtraFloatMenuOptions(pawn2, pawn);
			if (enumerable == null)
			{
				continue;
			}
			foreach (FloatMenuOption item11 in enumerable)
			{
				opts.Add(item11);
			}
		}
		if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
		{
			foreach (LocalTargetInfo item12 in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), thingsOnly: true))
			{
				bool flag = item12.HasThing && item12.Thing is Pawn && ((Pawn)item12.Thing).IsWildMan();
				if (!pawn.Drafted && !flag)
				{
					continue;
				}
				if (item12.Thing is Pawn && (pawn.InSameExtraFaction((Pawn)item12.Thing, ExtraFactionType.HomeFaction) || pawn.InSameExtraFaction((Pawn)item12.Thing, ExtraFactionType.MiniFaction)))
				{
					opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "SameFaction".Translate((Pawn)item12.Thing), null));
					continue;
				}
				if (!pawn.CanReach(item12, PathEndMode.OnCell, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}
				Pawn pTarg = (Pawn)item12.Thing;
				Action action = delegate
				{
					Building_Bed building_Bed = RestUtility.FindBedFor(pTarg, pawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Prisoner);
					if (building_Bed == null)
					{
						building_Bed = RestUtility.FindBedFor(pTarg, pawn, checkSocialProperness: false, ignoreOtherReservations: true, GuestStatus.Prisoner);
					}
					if (building_Bed == null)
					{
						Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg, MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						Job job = JobMaker.MakeJob(JobDefOf.Arrest, pTarg, building_Bed);
						job.count = 1;
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						if (pTarg.Faction != null && ((pTarg.Faction != Faction.OfPlayer && !pTarg.Faction.Hidden) || pTarg.IsQuestLodger()))
						{
							TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, pTarg.GetAcceptArrestChance(pawn).ToStringPercent());
						}
					}
				};
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TryToArrest".Translate(item12.Thing.LabelCap, item12.Thing, pTarg.GetAcceptArrestChance(pawn).ToStringPercent()), action, MenuOptionPriority.High, null, item12.Thing), pawn, pTarg));
			}
		}
		foreach (Thing thing9 in clickCell.GetThingList(pawn.Map))
		{
			Thing t2 = thing9;
			if ((!t2.def.IsDrug && pawn.needs?.food == null) || t2.def.ingestible == null || !t2.def.ingestible.showIngestFloatOption || !pawn.RaceProps.CanEverEat(t2) || !t2.IngestibleNow)
			{
				continue;
			}
			string text = ((!t2.def.ingestible.ingestCommandString.NullOrEmpty()) ? ((string)t2.def.ingestible.ingestCommandString.Formatted(t2.LabelShort)) : ((string)"ConsumeThing".Translate(t2.LabelShort, t2)));
			if (!t2.IsSociallyProper(pawn))
			{
				text = text + ": " + "ReservedForPrisoners".Translate().CapitalizeFirst();
			}
			else if (FoodUtility.MoodFromIngesting(pawn, t2, t2.def) < 0f)
			{
				text = string.Format("{0}: ({1})", text, "WarningFoodDisliked".Translate());
			}
			if ((!t2.def.IsDrug || !ModsConfig.IdeologyActive || new HistoryEvent(HistoryEventDefOf.IngestedDrug, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo(out var opt, text) || PawnUtility.CanTakeDrugForDependency(pawn, t2.def)) && (!t2.def.IsNonMedicalDrug || !ModsConfig.IdeologyActive || new HistoryEvent(HistoryEventDefOf.IngestedRecreationalDrug, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo(out opt, text) || PawnUtility.CanTakeDrugForDependency(pawn, t2.def)) && (!t2.def.IsDrug || !ModsConfig.IdeologyActive || t2.def.ingestible.drugCategory != DrugCategory.Hard || new HistoryEvent(HistoryEventDefOf.IngestedHardDrug, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo(out opt, text)))
			{
				if (t2.def.IsNonMedicalDrug && !pawn.CanTakeDrug(t2.def))
				{
					opt = new FloatMenuOption(text + ": " + TraitDefOf.DrugDesire.DataAtDegree(-1).GetLabelCapFor(pawn), null);
				}
				else if (FoodUtility.InappropriateForTitle(t2.def, pawn, allowIfStarving: true))
				{
					opt = new FloatMenuOption(text + ": " + "FoodBelowTitleRequirements".Translate(pawn.royalty.MostSeniorTitle.def.GetLabelFor(pawn).CapitalizeFirst()).CapitalizeFirst(), null);
				}
				else if (!pawn.CanReach(t2, PathEndMode.OnCell, Danger.Deadly))
				{
					opt = new FloatMenuOption(text + ": " + "NoPath".Translate().CapitalizeFirst(), null);
				}
				else
				{
					MenuOptionPriority priority = ((t2 is Corpse) ? MenuOptionPriority.Low : MenuOptionPriority.Default);
					int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(t2, pawn, FoodUtility.WillIngestStackCountOf(pawn, t2.def, FoodUtility.NutritionForEater(pawn, t2)));
					opt = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
					{
						int maxAmountToPickup2 = FoodUtility.GetMaxAmountToPickup(t2, pawn, FoodUtility.WillIngestStackCountOf(pawn, t2.def, FoodUtility.NutritionForEater(pawn, t2)));
						if (maxAmountToPickup2 != 0)
						{
							t2.SetForbidden(value: false);
							Job job2 = JobMaker.MakeJob(JobDefOf.Ingest, t2);
							job2.count = maxAmountToPickup2;
							pawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
						}
					}, priority), pawn, t2);
					if (maxAmountToPickup == 0)
					{
						opt.action = null;
					}
				}
			}
			opts.Add(opt);
		}
		foreach (LocalTargetInfo item13 in GenUI.TargetsAt(clickPos, TargetingParameters.ForQuestPawnsWhoWillJoinColony(pawn), thingsOnly: true))
		{
			Pawn toHelpPawn = (Pawn)item13.Thing;
			FloatMenuOption item = (pawn.CanReach(item13, PathEndMode.Touch, Danger.Deadly) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(toHelpPawn.IsPrisoner ? "FreePrisoner".Translate() : "OfferHelp".Translate(), delegate
			{
				pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.OfferHelp, toHelpPawn), JobTag.Misc);
			}, MenuOptionPriority.RescueOrCapture, null, toHelpPawn), pawn, toHelpPawn) : new FloatMenuOption("CannotGoNoPath".Translate(), null));
			opts.Add(item);
		}
		ChildcareUtility.BreastfeedFailReason? reason;
		if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
		{
			List<Thing> thingList = clickCell.GetThingList(pawn.Map);
			foreach (Thing item14 in thingList)
			{
				Corpse corpse;
				if ((corpse = item14 as Corpse) == null || !corpse.IsInValidStorage())
				{
					continue;
				}
				StoragePriority priority2 = StoreUtility.CurrentHaulDestinationOf(corpse).GetStoreSettings().Priority;
				Building_Grave grave;
				if (StoreUtility.TryFindBestBetterNonSlotGroupStorageFor(corpse, pawn, pawn.Map, priority2, Faction.OfPlayer, out var haulDestination, acceptSamePriority: true) && haulDestination.GetStoreSettings().Priority == priority2 && (grave = haulDestination as Building_Grave) != null)
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PrioritizeGeneric".Translate("Burying".Translate(), corpse.Label).CapitalizeFirst(), delegate
					{
						pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToContainerJob(pawn, corpse, grave), JobTag.Misc);
					}), pawn, new LocalTargetInfo(corpse)));
				}
			}
			foreach (Thing item15 in thingList)
			{
				Corpse corpse2 = item15 as Corpse;
				if (corpse2 == null)
				{
					continue;
				}
				Building_GibbetCage cage = Building_GibbetCage.FindGibbetCageFor(corpse2, pawn);
				if (cage != null)
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PlaceIn".Translate(corpse2, cage), delegate
					{
						pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToContainerJob(pawn, corpse2, cage), JobTag.Misc);
					}), pawn, new LocalTargetInfo(corpse2)));
				}
				if (ModsConfig.BiotechActive && corpse2.InnerPawn.health.hediffSet.HasHediff(HediffDefOf.MechlinkImplant))
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Extract".Translate() + " " + HediffDefOf.MechlinkImplant.label, delegate
					{
						Job job3 = JobMaker.MakeJob(JobDefOf.RemoveMechlink, corpse2);
						pawn.jobs.TryTakeOrderedJob(job3, JobTag.Misc);
					}), pawn, new LocalTargetInfo(corpse2)));
				}
			}
			foreach (LocalTargetInfo item16 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
			{
				Pawn victim = (Pawn)item16.Thing;
				if (!HealthAIUtility.CanRescueNow(pawn, victim, forced: true) || victim.mindState.WillJoinColonyIfRescued)
				{
					continue;
				}
				if (!victim.IsPrisonerOfColony && !victim.IsSlaveOfColony && !victim.IsColonyMech)
				{
					bool isBaby = ChildcareUtility.CanSuckle(victim, out reason);
					if (victim.Faction == Faction.OfPlayer || victim.Faction == null || !victim.Faction.HostileTo(Faction.OfPlayer) || isBaby)
					{
						FloatMenuOption floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption((HealthAIUtility.ShouldSeekMedicalRest(victim) || !victim.ageTracker.CurLifeStage.alwaysDowned) ? "Rescue".Translate(victim.LabelCap, victim) : "PutSomewhereSafe".Translate(victim.LabelCap, victim), delegate
						{
							if (isBaby)
							{
								pawn.jobs.TryTakeOrderedJob(ChildcareUtility.MakeBringBabyToSafetyJob(pawn, victim), JobTag.Misc);
							}
							else
							{
								Building_Bed building_Bed2 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: false, null);
								if (building_Bed2 == null)
								{
									building_Bed2 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: true, null);
								}
								if (building_Bed2 == null)
								{
									string text2 = ((!victim.RaceProps.Animal) ? ((string)"NoNonPrisonerBed".Translate()) : ((string)"NoAnimalBed".Translate()));
									Messages.Message("CannotRescue".Translate() + ": " + text2, victim, MessageTypeDefOf.RejectInput, historical: false);
								}
								else
								{
									Job job4 = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed2);
									job4.count = 1;
									pawn.jobs.TryTakeOrderedJob(job4, JobTag.Misc);
									PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
								}
							}
						}, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim);
						if (!isBaby)
						{
							string key = (victim.RaceProps.Animal ? "NoAnimalBed" : "NoNonPrisonerBed");
							string cannot = string.Format("{0}: {1}", "CannotRescue".Translate(), key.Translate().CapitalizeFirst());
							ValidateTakeToBedOption(pawn, victim, floatMenuOption, cannot, null);
						}
						opts.Add(floatMenuOption);
					}
				}
				if (victim.IsSlaveOfColony && !victim.InMentalState)
				{
					FloatMenuOption floatMenuOption2 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ReturnToSlaveBed".Translate(), delegate
					{
						Building_Bed building_Bed3 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Slave);
						if (building_Bed3 == null)
						{
							building_Bed3 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: true, GuestStatus.Slave);
						}
						if (building_Bed3 == null)
						{
							Messages.Message(string.Format("{0}: {1}", "CannotRescue".Translate(), "NoSlaveBed".Translate()), victim, MessageTypeDefOf.RejectInput, historical: false);
						}
						else
						{
							Job job5 = JobMaker.MakeJob(JobDefOf.Rescue, victim, building_Bed3);
							job5.count = 1;
							pawn.jobs.TryTakeOrderedJob(job5, JobTag.Misc);
							PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
						}
					}, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim);
					string cannot2 = string.Format("{0}: {1}", "CannotRescue".Translate(), "NoSlaveBed".Translate());
					ValidateTakeToBedOption(pawn, victim, floatMenuOption2, cannot2, GuestStatus.Slave);
					opts.Add(floatMenuOption2);
				}
				if (!victim.CanBeCaptured())
				{
					continue;
				}
				TaggedString taggedString = "Capture".Translate(victim.LabelCap, victim);
				if (!victim.guest.Recruitable)
				{
					taggedString += " (" + "Unrecruitable".Translate() + ")";
				}
				if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.Hidden && !victim.Faction.HostileTo(Faction.OfPlayer) && !victim.IsPrisonerOfColony)
				{
					taggedString += ": " + "AngersFaction".Translate().CapitalizeFirst();
				}
				FloatMenuOption floatMenuOption3 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString, delegate
				{
					Building_Bed building_Bed4 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: false, GuestStatus.Prisoner);
					if (building_Bed4 == null)
					{
						building_Bed4 = RestUtility.FindBedFor(victim, pawn, checkSocialProperness: false, ignoreOtherReservations: true, GuestStatus.Prisoner);
					}
					if (building_Bed4 == null)
					{
						Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						Job job6 = JobMaker.MakeJob(JobDefOf.Capture, victim, building_Bed4);
						job6.count = 1;
						pawn.jobs.TryTakeOrderedJob(job6, JobTag.Misc);
						PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
						if (victim.Faction != null && victim.Faction != Faction.OfPlayer && !victim.Faction.Hidden && !victim.Faction.HostileTo(Faction.OfPlayer) && !victim.IsPrisonerOfColony)
						{
							Messages.Message("MessageCapturingWillAngerFaction".Translate(victim.Named("PAWN")).AdjustedFor(victim), victim, MessageTypeDefOf.CautionInput, historical: false);
						}
					}
				}, MenuOptionPriority.RescueOrCapture, null, victim), pawn, victim);
				string cannot3 = string.Format("{0}: {1}", "CannotCapture".Translate(), "NoPrisonerBed".Translate());
				ValidateTakeToBedOption(pawn, victim, floatMenuOption3, cannot3, GuestStatus.Prisoner);
				opts.Add(floatMenuOption3);
			}
			foreach (LocalTargetInfo item17 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
			{
				LocalTargetInfo localTargetInfo = item17;
				Pawn victim2 = (Pawn)localTargetInfo.Thing;
				if (!victim2.Downed || !pawn.CanReserveAndReach(victim2, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) || Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn, ignoreOtherReservations: true) == null)
				{
					continue;
				}
				string text3 = "CarryToCryptosleepCasket".Translate(localTargetInfo.Thing.LabelCap, localTargetInfo.Thing);
				JobDef jDef = JobDefOf.CarryToCryptosleepCasket;
				Action action2 = delegate
				{
					Building_CryptosleepCasket building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn);
					if (building_CryptosleepCasket == null)
					{
						building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim2, pawn, ignoreOtherReservations: true);
					}
					if (building_CryptosleepCasket == null)
					{
						Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim2, MessageTypeDefOf.RejectInput, historical: false);
					}
					else
					{
						Job job7 = JobMaker.MakeJob(jDef, victim2, building_CryptosleepCasket);
						job7.count = 1;
						pawn.jobs.TryTakeOrderedJob(job7, JobTag.Misc);
					}
				};
				if (victim2.IsQuestLodger())
				{
					text3 += " (" + "CryptosleepCasketGuestsNotAllowed".Translate() + ")";
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text3, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
				}
				else if (victim2.GetExtraHostFaction() != null)
				{
					text3 += " (" + "CryptosleepCasketGuestPrisonersNotAllowed".Translate() + ")";
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text3, null, MenuOptionPriority.Default, null, victim2), pawn, victim2));
				}
				else
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text3, action2, MenuOptionPriority.Default, null, victim2), pawn, victim2));
				}
			}
			if (ModsConfig.AnomalyActive && pawn.ageTracker.AgeBiologicalYears >= 10)
			{
				foreach (LocalTargetInfo item18 in GenUI.TargetsAt(clickPos, TargetingParameters.ForEntityCapture(), thingsOnly: true))
				{
					Thing studyTarget = item18.Thing;
					CompHoldingPlatformTarget holdComp = studyTarget.TryGetComp<CompHoldingPlatformTarget>();
					if (holdComp == null || !holdComp.StudiedAtHoldingPlatform || !holdComp.CanBeCaptured)
					{
						continue;
					}
					if (!pawn.CanReserveAndReach(studyTarget, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
					{
						opts.Add(new FloatMenuOption("CannotGenericWorkCustom".Translate("CaptureLower".Translate(studyTarget)) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						continue;
					}
					IEnumerable<Building_HoldingPlatform> enumerable2 = from x in pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_HoldingPlatform>()
						where !x.Occupied && pawn.CanReserveAndReach(x, PathEndMode.Touch, Danger.Deadly)
						select x;
					Thing building = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, enumerable2, PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Some), 9999f, null, delegate(Thing t)
					{
						CompEntityHolder compEntityHolder = t.TryGetComp<CompEntityHolder>();
						return (compEntityHolder != null && compEntityHolder.ContainmentStrength >= studyTarget.GetStatValue(StatDefOf.MinimumContainmentStrength)) ? (compEntityHolder.ContainmentStrength / Mathf.Max(studyTarget.PositionHeld.DistanceTo(t.Position), 1f)) : 0f;
					});
					if (building != null)
					{
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Capture".Translate(studyTarget.Label, studyTarget), delegate
						{
							if (!ContainmentUtility.SafeContainerExistsFor(studyTarget))
							{
								Messages.Message("MessageNoRoomWithMinimumContainmentStrength".Translate(studyTarget.Label), MessageTypeDefOf.ThreatSmall);
							}
							holdComp.targetHolder = building;
							Job job8 = JobMaker.MakeJob(JobDefOf.CarryToEntityHolder, building, studyTarget);
							job8.count = 1;
							pawn.jobs.TryTakeOrderedJob(job8, JobTag.Misc);
						}), pawn, studyTarget));
						if (enumerable2.Count() > 1)
						{
							opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Capture".Translate(studyTarget.Label, studyTarget) + " (" + "ChooseEntityHolder".Translate() + "...)", delegate
							{
								StudyUtility.TargetHoldingPlatformForEntity(pawn, studyTarget);
							}), pawn, studyTarget));
						}
					}
					else
					{
						opts.Add(new FloatMenuOption("CannotGenericWorkCustom".Translate("CaptureLower".Translate(studyTarget)) + ": " + "NoHoldingPlatformsAvailable".Translate().CapitalizeFirst(), null));
					}
				}
				foreach (LocalTargetInfo item19 in GenUI.TargetsAt(clickPos, TargetingParameters.ForHeldEntity(), thingsOnly: true))
				{
					Building_HoldingPlatform holdingPlatform;
					if ((holdingPlatform = item19.Thing as Building_HoldingPlatform) == null)
					{
						continue;
					}
					Pawn heldPawn = holdingPlatform.HeldPawn;
					if (heldPawn != null && pawn.CanReserveAndReach(holdingPlatform, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true) && GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_HoldingPlatform>(), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Some), 9999f, delegate(Thing b)
					{
						if (!(b is Building_HoldingPlatform building_HoldingPlatform))
						{
							return false;
						}
						if (building_HoldingPlatform.Occupied)
						{
							return false;
						}
						return pawn.CanReserve(building_HoldingPlatform) ? true : false;
					}, delegate(Thing t)
					{
						CompEntityHolder compEntityHolder2 = t.TryGetComp<CompEntityHolder>();
						return (compEntityHolder2 != null && compEntityHolder2.ContainmentStrength >= heldPawn.GetStatValue(StatDefOf.MinimumContainmentStrength)) ? (compEntityHolder2.ContainmentStrength / Mathf.Max(heldPawn.PositionHeld.DistanceTo(t.Position), 1f)) : 0f;
					}) != null)
					{
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TransferEntity".Translate(heldPawn) + " (" + "ChooseEntityHolder".Translate() + "...)", delegate
						{
							StudyUtility.TargetHoldingPlatformForEntity(pawn, heldPawn, transferBetweenPlatforms: true, holdingPlatform);
						}), pawn, holdingPlatform));
					}
				}
			}
			if (ModsConfig.IdeologyActive)
			{
				foreach (LocalTargetInfo item20 in GenUI.TargetsAt(clickPos, TargetingParameters.ForCarryToBiosculpterPod(pawn), thingsOnly: true))
				{
					Pawn pawn3 = (Pawn)item20.Thing;
					if ((pawn3.IsColonist && pawn3.Downed) || pawn3.IsPrisonerOfColony)
					{
						CompBiosculpterPod.AddCarryToPodJobs(opts, pawn, pawn3);
					}
				}
			}
			if (ModsConfig.RoyaltyActive)
			{
				foreach (LocalTargetInfo item21 in GenUI.TargetsAt(clickPos, TargetingParameters.ForShuttle(pawn), thingsOnly: true))
				{
					LocalTargetInfo localTargetInfo2 = item21;
					Pawn victim3 = (Pawn)localTargetInfo2.Thing;
					if (!victim3.Spawned)
					{
						continue;
					}
					Thing shuttleThing = GenClosest.ClosestThingReachable(victim3.Position, victim3.Map, ThingRequest.ForDef(ThingDefOf.Shuttle), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, IsValidShuttle);
					if (shuttleThing != null)
					{
						if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Hauling))
						{
							opts.Add(new FloatMenuOption("CannotLoadIntoShuttle".Translate(shuttleThing) + ": " + "Incapable".Translate().CapitalizeFirst(), null));
						}
						else if (pawn.CanReserveAndReach(victim3, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
						{
							opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CarryToShuttle".Translate(localTargetInfo2.Thing), CarryToShuttleAct), pawn, victim3));
						}
					}
					void CarryToShuttleAct()
					{
						CompShuttle compShuttle = shuttleThing.TryGetComp<CompShuttle>();
						if (!compShuttle.LoadingInProgressOrReadyToLaunch)
						{
							TransporterUtility.InitiateLoading(Gen.YieldSingle(compShuttle.Transporter));
						}
						Job job37 = JobMaker.MakeJob(JobDefOf.HaulToTransporter, victim3, shuttleThing);
						job37.ignoreForbidden = true;
						job37.count = 1;
						pawn.jobs.TryTakeOrderedJob(job37, JobTag.Misc);
					}
					bool IsValidShuttle(Thing thing)
					{
						return thing.TryGetComp<CompShuttle>()?.IsAllowedNow(victim3) ?? false;
					}
				}
			}
			if (ModsConfig.IdeologyActive)
			{
				foreach (Thing thing2 in thingList)
				{
					CompHackable compHackable = thing2.TryGetComp<CompHackable>();
					if (compHackable == null)
					{
						continue;
					}
					if (compHackable.IsHacked)
					{
						opts.Add(new FloatMenuOption("CannotHack".Translate(thing2.Label) + ": " + "AlreadyHacked".Translate(), null));
					}
					else if (!HackUtility.IsCapableOfHacking(pawn))
					{
						opts.Add(new FloatMenuOption("CannotHack".Translate(thing2.Label) + ": " + "IncapableOfHacking".Translate(), null));
					}
					else if (!pawn.CanReach(thing2, PathEndMode.ClosestTouch, Danger.Deadly))
					{
						opts.Add(new FloatMenuOption("CannotHack".Translate(thing2.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					}
					else if (thing2.def == ThingDefOf.AncientEnemyTerminal)
					{
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Hack".Translate(thing2.Label), delegate
						{
							Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmHackEnenyTerminal".Translate(ThingDefOf.AncientEnemyTerminal.label), delegate
							{
								pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Hack, thing2), JobTag.Misc);
							}));
						}), pawn, new LocalTargetInfo(thing2)));
					}
					else
					{
						TaggedString taggedString2 = ((thing2.def == ThingDefOf.AncientCommsConsole) ? "Hack".Translate("ToDropSupplies".Translate()) : "Hack".Translate(thing2.Label));
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString2, delegate
						{
							pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Hack, thing2), JobTag.Misc);
						}), pawn, new LocalTargetInfo(thing2)));
					}
				}
				foreach (LocalTargetInfo thing3 in GenUI.TargetsAt(clickPos, TargetingParameters.ForBuilding(ThingDefOf.ArchonexusCore)))
				{
					if (!pawn.CanReach(thing3, PathEndMode.InteractionCell, Danger.Deadly))
					{
						opts.Add(new FloatMenuOption("CannotInvoke".Translate("Power".Translate()) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						continue;
					}
					if (!((Building_ArchonexusCore)(Thing)thing3).CanActivateNow)
					{
						opts.Add(new FloatMenuOption("CannotInvoke".Translate("Power".Translate()) + ": " + "AlreadyInvoked".Translate(), null));
						continue;
					}
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Invoke".Translate("Power".Translate()), delegate
					{
						pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.ActivateArchonexusCore, thing3), JobTag.Misc);
					}), pawn, thing3));
				}
			}
			if (ModsConfig.IdeologyActive)
			{
				foreach (Thing thing4 in thingList)
				{
					CompRelicContainer container = thing4.TryGetComp<CompRelicContainer>();
					if (container == null)
					{
						continue;
					}
					if (container.Full)
					{
						string text4 = "ExtractRelic".Translate(container.ContainedThing.Label);
						if (!StoreUtility.TryFindBestBetterStorageFor(container.ContainedThing, pawn, pawn.Map, StoragePriority.Unstored, pawn.Faction, out var foundCell, out var _))
						{
							opts.Add(new FloatMenuOption(text4 + " (" + HaulAIUtility.NoEmptyPlaceLowerTrans + ")", null));
						}
						else
						{
							Job job9 = JobMaker.MakeJob(JobDefOf.ExtractRelic, thing4, container.ContainedThing, foundCell);
							job9.count = 1;
							opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text4, delegate
							{
								pawn.jobs.TryTakeOrderedJob(job9, JobTag.Misc);
							}), pawn, new LocalTargetInfo(thing4)));
						}
					}
					else
					{
						IEnumerable<Thing> enumerable3 = pawn.Map.listerThings.AllThings.Where((Thing x) => CompRelicContainer.IsRelic(x) && pawn.CanReach(x, PathEndMode.ClosestTouch, Danger.Deadly));
						if (!enumerable3.Any())
						{
							opts.Add(new FloatMenuOption("NoRelicToInstall".Translate(), null));
						}
						else
						{
							foreach (Thing item22 in enumerable3)
							{
								Job job10 = JobMaker.MakeJob(JobDefOf.InstallRelic, item22, thing4, thing4.InteractionCell);
								job10.count = 1;
								opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("InstallRelic".Translate(item22.Label), delegate
								{
									pawn.jobs.TryTakeOrderedJob(job10, JobTag.Misc);
								}), pawn, new LocalTargetInfo(thing4)));
							}
						}
					}
					if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan() && container.Full)
					{
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ExtractRelicToInventory".Translate(container.ContainedThing.Label, 300.ToStringTicksToPeriod()), delegate
						{
							Job job11 = JobMaker.MakeJob(JobDefOf.ExtractToInventory, thing4, container.ContainedThing, thing4.InteractionCell);
							job11.count = 1;
							pawn.jobs.TryTakeOrderedJob(job11, JobTag.Misc);
						}), pawn, new LocalTargetInfo(thing4)));
					}
				}
				foreach (Thing item23 in thingList)
				{
					if (!CompRelicContainer.IsRelic(item23))
					{
						continue;
					}
					IEnumerable<Thing> searchSet = from x in item23.Map.listerThings.ThingsOfDef(ThingDefOf.Reliquary)
						where x.TryGetComp<CompRelicContainer>().ContainedThing == null
						select x;
					Thing thing5 = GenClosest.ClosestThing_Global_Reachable(item23.Position, item23.Map, searchSet, PathEndMode.Touch, TraverseParms.For(pawn), 9999f, (Thing t) => pawn.CanReserve(t));
					if (thing5 == null)
					{
						opts.Add(new FloatMenuOption("InstallInReliquary".Translate() + " (" + "NoEmptyReliquary".Translate() + ")", null));
						continue;
					}
					Job job12 = JobMaker.MakeJob(JobDefOf.InstallRelic, item23, thing5, thing5.InteractionCell);
					job12.count = 1;
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("InstallInReliquary".Translate(), delegate
					{
						pawn.jobs.TryTakeOrderedJob(job12, JobTag.Misc);
					}), pawn, new LocalTargetInfo(item23)));
				}
			}
			if (ModsConfig.BiotechActive && MechanitorUtility.IsMechanitor(pawn))
			{
				foreach (Thing thing6 in thingList)
				{
					Pawn mech;
					if ((mech = thing6 as Pawn) == null || !mech.IsColonyMech)
					{
						continue;
					}
					if (mech.GetOverseer() != pawn)
					{
						if (!pawn.CanReach(mech, PathEndMode.Touch, Danger.Deadly))
						{
							opts.Add(new FloatMenuOption("CannotControlMech".Translate(mech.LabelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						}
						else if (!MechanitorUtility.CanControlMech(pawn, mech))
						{
							AcceptanceReport acceptanceReport = MechanitorUtility.CanControlMech(pawn, mech);
							if (!acceptanceReport.Reason.NullOrEmpty())
							{
								opts.Add(new FloatMenuOption("CannotControlMech".Translate(mech.LabelShort) + ": " + acceptanceReport.Reason, null));
							}
						}
						else
						{
							opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ControlMech".Translate(mech.LabelShort), delegate
							{
								Job job13 = JobMaker.MakeJob(JobDefOf.ControlMech, thing6);
								pawn.jobs.TryTakeOrderedJob(job13, JobTag.Misc);
							}), pawn, new LocalTargetInfo(thing6)));
						}
						opts.Add(new FloatMenuOption("CannotDisassembleMech".Translate(mech.LabelCap) + ": " + "MustBeOverseer".Translate().CapitalizeFirst(), null));
					}
					else
					{
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DisconnectMech".Translate(mech.LabelShort), delegate
						{
							MechanitorUtility.ForceDisconnectMechFromOverseer(mech);
						}, MenuOptionPriority.Low, null, null, 0f, null, null, playSelectionSound: true, -10), pawn, new LocalTargetInfo(thing6)));
						if (!mech.IsFighting())
						{
							opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DisassembleMech".Translate(mech.LabelCap), delegate
							{
								Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDisassemblingMech".Translate(mech.LabelCap) + ":\n" + (from x in MechanitorUtility.IngredientsFromDisassembly(mech.def)
									select x.Summary).ToLineList("  - "), delegate
								{
									pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DisassembleMech, thing6), JobTag.Misc);
								}, destructive: true));
							}, MenuOptionPriority.Low, null, null, 0f, null, null, playSelectionSound: true, -20), pawn, new LocalTargetInfo(thing6)));
						}
					}
					if (!pawn.Drafted || !MechRepairUtility.CanRepair(mech))
					{
						continue;
					}
					if (!pawn.CanReach(mech, PathEndMode.Touch, Danger.Deadly))
					{
						opts.Add(new FloatMenuOption("CannotRepairMech".Translate(mech.LabelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						continue;
					}
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RepairThing".Translate(mech.LabelShort), delegate
					{
						Job job14 = JobMaker.MakeJob(JobDefOf.RepairMech, mech);
						pawn.jobs.TryTakeOrderedJob(job14, JobTag.Misc);
					}), pawn, new LocalTargetInfo(thing6)));
				}
			}
			if (ModsConfig.BiotechActive)
			{
				foreach (Thing item24 in thingList)
				{
					Pawn p;
					if ((p = item24 as Pawn) == null || !p.IsSelfShutdown())
					{
						continue;
					}
					Building_MechCharger charger = JobGiver_GetEnergy_Charger.GetClosestCharger(p, pawn, forced: false);
					if (charger == null)
					{
						charger = JobGiver_GetEnergy_Charger.GetClosestCharger(p, pawn, forced: true);
					}
					if (charger == null)
					{
						opts.Add(new FloatMenuOption("CannotCarryToRecharger".Translate(p.Named("PAWN")) + ": " + "CannotCarryToRechargerNoneAvailable".Translate(), null));
						continue;
					}
					if (!pawn.CanReach(charger, PathEndMode.Touch, Danger.Deadly))
					{
						opts.Add(new FloatMenuOption("CannotCarryToRecharger".Translate(p.Named("PAWN")) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						continue;
					}
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CarryToRechargerOrdered".Translate(p.Named("PAWN")), delegate
					{
						Job job15 = JobMaker.MakeJob(JobDefOf.HaulMechToCharger, p, charger, charger.InteractionCell);
						job15.count = 1;
						pawn.jobs.TryTakeOrderedJob(job15, JobTag.Misc);
					}), pawn, new LocalTargetInfo(p)));
				}
			}
		}
		if (ModsConfig.BiotechActive && pawn.CanDeathrest())
		{
			List<Thing> thingList2 = clickCell.GetThingList(pawn.Map);
			for (int i = 0; i < thingList2.Count; i++)
			{
				Building_Bed bed;
				if ((bed = thingList2[i] as Building_Bed) == null || !bed.def.building.bed_humanlike)
				{
					continue;
				}
				if (!pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotDeathrest".Translate().CapitalizeFirst() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}
				AcceptanceReport acceptanceReport2 = bed.CompAssignableToPawn.CanAssignTo(pawn);
				if (!acceptanceReport2.Accepted)
				{
					opts.Add(new FloatMenuOption("CannotDeathrest".Translate().CapitalizeFirst() + ": " + acceptanceReport2.Reason, null));
					continue;
				}
				if ((!bed.CompAssignableToPawn.HasFreeSlot || !RestUtility.BedOwnerWillShare(bed, pawn, pawn.guest.GuestStatus)) && !bed.IsOwner(pawn))
				{
					opts.Add(new FloatMenuOption("CannotDeathrest".Translate().CapitalizeFirst() + ": " + "AssignedToOtherPawn".Translate(bed).CapitalizeFirst(), null));
					continue;
				}
				bool flag2 = false;
				foreach (IntVec3 item25 in bed.OccupiedRect())
				{
					if (item25.GetRoof(bed.Map) == null)
					{
						flag2 = true;
						break;
					}
				}
				if (flag2)
				{
					opts.Add(new FloatMenuOption("CannotDeathrest".Translate().CapitalizeFirst() + ": " + "ThingIsSkyExposed".Translate(bed).CapitalizeFirst(), null));
				}
				else if (RestUtility.IsValidBedFor(bed, pawn, pawn, checkSocialProperness: true, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations: false, pawn.GuestStatus))
				{
					opts.Add(new FloatMenuOption("StartDeathrest".Translate(), delegate
					{
						Job job16 = JobMaker.MakeJob(JobDefOf.Deathrest, bed);
						job16.forceSleep = true;
						pawn.jobs.TryTakeOrderedJob(job16, JobTag.Misc);
					}));
				}
			}
		}
		if (ModsConfig.BiotechActive && pawn.IsBloodfeeder() && pawn.genes?.GetFirstGeneOfType<Gene_Hemogen>() != null)
		{
			foreach (LocalTargetInfo item26 in GenUI.TargetsAt(clickPos, TargetingParameters.ForBloodfeeding(pawn)))
			{
				Pawn targPawn = (Pawn)item26.Thing;
				if (!pawn.CanReach(targPawn, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotBloodfeedOn".Translate(targPawn.Named("PAWN")) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}
				AcceptanceReport acceptanceReport3 = JobGiver_GetHemogen.CanFeedOnPrisoner(pawn, targPawn);
				if (acceptanceReport3.Accepted)
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("BloodfeedOn".Translate(targPawn.Named("PAWN")), delegate
					{
						Job job17 = JobMaker.MakeJob(JobDefOf.PrisonerBloodfeed, targPawn);
						pawn.jobs.TryTakeOrderedJob(job17, JobTag.Misc);
					}), pawn, targPawn));
				}
				else if (!acceptanceReport3.Reason.NullOrEmpty())
				{
					opts.Add(new FloatMenuOption("CannotBloodfeedOn".Translate(targPawn.Named("PAWN")) + ": " + acceptanceReport3.Reason.CapitalizeFirst(), null));
				}
			}
		}
		if (ModsConfig.BiotechActive && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
		{
			foreach (LocalTargetInfo item27 in GenUI.TargetsAt(clickPos, TargetingParameters.ForCarryDeathresterToBed(pawn)))
			{
				Pawn targPawn2 = (Pawn)item27.Thing;
				if (targPawn2.InBed())
				{
					continue;
				}
				if (!pawn.CanReach(targPawn2, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotCarry".Translate(targPawn2) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}
				Thing bestBedOrCasket = GenClosest.ClosestThingReachable(targPawn2.PositionHeld, pawn.Map, ThingRequest.ForDef(ThingDefOf.DeathrestCasket), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing casket) => casket.Faction == Faction.OfPlayer && RestUtility.IsValidBedFor(casket, targPawn2, pawn, checkSocialProperness: true, allowMedBedEvenIfSetToNoCare: false, ignoreOtherReservations: false, targPawn2.GuestStatus));
				if (bestBedOrCasket == null)
				{
					bestBedOrCasket = RestUtility.FindBedFor(targPawn2, pawn, checkSocialProperness: false, ignoreOtherReservations: false, null);
				}
				if (bestBedOrCasket != null)
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CarryToSpecificThing".Translate(bestBedOrCasket), delegate
					{
						Job job18 = JobMaker.MakeJob(JobDefOf.DeliverToBed, targPawn2, bestBedOrCasket);
						job18.count = 1;
						pawn.jobs.TryTakeOrderedJob(job18, JobTag.Misc);
					}, MenuOptionPriority.RescueOrCapture, null, targPawn2), pawn, targPawn2));
				}
				else
				{
					opts.Add(new FloatMenuOption("CannotCarry".Translate(targPawn2) + ": " + "NoCasketOrBed".Translate(), null));
				}
			}
		}
		if (ModsConfig.BiotechActive && pawn.genes != null)
		{
			foreach (LocalTargetInfo item28 in GenUI.TargetsAt(clickPos, TargetingParameters.ForXenogermAbsorption(pawn), thingsOnly: true))
			{
				Pawn targPawn3 = (Pawn)item28.Thing;
				if (!pawn.CanReserveAndReach(targPawn3, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
				{
					continue;
				}
				FloatMenuOption item2 = (pawn.IsQuestLodger() ? new FloatMenuOption("CannotAbsorbXenogerm".Translate(targPawn3.Named("PAWN")) + ": " + "TemporaryFactionMember".Translate(pawn.Named("PAWN")), null) : (GeneUtility.SameXenotype(pawn, targPawn3) ? new FloatMenuOption("CannotAbsorbXenogerm".Translate(targPawn3.Named("PAWN")) + ": " + "SameXenotype".Translate(pawn.Named("PAWN")), null) : (targPawn3.health.hediffSet.HasHediff(HediffDefOf.XenogermLossShock) ? new FloatMenuOption("CannotAbsorbXenogerm".Translate(targPawn3.Named("PAWN")) + ": " + "XenogermLossShockPresent".Translate(targPawn3.Named("PAWN")), null) : (CompAbilityEffect_ReimplantXenogerm.PawnIdeoCanAcceptReimplant(targPawn3, pawn) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("AbsorbXenogerm".Translate(targPawn3.Named("PAWN")), delegate
				{
					if (targPawn3.IsPrisonerOfColony && !targPawn3.Downed)
					{
						Messages.Message("MessageTargetMustBeDownedToForceReimplant".Translate(targPawn3.Named("PAWN")), targPawn3, MessageTypeDefOf.RejectInput, historical: false);
					}
					else if (GeneUtility.PawnWouldDieFromReimplanting(targPawn3))
					{
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("WarningPawnWillDieFromReimplanting".Translate(targPawn3.Named("PAWN")), delegate
						{
							GeneUtility.GiveReimplantJob(pawn, targPawn3);
						}, destructive: true));
					}
					else
					{
						GeneUtility.GiveReimplantJob(pawn, targPawn3);
					}
				}), pawn, targPawn3) : new FloatMenuOption("CannotAbsorbXenogerm".Translate(targPawn3.Named("PAWN")) + ": " + "IdeoligionForbids".Translate(), null)))));
				opts.Add(item2);
			}
		}
		if (ModsConfig.BiotechActive && !pawn.Downed && !pawn.Drafted)
		{
			foreach (LocalTargetInfo item29 in GenUI.TargetsAt(clickPos, TargetingParameters.ForBabyCare(pawn), thingsOnly: true))
			{
				Pawn baby = (Pawn)item29.Thing;
				if (!ChildcareUtility.CanSuckle(baby, out reason))
				{
					continue;
				}
				FloatMenuOption item3;
				if (ChildcareUtility.CanBreastfeed(pawn, out reason))
				{
					if (!ChildcareUtility.HasBreastfeedCompatibleFactions(pawn, baby))
					{
						continue;
					}
					item3 = (ChildcareUtility.CanMomAutoBreastfeedBabyNow(pawn, baby, forced: true, out var reason2) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("BabyCareBreastfeed".Translate(baby.Named("BABY")), delegate
					{
						pawn.jobs.TryTakeOrderedJob(ChildcareUtility.MakeBreastfeedJob(baby), JobTag.Misc);
					}), pawn, baby) : new FloatMenuOption("BabyCareBreastfeedUnable".Translate(baby.Named("BABY")) + ": " + reason2.Value.Translate(pawn, pawn, baby).CapitalizeFirst(), null));
					opts.Add(item3);
				}
				if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(baby))
				{
					continue;
				}
				LocalTargetInfo safePlace = ChildcareUtility.SafePlaceForBaby(baby, pawn, ignoreOtherReservations: true);
				if (!safePlace.IsValid)
				{
					continue;
				}
				if (safePlace.Thing is Building_Bed building_Bed5)
				{
					if (baby.CurrentBed() == building_Bed5)
					{
						continue;
					}
				}
				else if (baby.Spawned && baby.Position == safePlace.Cell)
				{
					continue;
				}
				item3 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("CarryToSafePlace".Translate(baby.Named("BABY")), delegate
				{
					Job job19 = JobMaker.MakeJob(JobDefOf.BringBabyToSafety, baby, safePlace);
					job19.count = 1;
					pawn.jobs.TryTakeOrderedJob(job19, JobTag.Misc);
				}), pawn, baby);
				if (!ChildcareUtility.CanHaulBabyNow(pawn, baby, ignoreOtherReservations: false, out var reason3))
				{
					if (pawn.MapHeld.reservationManager.TryGetReserver(baby, pawn.Faction, out var reserver))
					{
						item3.Label = string.Format("{0}: {1} {2}", "CannotCarryToSafePlace".Translate(), baby.LabelShort, "ReservedBy".Translate(reserver.LabelShort, reserver).Resolve().StripTags());
					}
					else
					{
						if (reason3.HasValue)
						{
							reason = reason3;
							if (reason == ChildcareUtility.BreastfeedFailReason.HaulerCannotReachBaby)
							{
								item3.Label = string.Format("{0}: {1}", "CannotCarryToSafePlace".Translate(), "NoPath".Translate().CapitalizeFirst());
								goto IL_3d49;
							}
						}
						item3.Label = string.Format("{0}: {1}", "CannotCarryToSafePlace".Translate(), "Incapable".Translate().CapitalizeFirst());
					}
					goto IL_3d49;
				}
				goto IL_3d51;
				IL_3d49:
				item3.Disabled = true;
				goto IL_3d51;
				IL_3d51:
				opts.Add(item3);
			}
		}
		if (!pawn.Drafted && ModsConfig.BiotechActive)
		{
			foreach (LocalTargetInfo item30 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRomance(pawn), thingsOnly: true))
			{
				Pawn pawn4 = (Pawn)item30.Thing;
				if (!pawn4.Drafted && !ChildcareUtility.CanSuckle(pawn4, out reason))
				{
					FloatMenuOption option;
					float chance;
					bool flag3 = RelationsUtility.RomanceOption(pawn, pawn4, out option, out chance);
					if (option != null)
					{
						option.Label = (flag3 ? "CanRomance" : "CannotRomance").Translate(option.Label);
						opts.Add(option);
					}
				}
			}
		}
		foreach (LocalTargetInfo item31 in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), thingsOnly: true))
		{
			LocalTargetInfo stripTarg = item31;
			FloatMenuOption item4 = (pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly) ? ((stripTarg.Pawn != null && stripTarg.Pawn.HasExtraHomeFaction()) ? new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null) : (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Strip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing), delegate
			{
				stripTarg.Thing.SetForbidden(value: false, warnOnFail: false);
				pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Strip, stripTarg), JobTag.Misc);
				StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(stripTarg.Thing);
			}), pawn, stripTarg) : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "Incapable".Translate().CapitalizeFirst(), null))) : new FloatMenuOption("CannotStrip".Translate(stripTarg.Thing.LabelCap, stripTarg.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
			opts.Add(item4);
		}
		if (pawn.equipment != null)
		{
			List<Thing> thingList3 = clickCell.GetThingList(pawn.Map);
			for (int j = 0; j < thingList3.Count; j++)
			{
				if (thingList3[j].TryGetComp<CompEquippable>() == null)
				{
					continue;
				}
				ThingWithComps equipment = (ThingWithComps)thingList3[j];
				string labelShort = equipment.LabelShort;
				FloatMenuOption item5;
				string cantReason;
				if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
				{
					item5 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn), null);
				}
				else if (equipment.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
				{
					item5 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfShootingLower".Translate(pawn), null);
				}
				else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					item5 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
				}
				else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
				{
					item5 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "Incapable".Translate().CapitalizeFirst(), null);
				}
				else if (equipment.IsBurning())
				{
					item5 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "BurningLower".Translate(), null);
				}
				else if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(equipment, pawn))
				{
					item5 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null);
				}
				else if (!EquipmentUtility.CanEquip(equipment, pawn, out cantReason, checkBonded: false))
				{
					item5 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + cantReason.CapitalizeFirst(), null);
				}
				else
				{
					string text5 = "Equip".Translate(labelShort);
					if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
					{
						text5 += " " + "EquipWarningBrawler".Translate();
					}
					if (EquipmentUtility.AlreadyBondedToWeapon(equipment, pawn))
					{
						text5 += " " + "BladelinkAlreadyBonded".Translate();
						TaggedString dialogText = "BladelinkAlreadyBondedDialog".Translate(pawn.Named("PAWN"), equipment.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"));
						item5 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text5, delegate
						{
							Find.WindowStack.Add(new Dialog_MessageBox(dialogText));
						}, MenuOptionPriority.High), pawn, equipment);
					}
					else
					{
						item5 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text5, delegate
						{
							string personaWeaponConfirmationText = EquipmentUtility.GetPersonaWeaponConfirmationText(equipment, pawn);
							if (!personaWeaponConfirmationText.NullOrEmpty())
							{
								Find.WindowStack.Add(new Dialog_MessageBox(personaWeaponConfirmationText, "Yes".Translate(), delegate
								{
									Equip();
								}, "No".Translate()));
							}
							else
							{
								Equip();
							}
						}, MenuOptionPriority.High), pawn, equipment);
					}
				}
				opts.Add(item5);
				void Equip()
				{
					equipment.SetForbidden(value: false);
					pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, equipment), JobTag.Misc);
					FleckMaker.Static(equipment.DrawPos, equipment.MapHeld, FleckDefOf.FeedbackEquip);
					PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
				}
			}
		}
		foreach (Pair<IReloadableComp, Thing> item32 in ReloadableUtility.FindPotentiallyReloadableGear(pawn, clickCell.GetThingList(pawn.Map)))
		{
			IReloadableComp reloadable = item32.First;
			Thing second = item32.Second;
			ThingComp thingComp = reloadable as ThingComp;
			string text6 = "Reload".Translate(thingComp.parent.Named("GEAR"), NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")) + " (" + reloadable.LabelRemaining + ")";
			if (!pawn.CanReach(second, PathEndMode.ClosestTouch, Danger.Deadly))
			{
				opts.Add(new FloatMenuOption(text6 + ": " + "NoPath".Translate().CapitalizeFirst(), null));
				continue;
			}
			if (!reloadable.NeedsReload(allowForceReload: true))
			{
				opts.Add(new FloatMenuOption(text6 + ": " + "ReloadFull".Translate(), null));
				continue;
			}
			List<Thing> chosenAmmo;
			if ((chosenAmmo = ReloadableUtility.FindEnoughAmmo(pawn, second.Position, reloadable, forceReload: true)) == null)
			{
				opts.Add(new FloatMenuOption(text6 + ": " + "ReloadNotEnough".Translate(), null));
				continue;
			}
			if (pawn.carryTracker.AvailableStackSpace(reloadable.AmmoDef) < reloadable.MinAmmoNeeded(allowForcedReload: true))
			{
				opts.Add(new FloatMenuOption(text6 + ": " + "ReloadCannotCarryEnough".Translate(NamedArgumentUtility.Named(reloadable.AmmoDef, "AMMO")), null));
				continue;
			}
			Action action3 = delegate
			{
				pawn.jobs.TryTakeOrderedJob(JobGiver_Reload.MakeReloadJob(reloadable, chosenAmmo), JobTag.Misc);
			};
			opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text6, action3), pawn, second));
		}
		if (pawn.apparel != null)
		{
			foreach (Thing item33 in pawn.Map.thingGrid.ThingsAt(clickCell))
			{
				Apparel apparel = item33 as Apparel;
				if (apparel == null)
				{
					continue;
				}
				string key2 = "CannotWear";
				string key3 = "ForceWear";
				if (apparel.def.apparel.LastLayer.IsUtilityLayer)
				{
					key2 = "CannotEquipApparel";
					key3 = "ForceEquipApparel";
				}
				string cantReason2;
				FloatMenuOption item6 = ((!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly)) ? new FloatMenuOption(key2.Translate(apparel.Label, apparel) + ": " + "NoPath".Translate().CapitalizeFirst(), null) : (apparel.IsBurning() ? new FloatMenuOption(key2.Translate(apparel.Label, apparel) + ": " + "Burning".Translate(), null) : (pawn.apparel.WouldReplaceLockedApparel(apparel) ? new FloatMenuOption(key2.Translate(apparel.Label, apparel) + ": " + "WouldReplaceLockedApparel".Translate().CapitalizeFirst(), null) : ((!ApparelUtility.HasPartsToWear(pawn, apparel.def)) ? new FloatMenuOption(key2.Translate(apparel.Label, apparel) + ": " + "CannotWearBecauseOfMissingBodyParts".Translate().CapitalizeFirst(), null) : (EquipmentUtility.CanEquip(apparel, pawn, out cantReason2) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(key3.Translate(apparel.LabelShort, apparel), delegate
				{
					Action action4 = delegate
					{
						apparel.SetForbidden(value: false);
						Job job20 = JobMaker.MakeJob(JobDefOf.Wear, apparel);
						pawn.jobs.TryTakeOrderedJob(job20, JobTag.Misc);
					};
					Apparel apparelReplacedByNewApparel = ApparelUtility.GetApparelReplacedByNewApparel(pawn, apparel);
					if (apparelReplacedByNewApparel == null || !ModsConfig.BiotechActive || !MechanitorUtility.TryConfirmBandwidthLossFromDroppingThing(pawn, apparelReplacedByNewApparel, action4))
					{
						action4();
					}
				}, MenuOptionPriority.High), pawn, apparel) : new FloatMenuOption(key2.Translate(apparel.Label, apparel) + ": " + cantReason2, null))))));
				opts.Add(item6);
			}
		}
		if (pawn.IsFormingCaravan())
		{
			foreach (Thing item7 in clickCell.GetItems(pawn.Map))
			{
				if (!item7.def.EverHaulable || !item7.def.canLoadIntoCaravan)
				{
					continue;
				}
				Pawn packTarget = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn) ?? pawn;
				JobDef jobDef = ((packTarget == pawn) ? JobDefOf.TakeInventory : JobDefOf.GiveToPackAnimal);
				if (!pawn.CanReach(item7, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item7.Label, item7) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}
				if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item7, 1))
				{
					opts.Add(new FloatMenuOption("CannotLoadIntoCaravan".Translate(item7.Label, item7) + ": " + "TooHeavy".Translate(), null));
					continue;
				}
				LordJob_FormAndSendCaravan lordJob = (LordJob_FormAndSendCaravan)pawn.GetLord().LordJob;
				float capacityLeft = CaravanFormingUtility.CapacityLeft(lordJob);
				if (item7.stackCount == 1)
				{
					float capacityLeft2 = capacityLeft - item7.GetStatValue(StatDefOf.Mass);
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravan".Translate(item7.Label, item7), capacityLeft2), delegate
					{
						item7.SetForbidden(value: false, warnOnFail: false);
						Job job21 = JobMaker.MakeJob(jobDef, item7);
						job21.count = 1;
						job21.checkEncumbrance = packTarget == pawn;
						pawn.jobs.TryTakeOrderedJob(job21, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item7));
					continue;
				}
				if (MassUtility.WillBeOverEncumberedAfterPickingUp(packTarget, item7, item7.stackCount))
				{
					opts.Add(new FloatMenuOption("CannotLoadIntoCaravanAll".Translate(item7.Label, item7) + ": " + "TooHeavy".Translate(), null));
				}
				else
				{
					float capacityLeft3 = capacityLeft - (float)item7.stackCount * item7.GetStatValue(StatDefOf.Mass);
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanAll".Translate(item7.Label, item7), capacityLeft3), delegate
					{
						item7.SetForbidden(value: false, warnOnFail: false);
						Job job22 = JobMaker.MakeJob(jobDef, item7);
						job22.count = item7.stackCount;
						job22.checkEncumbrance = packTarget == pawn;
						pawn.jobs.TryTakeOrderedJob(job22, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item7));
				}
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("LoadIntoCaravanSome".Translate(item7.LabelNoCount, item7), delegate
				{
					int to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(packTarget, item7), item7.stackCount);
					Dialog_Slider window = new Dialog_Slider(delegate(int val)
					{
						float capacityLeft4 = capacityLeft - (float)val * item7.GetStatValue(StatDefOf.Mass);
						return CaravanFormingUtility.AppendOverweightInfo("LoadIntoCaravanCount".Translate(item7.LabelNoCount, item7).Formatted(val), capacityLeft4);
					}, 1, to, delegate(int count)
					{
						item7.SetForbidden(value: false, warnOnFail: false);
						Job job23 = JobMaker.MakeJob(jobDef, item7);
						job23.count = count;
						job23.checkEncumbrance = packTarget == pawn;
						pawn.jobs.TryTakeOrderedJob(job23, JobTag.Misc);
					});
					Find.WindowStack.Add(window);
				}, MenuOptionPriority.High), pawn, item7));
			}
		}
		if (!pawn.IsFormingCaravan())
		{
			foreach (Thing item8 in clickCell.GetItems(pawn.Map))
			{
				if (!item8.def.EverHaulable || !PawnUtility.CanPickUp(pawn, item8.def) || (pawn.Map.IsPlayerHome && !JobGiver_DropUnusedInventory.ShouldKeepDrugInInventory(pawn, item8)))
				{
					continue;
				}
				if (!pawn.CanReach(item8, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotPickUp".Translate(item8.Label, item8) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}
				if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item8, 1))
				{
					opts.Add(new FloatMenuOption("CannotPickUp".Translate(item8.Label, item8) + ": " + "TooHeavy".Translate(), null));
					continue;
				}
				int maxAllowedToPickUp = PawnUtility.GetMaxAllowedToPickUp(pawn, item8.def);
				if (maxAllowedToPickUp == 0)
				{
					opts.Add(new FloatMenuOption("CannotPickUp".Translate(item8.Label, item8) + ": " + "MaxPickUpAllowed".Translate(item8.def.orderedTakeGroup.max, item8.def.orderedTakeGroup.label), null));
					continue;
				}
				if (item8.stackCount == 1 || maxAllowedToPickUp == 1)
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpOne".Translate(item8.LabelNoCount, item8), delegate
					{
						item8.SetForbidden(value: false, warnOnFail: false);
						Job job24 = JobMaker.MakeJob(JobDefOf.TakeInventory, item8);
						job24.count = 1;
						job24.checkEncumbrance = true;
						job24.takeInventoryDelay = 120;
						pawn.jobs.TryTakeOrderedJob(job24, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item8));
					continue;
				}
				if (maxAllowedToPickUp < item8.stackCount)
				{
					opts.Add(new FloatMenuOption("CannotPickUpAll".Translate(item8.Label, item8) + ": " + "MaxPickUpAllowed".Translate(item8.def.orderedTakeGroup.max, item8.def.orderedTakeGroup.label), null));
				}
				else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item8, item8.stackCount))
				{
					opts.Add(new FloatMenuOption("CannotPickUpAll".Translate(item8.Label, item8) + ": " + "TooHeavy".Translate(), null));
				}
				else
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpAll".Translate(item8.Label, item8), delegate
					{
						item8.SetForbidden(value: false, warnOnFail: false);
						Job job25 = JobMaker.MakeJob(JobDefOf.TakeInventory, item8);
						job25.count = item8.stackCount;
						job25.checkEncumbrance = true;
						job25.takeInventoryDelay = 120;
						pawn.jobs.TryTakeOrderedJob(job25, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item8));
				}
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PickUpSome".Translate(item8.LabelNoCount, item8), delegate
				{
					int b2 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(pawn, item8), item8.stackCount);
					int to2 = Mathf.Min(maxAllowedToPickUp, b2);
					Dialog_Slider window2 = new Dialog_Slider("PickUpCount".Translate(item8.LabelNoCount, item8), 1, to2, delegate(int count)
					{
						item8.SetForbidden(value: false, warnOnFail: false);
						Job job26 = JobMaker.MakeJob(JobDefOf.TakeInventory, item8);
						job26.count = count;
						job26.checkEncumbrance = true;
						job26.takeInventoryDelay = 120;
						pawn.jobs.TryTakeOrderedJob(job26, JobTag.Misc);
					});
					Find.WindowStack.Add(window2);
				}, MenuOptionPriority.High), pawn, item8));
			}
		}
		if (!pawn.Map.IsPlayerHome && !pawn.IsFormingCaravan())
		{
			foreach (Thing item9 in clickCell.GetItems(pawn.Map))
			{
				if (!item9.def.EverHaulable)
				{
					continue;
				}
				Pawn bestPackAnimal = GiveToPackAnimalUtility.UsablePackAnimalWithTheMostFreeSpace(pawn);
				if (bestPackAnimal == null)
				{
					continue;
				}
				if (!pawn.CanReach(item9, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item9.Label, item9) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
					continue;
				}
				if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item9, 1))
				{
					opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(item9.Label, item9) + ": " + "TooHeavy".Translate(), null));
					continue;
				}
				if (item9.stackCount == 1)
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimal".Translate(item9.Label, item9), delegate
					{
						item9.SetForbidden(value: false, warnOnFail: false);
						Job job27 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item9);
						job27.count = 1;
						pawn.jobs.TryTakeOrderedJob(job27, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item9));
					continue;
				}
				if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item9, item9.stackCount))
				{
					opts.Add(new FloatMenuOption("CannotGiveToPackAnimalAll".Translate(item9.Label, item9) + ": " + "TooHeavy".Translate(), null));
				}
				else
				{
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalAll".Translate(item9.Label, item9), delegate
					{
						item9.SetForbidden(value: false, warnOnFail: false);
						Job job28 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item9);
						job28.count = item9.stackCount;
						pawn.jobs.TryTakeOrderedJob(job28, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item9));
				}
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("GiveToPackAnimalSome".Translate(item9.LabelNoCount, item9), delegate
				{
					int to3 = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(bestPackAnimal, item9), item9.stackCount);
					Dialog_Slider window3 = new Dialog_Slider("GiveToPackAnimalCount".Translate(item9.LabelNoCount, item9), 1, to3, delegate(int count)
					{
						item9.SetForbidden(value: false, warnOnFail: false);
						Job job29 = JobMaker.MakeJob(JobDefOf.GiveToPackAnimal, item9);
						job29.count = count;
						pawn.jobs.TryTakeOrderedJob(job29, JobTag.Misc);
					});
					Find.WindowStack.Add(window3);
				}, MenuOptionPriority.High), pawn, item9));
			}
		}
		if (!pawn.Map.IsPlayerHome && pawn.Map.exitMapGrid.MapUsesExitGrid)
		{
			foreach (LocalTargetInfo item34 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), thingsOnly: true))
			{
				Pawn p2 = (Pawn)item34.Thing;
				if (p2.Faction != Faction.OfPlayer && !p2.IsPrisonerOfColony && !CaravanUtility.ShouldAutoCapture(p2, Faction.OfPlayer))
				{
					continue;
				}
				IntVec3 exitSpot;
				if (!pawn.CanReach(p2, PathEndMode.ClosestTouch, Danger.Deadly))
				{
					opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(p2.Label, p2) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
				}
				else if (pawn.Map.IsPocketMap)
				{
					if (!RCellFinder.TryFindExitPortal(pawn, out var portal))
					{
						opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(p2.Label, p2) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
						continue;
					}
					TaggedString taggedString3 = ((p2.Faction == Faction.OfPlayer || p2.IsPrisonerOfColony) ? "CarryToExit".Translate(p2.Label, p2) : "CarryToExitAndCapture".Translate(p2.Label, p2));
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString3, delegate
					{
						Job job30 = JobMaker.MakeJob(JobDefOf.CarryDownedPawnToPortal, portal, p2);
						job30.count = 1;
						pawn.jobs.TryTakeOrderedJob(job30, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item34));
				}
				else if (!RCellFinder.TryFindBestExitSpot(pawn, out exitSpot))
				{
					opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(p2.Label, p2) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
				}
				else
				{
					TaggedString taggedString4 = ((p2.Faction == Faction.OfPlayer || p2.IsPrisonerOfColony) ? "CarryToExit".Translate(p2.Label, p2) : "CarryToExitAndCapture".Translate(p2.Label, p2));
					opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(taggedString4, delegate
					{
						Job job31 = JobMaker.MakeJob(JobDefOf.CarryDownedPawnToExit, p2, exitSpot);
						job31.count = 1;
						job31.failIfCantJoinOrCreateCaravan = true;
						pawn.jobs.TryTakeOrderedJob(job31, JobTag.Misc);
					}, MenuOptionPriority.High), pawn, item34));
				}
			}
		}
		if (pawn.equipment != null && pawn.equipment.Primary != null && GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), thingsOnly: true).Any())
		{
			if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(pawn.equipment.Primary, pawn))
			{
				opts.Add(new FloatMenuOption("CannotDrop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null));
			}
			else
			{
				Action action5 = delegate
				{
					pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, pawn.equipment.Primary), JobTag.Misc);
				};
				opts.Add(new FloatMenuOption("Drop".Translate(pawn.equipment.Primary.Label, pawn.equipment.Primary), action5, MenuOptionPriority.Default, null, pawn));
			}
		}
		foreach (LocalTargetInfo item35 in GenUI.TargetsAt(clickPos, TargetingParameters.ForTrade(), thingsOnly: true))
		{
			LocalTargetInfo dest = item35;
			if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
			{
				opts.Add(new FloatMenuOption("CannotTrade".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
				continue;
			}
			if (pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
			{
				opts.Add(new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap), null));
				continue;
			}
			Pawn pTarg2 = (Pawn)dest.Thing;
			if (pTarg2.mindState.traderDismissed)
			{
				opts.Add(new FloatMenuOption("TraderDismissed".Translate(), null));
				continue;
			}
			if (!pawn.CanTradeWith(pTarg2.Faction, pTarg2.TraderKind).Accepted)
			{
				opts.Add(new FloatMenuOption("CannotTrade".Translate() + ": " + "MissingTitleAbility".Translate().CapitalizeFirst(), null));
			}
			else
			{
				Action action6 = delegate
				{
					Job job32 = JobMaker.MakeJob(JobDefOf.TradeWithPawn, pTarg2);
					job32.playerForced = true;
					pawn.jobs.TryTakeOrderedJob(job32, JobTag.Misc);
					PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
				};
				string text7 = "";
				if (pTarg2.Faction != null)
				{
					text7 = " (" + pTarg2.Faction.Name + ")";
				}
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TradeWith".Translate(pTarg2.LabelShort + ", " + pTarg2.TraderKind.label) + text7, action6, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pTarg2));
			}
			if (pTarg2.GetLord().LordJob is LordJob_TradeWithColony && !pTarg2.mindState.traderDismissed)
			{
				Action action7 = delegate
				{
					Job job33 = JobMaker.MakeJob(JobDefOf.DismissTrader, pTarg2);
					job33.playerForced = true;
					pawn.jobs.TryTakeOrderedJob(job33, JobTag.Misc);
				};
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DismissTrader".Translate(), action7, MenuOptionPriority.InitiateSocial, null, dest.Thing), pawn, pTarg2));
			}
		}
		foreach (LocalTargetInfo casket2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForOpen(pawn), thingsOnly: true))
		{
			if (!pawn.CanReach(casket2, PathEndMode.OnCell, Danger.Deadly))
			{
				opts.Add(new FloatMenuOption("CannotOpen".Translate(casket2.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
			}
			else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				opts.Add(new FloatMenuOption("CannotOpen".Translate(casket2.Thing) + ": " + "Incapable".Translate().CapitalizeFirst(), null));
			}
			else if (casket2.Thing.Map.designationManager.DesignationOn(casket2.Thing, DesignationDefOf.Open) == null)
			{
				opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("Open".Translate(casket2.Thing), delegate
				{
					Job job34 = JobMaker.MakeJob(JobDefOf.Open, casket2.Thing);
					job34.ignoreDesignations = true;
					pawn.jobs.TryTakeOrderedJob(job34, JobTag.Misc);
				}, MenuOptionPriority.High), pawn, casket2.Thing));
			}
		}
		if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && new TargetInfo(clickCell, pawn.Map).IsBurning())
		{
			FloatMenuOption item10;
			if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Firefighter))
			{
				WorkGiverDef fightFires = WorkGiverDefOf.FightFires;
				item10 = new FloatMenuOption(string.Format("{0}: {1}", "CannotGenericWorkCustom".Translate(fightFires.label), "IncapableOf".Translate().CapitalizeFirst() + " " + WorkTypeDefOf.Firefighter.gerundLabel), null);
			}
			else
			{
				item10 = new FloatMenuOption("ExtinguishFiresNearby".Translate(), delegate
				{
					Job job35 = JobMaker.MakeJob(JobDefOf.ExtinguishFiresNearby);
					foreach (Fire item36 in clickCell.GetFiresNearCell(pawn.Map))
					{
						job35.AddQueuedTarget(TargetIndex.A, item36);
					}
					pawn.jobs.TryTakeOrderedJob(job35, JobTag.Misc);
				});
			}
			opts.Add(item10);
		}
		if (!pawn.Drafted && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning))
		{
			Room room = clickCell.GetRoom(pawn.Map);
			if (room != null && room.ProperRoom && !room.PsychologicallyOutdoors && !room.TouchesMapEdge)
			{
				IEnumerable<Filth> filth = CleanRoomFilthUtility.GetRoomFilthCleanableByPawn(clickCell, pawn);
				if (!filth.EnumerableNullOrEmpty())
				{
					string roomRoleLabel = room.GetRoomRoleLabel();
					opts.Add(new FloatMenuOption("CleanRoom".Translate(roomRoleLabel), delegate
					{
						Job job36 = JobMaker.MakeJob(JobDefOf.Clean);
						foreach (Filth item37 in filth)
						{
							job36.AddQueuedTarget(TargetIndex.A, item37);
						}
						pawn.jobs.TryTakeOrderedJob(job36, JobTag.Misc);
					}));
				}
			}
		}
		foreach (Thing item38 in pawn.Map.thingGrid.ThingsAt(clickCell))
		{
			Thing thing7 = item38;
			CompSelectProxy compSelectProxy;
			if ((compSelectProxy = thing7.TryGetComp<CompSelectProxy>()) != null && compSelectProxy.thingToSelect != null)
			{
				thing7 = compSelectProxy.thingToSelect;
			}
			foreach (FloatMenuOption floatMenuOption4 in thing7.GetFloatMenuOptions(pawn))
			{
				cachedThings.Add(item38);
				opts.Add(floatMenuOption4);
			}
		}
		foreach (LocalTargetInfo item39 in GenUI.TargetsAt(clickPos, TargetingParameters.ForPawns(), thingsOnly: true))
		{
			if (cachedThings.Contains(item39.Pawn))
			{
				continue;
			}
			foreach (FloatMenuOption floatMenuOption5 in item39.Pawn.GetFloatMenuOptions(pawn))
			{
				cachedThings.Add(item39.Pawn);
				opts.Add(floatMenuOption5);
			}
		}
		cachedThings.Clear();
	}

	private static void ValidateTakeToBedOption(Pawn pawn, Pawn target, FloatMenuOption option, string cannot, GuestStatus? guestStatus = null)
	{
		Building_Bed building_Bed = RestUtility.FindBedFor(target, pawn, checkSocialProperness: false, ignoreOtherReservations: false, guestStatus);
		if (building_Bed != null)
		{
			return;
		}
		building_Bed = RestUtility.FindBedFor(target, pawn, checkSocialProperness: false, ignoreOtherReservations: true, guestStatus);
		if (building_Bed != null)
		{
			if (pawn.MapHeld.reservationManager.TryGetReserver(building_Bed, pawn.Faction, out var reserver))
			{
				option.Label = option.Label + " (" + building_Bed.def.label + " " + "ReservedBy".Translate(reserver.LabelShort, reserver).Resolve().StripTags() + ")";
			}
		}
		else
		{
			option.Disabled = true;
			option.Label = cannot;
		}
	}

	private static void AddUndraftedOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
	{
		if (equivalenceGroupTempStorage == null || equivalenceGroupTempStorage.Length != DefDatabase<WorkGiverEquivalenceGroupDef>.DefCount)
		{
			equivalenceGroupTempStorage = new FloatMenuOption[DefDatabase<WorkGiverEquivalenceGroupDef>.DefCount];
		}
		AddJobGiverWorkOrders(clickPos, pawn, opts, drafted: false);
	}

	private static void AddMutantOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
	{
		IntVec3 c = IntVec3.FromVector3(clickPos);
		foreach (Thing thing in c.GetThingList(pawn.Map))
		{
			Thing t = thing;
			if (t.def.ingestible == null || !t.def.ingestible.showIngestFloatOption || !pawn.RaceProps.CanEverEat(t) || !t.IngestibleNow || (pawn.needs?.food == null && !t.def.IsDrug))
			{
				continue;
			}
			string text = ((!t.def.ingestible.ingestCommandString.NullOrEmpty()) ? ((string)t.def.ingestible.ingestCommandString.Formatted(t.LabelShort)) : ((string)"ConsumeThing".Translate(t.LabelShort, t)));
			if (!t.IsSociallyProper(pawn))
			{
				text = text + ": " + "ReservedForPrisoners".Translate().CapitalizeFirst();
			}
			FloatMenuOption floatMenuOption;
			if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly))
			{
				floatMenuOption = new FloatMenuOption(text + ": " + "NoPath".Translate().CapitalizeFirst(), null);
			}
			else if (!t.def.IsDrug && !pawn.WillEat(t))
			{
				floatMenuOption = new FloatMenuOption(text + ": " + "FoodNotSuitable".Translate().CapitalizeFirst(), null);
			}
			else if (t.def.IsDrug && pawn.IsMutant && (!pawn.mutant.Def.canUseDrugs || !pawn.mutant.Def.drugWhitelist.Contains(t.def)))
			{
				floatMenuOption = new FloatMenuOption(text + ": " + "DrugNotSuitable".Translate().CapitalizeFirst(), null);
			}
			else
			{
				MenuOptionPriority priority = ((t is Corpse) ? MenuOptionPriority.Low : MenuOptionPriority.Default);
				int maxAmountToPickup = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, FoodUtility.NutritionForEater(pawn, t)));
				floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
				{
					int maxAmountToPickup2 = FoodUtility.GetMaxAmountToPickup(t, pawn, FoodUtility.WillIngestStackCountOf(pawn, t.def, FoodUtility.NutritionForEater(pawn, t)));
					if (maxAmountToPickup2 != 0)
					{
						t.SetForbidden(value: false);
						Job job = JobMaker.MakeJob(JobDefOf.Ingest, t);
						job.count = maxAmountToPickup2;
						pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}
				}, priority), pawn, t);
				if (maxAmountToPickup == 0)
				{
					floatMenuOption.action = null;
				}
			}
			opts.Add(floatMenuOption);
		}
		foreach (Thing item in pawn.Map.thingGrid.ThingsAt(c))
		{
			if (item is Building_Bed building_Bed)
			{
				FloatMenuOption bedRestFloatMenuOption = building_Bed.GetBedRestFloatMenuOption(pawn);
				if (bedRestFloatMenuOption != null)
				{
					opts.Add(bedRestFloatMenuOption);
				}
			}
		}
	}

	private static void AddJobGiverWorkOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool drafted)
	{
		if (pawn.thinker.TryGetMainTreeThinkNode<JobGiver_Work>() == null)
		{
			return;
		}
		IntVec3 clickCell = IntVec3.FromVector3(clickPos);
		foreach (Thing thing2 in clickCell.GetThingList(pawn.Map))
		{
			if (!thing2.Spawned)
			{
				continue;
			}
			Thing thing = thing2;
			CompSelectProxy compSelectProxy;
			if ((compSelectProxy = thing.TryGetComp<CompSelectProxy>()) != null && compSelectProxy.thingToSelect != null)
			{
				thing = compSelectProxy.thingToSelect;
			}
			bool flag = false;
			foreach (WorkTypeDef item in DefDatabase<WorkTypeDef>.AllDefsListForReading)
			{
				for (int i = 0; i < item.workGiversByPriority.Count; i++)
				{
					WorkGiverDef workGiver = item.workGiversByPriority[i];
					if ((drafted && !workGiver.canBeDoneWhileDrafted) || !(workGiver.Worker is WorkGiver_Scanner workGiver_Scanner) || !workGiver_Scanner.def.directOrderable)
					{
						continue;
					}
					JobFailReason.Clear();
					if (ScannerShouldSkip(pawn, workGiver_Scanner, thing))
					{
						continue;
					}
					string text = null;
					Action action = null;
					PawnCapacityDef pawnCapacityDef = workGiver_Scanner.MissingRequiredCapacity(pawn);
					if (pawnCapacityDef != null)
					{
						text = "CannotMissingHealthActivities".Translate(pawnCapacityDef.label);
					}
					else
					{
						Job job = (workGiver_Scanner.HasJobOnThing(pawn, thing, forced: true) ? workGiver_Scanner.JobOnThing(pawn, thing, forced: true) : null);
						if (JobFailReason.Silent)
						{
							continue;
						}
						if (job == null)
						{
							if (JobFailReason.HaveReason)
							{
								text = (JobFailReason.CustomJobString.NullOrEmpty() ? ((string)"CannotGenericWork".Translate(workGiver_Scanner.def.verb, thing.LabelShort, thing)) : ((string)"CannotGenericWorkCustom".Translate(JobFailReason.CustomJobString)));
								text = text + ": " + JobFailReason.Reason.CapitalizeFirst();
							}
							else
							{
								if (!thing.IsForbidden(pawn))
								{
									continue;
								}
								text = (thing.Position.InAllowedArea(pawn) ? ((string)"CannotPrioritizeForbidden".Translate(thing.Label, thing)) : ((string)("CannotPrioritizeForbiddenOutsideAllowedArea".Translate() + ": " + pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap.Label)));
							}
						}
						else
						{
							WorkTypeDef workType = workGiver_Scanner.def.workType;
							if (pawn.WorkTagIsDisabled(workGiver_Scanner.def.workTags))
							{
								text = "CannotPrioritizeWorkGiverDisabled".Translate(workGiver_Scanner.def.label);
							}
							else if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(pawn, job))
							{
								text = "CannotGenericAlreadyAm".Translate(workGiver_Scanner.PostProcessedGerund(job), thing.LabelShort, thing);
							}
							else if (pawn.workSettings.GetPriority(workType) == 0)
							{
								text = (pawn.WorkTypeIsDisabled(workType) ? ((string)"CannotPrioritizeWorkTypeDisabled".Translate(workType.gerundLabel)) : ((!"CannotPrioritizeNotAssignedToWorkType".CanTranslate()) ? ((string)"CannotPrioritizeWorkTypeDisabled".Translate(workType.pawnLabel)) : ((string)"CannotPrioritizeNotAssignedToWorkType".Translate(workType.gerundLabel))));
							}
							else if (job.def == JobDefOf.Research && thing is Building_ResearchBench)
							{
								text = "CannotPrioritizeResearch".Translate();
							}
							else if (thing.IsForbidden(pawn))
							{
								text = (thing.Position.InAllowedArea(pawn) ? ((string)"CannotPrioritizeForbidden".Translate(thing.Label, thing)) : ((string)("CannotPrioritizeForbiddenOutsideAllowedArea".Translate() + ": " + pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap.Label)));
							}
							else if (!pawn.CanReach(thing, workGiver_Scanner.PathEndMode, Danger.Deadly))
							{
								text = (thing.Label + ": " + "NoPath".Translate().CapitalizeFirst()).CapitalizeFirst();
							}
							else
							{
								text = "PrioritizeGeneric".Translate(workGiver_Scanner.PostProcessedGerund(job), thing.Label).CapitalizeFirst();
								string text2 = workGiver_Scanner.JobInfo(pawn, job);
								if (!string.IsNullOrEmpty(text2))
								{
									text = text + ": " + text2;
								}
								Job localJob = job;
								WorkGiver_Scanner localScanner = workGiver_Scanner;
								job.workGiverDef = workGiver_Scanner.def;
								action = delegate
								{
									if (pawn.jobs.TryTakeOrderedJobPrioritizedWork(localJob, localScanner, clickCell))
									{
										if (workGiver.forceMote != null)
										{
											MoteMaker.MakeStaticMote(clickCell, pawn.Map, workGiver.forceMote);
										}
										if (workGiver.forceFleck != null)
										{
											FleckMaker.Static(clickCell, pawn.Map, workGiver.forceFleck);
										}
									}
								};
							}
						}
					}
					if (DebugViewSettings.showFloatMenuWorkGivers)
					{
						text += $" (from {workGiver.defName})";
					}
					FloatMenuOption menuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action), pawn, thing, "ReservedBy", workGiver_Scanner.GetReservationLayer(pawn, thing));
					if (drafted && workGiver.autoTakeablePriorityDrafted != -1)
					{
						menuOption.autoTakeable = true;
						menuOption.autoTakeablePriority = workGiver.autoTakeablePriorityDrafted;
					}
					if (opts.Any((FloatMenuOption op) => op.Label == menuOption.Label))
					{
						continue;
					}
					if (workGiver.equivalenceGroup != null)
					{
						if (equivalenceGroupTempStorage[workGiver.equivalenceGroup.index] == null || (equivalenceGroupTempStorage[workGiver.equivalenceGroup.index].Disabled && !menuOption.Disabled))
						{
							equivalenceGroupTempStorage[workGiver.equivalenceGroup.index] = menuOption;
							flag = true;
						}
					}
					else
					{
						opts.Add(menuOption);
					}
				}
			}
			if (!flag)
			{
				continue;
			}
			for (int j = 0; j < equivalenceGroupTempStorage.Length; j++)
			{
				if (equivalenceGroupTempStorage[j] != null)
				{
					opts.Add(equivalenceGroupTempStorage[j]);
					equivalenceGroupTempStorage[j] = null;
				}
			}
		}
		foreach (WorkTypeDef item2 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
		{
			for (int k = 0; k < item2.workGiversByPriority.Count; k++)
			{
				WorkGiverDef workGiver2 = item2.workGiversByPriority[k];
				if ((drafted && !workGiver2.canBeDoneWhileDrafted) || !(workGiver2.Worker is WorkGiver_Scanner workGiver_Scanner2) || !workGiver_Scanner2.def.directOrderable)
				{
					continue;
				}
				JobFailReason.Clear();
				if (!workGiver_Scanner2.PotentialWorkCellsGlobal(pawn).Contains(clickCell) || workGiver_Scanner2.ShouldSkip(pawn, forced: true))
				{
					continue;
				}
				Action action2 = null;
				string label = null;
				PawnCapacityDef pawnCapacityDef2 = workGiver_Scanner2.MissingRequiredCapacity(pawn);
				if (pawnCapacityDef2 != null)
				{
					label = "CannotMissingHealthActivities".Translate(pawnCapacityDef2.label);
				}
				else
				{
					Job job2 = (workGiver_Scanner2.HasJobOnCell(pawn, clickCell, forced: true) ? workGiver_Scanner2.JobOnCell(pawn, clickCell, forced: true) : null);
					if (job2 == null)
					{
						if (JobFailReason.HaveReason)
						{
							if (!JobFailReason.CustomJobString.NullOrEmpty())
							{
								label = "CannotGenericWorkCustom".Translate(JobFailReason.CustomJobString);
							}
							else
							{
								label = "CannotGenericWork".Translate(workGiver_Scanner2.def.verb, "AreaLower".Translate());
							}
							label = label + ": " + JobFailReason.Reason.CapitalizeFirst();
						}
						else
						{
							if (!clickCell.IsForbidden(pawn))
							{
								continue;
							}
							if (!clickCell.InAllowedArea(pawn))
							{
								label = "CannotPrioritizeForbiddenOutsideAllowedArea".Translate() + ": " + pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap.Label;
							}
							else
							{
								label = "CannotPrioritizeCellForbidden".Translate();
							}
						}
					}
					else
					{
						WorkTypeDef workType2 = workGiver_Scanner2.def.workType;
						if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(pawn, job2))
						{
							label = "CannotGenericAlreadyAmCustom".Translate(workGiver_Scanner2.PostProcessedGerund(job2));
						}
						else if (pawn.workSettings.GetPriority(workType2) == 0)
						{
							if (pawn.WorkTypeIsDisabled(workType2))
							{
								label = "CannotPrioritizeWorkTypeDisabled".Translate(workType2.gerundLabel);
							}
							else if ("CannotPrioritizeNotAssignedToWorkType".CanTranslate())
							{
								label = "CannotPrioritizeNotAssignedToWorkType".Translate(workType2.gerundLabel);
							}
							else
							{
								label = "CannotPrioritizeWorkTypeDisabled".Translate(workType2.pawnLabel);
							}
						}
						else if (clickCell.IsForbidden(pawn))
						{
							if (!clickCell.InAllowedArea(pawn))
							{
								label = "CannotPrioritizeForbiddenOutsideAllowedArea".Translate() + ": " + pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap.Label;
							}
							else
							{
								label = "CannotPrioritizeCellForbidden".Translate();
							}
						}
						else if (!pawn.CanReach(clickCell, PathEndMode.Touch, Danger.Deadly))
						{
							label = "AreaLower".Translate().CapitalizeFirst() + ": " + "NoPath".Translate().CapitalizeFirst();
						}
						else
						{
							label = "PrioritizeGeneric".Translate(workGiver_Scanner2.PostProcessedGerund(job2), "AreaLower".Translate()).CapitalizeFirst();
							Job localJob2 = job2;
							WorkGiver_Scanner localScanner2 = workGiver_Scanner2;
							job2.workGiverDef = workGiver_Scanner2.def;
							action2 = delegate
							{
								if (pawn.jobs.TryTakeOrderedJobPrioritizedWork(localJob2, localScanner2, clickCell))
								{
									if (workGiver2.forceMote != null)
									{
										MoteMaker.MakeStaticMote(clickCell, pawn.Map, workGiver2.forceMote);
									}
									if (workGiver2.forceFleck != null)
									{
										FleckMaker.Static(clickCell, pawn.Map, workGiver2.forceFleck);
									}
								}
							};
						}
					}
				}
				if (!opts.Any((FloatMenuOption op) => op.Label == label.TrimEnd()))
				{
					FloatMenuOption floatMenuOption = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action2), pawn, clickCell);
					if (drafted && workGiver2.autoTakeablePriorityDrafted != -1)
					{
						floatMenuOption.autoTakeable = true;
						floatMenuOption.autoTakeablePriority = workGiver2.autoTakeablePriorityDrafted;
					}
					opts.Add(floatMenuOption);
				}
			}
		}
	}

	private static bool ScannerShouldSkip(Pawn pawn, WorkGiver_Scanner scanner, Thing t)
	{
		if (scanner.PotentialWorkThingRequest.Accepts(t) || (scanner.PotentialWorkThingsGlobal(pawn) != null && scanner.PotentialWorkThingsGlobal(pawn).Contains(t)))
		{
			return scanner.ShouldSkip(pawn, forced: true);
		}
		return true;
	}

	private static FloatMenuOption GotoLocationOption(IntVec3 clickCell, Pawn pawn, bool suppressAutoTakeableGoto)
	{
		if (suppressAutoTakeableGoto)
		{
			return null;
		}
		IntVec3 curLoc = CellFinder.StandableCellNear(clickCell, pawn.Map, 2.9f);
		if (curLoc.IsValid && curLoc != pawn.Position)
		{
			if (ModsConfig.BiotechActive && pawn.IsColonyMech && !MechanitorUtility.InMechanitorCommandRange(pawn, curLoc))
			{
				return new FloatMenuOption("CannotGoOutOfRange".Translate() + ": " + "OutOfCommandRange".Translate(), null);
			}
			if (!pawn.CanReach(curLoc, PathEndMode.OnCell, Danger.Deadly))
			{
				return new FloatMenuOption("CannotGoNoPath".Translate(), null);
			}
			Action action = delegate
			{
				PawnGotoAction(clickCell, pawn, RCellFinder.BestOrderedGotoDestNear(curLoc, pawn));
			};
			return new FloatMenuOption("GoHere".Translate(), action, MenuOptionPriority.GoHere)
			{
				autoTakeable = true,
				autoTakeablePriority = 10f
			};
		}
		return null;
	}

	public static void PawnGotoAction(IntVec3 clickCell, Pawn pawn, IntVec3 gotoLoc)
	{
		bool flag;
		if (pawn.Position == gotoLoc || (pawn.CurJobDef == JobDefOf.Goto && pawn.CurJob.targetA.Cell == gotoLoc))
		{
			flag = true;
		}
		else
		{
			Job job = JobMaker.MakeJob(JobDefOf.Goto, gotoLoc);
			if (pawn.Map.exitMapGrid.IsExitCell(clickCell))
			{
				job.exitMapOnArrival = !pawn.IsColonyMech;
			}
			else if (!pawn.Map.IsPlayerHome && !pawn.Map.exitMapGrid.MapUsesExitGrid && CellRect.WholeMap(pawn.Map).IsOnEdge(clickCell, 3) && pawn.Map.Parent.GetComponent<FormCaravanComp>() != null && MessagesRepeatAvoider.MessageShowAllowed("MessagePlayerTriedToLeaveMapViaExitGrid-" + pawn.Map.uniqueID, 60f))
			{
				if (pawn.Map.Parent.GetComponent<FormCaravanComp>().CanFormOrReformCaravanNow)
				{
					Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CanReform".Translate(), pawn.Map.Parent, MessageTypeDefOf.RejectInput, historical: false);
				}
				else
				{
					Messages.Message("MessagePlayerTriedToLeaveMapViaExitGrid_CantReform".Translate(), pawn.Map.Parent, MessageTypeDefOf.RejectInput, historical: false);
				}
			}
			flag = pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}
		if (flag)
		{
			FleckMaker.Static(gotoLoc, pawn.Map, FleckDefOf.FeedbackGoto);
		}
	}
}
