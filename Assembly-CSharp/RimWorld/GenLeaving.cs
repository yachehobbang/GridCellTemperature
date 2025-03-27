using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class GenLeaving
{
	private const float LeaveFraction_Kill = 0.25f;

	private const float LeaveFraction_Cancel = 1f;

	public const float LeaveFraction_DeconstructDefault = 0.5f;

	private const float LeaveFraction_FailConstruction = 0.5f;

	private static List<Thing> tmpKilledLeavings = new List<Thing>();

	private static List<IntVec3> tmpCellsCandidates = new List<IntVec3>();

	public static void DoLeavingsFor(Thing diedThing, Map map, DestroyMode mode, List<Thing> listOfLeavingsOut = null)
	{
		DoLeavingsFor(diedThing, map, mode, diedThing.OccupiedRect().ExpandedBy(diedThing.def.killedLeavingsExpandRect), null, listOfLeavingsOut);
	}

	public static void DoLeavingsFor(Thing diedThing, Map map, DestroyMode mode, CellRect leavingsRect, Predicate<IntVec3> nearPlaceValidator = null, List<Thing> listOfLeavingsOut = null)
	{
		if (Current.ProgramState != ProgramState.Playing && mode != DestroyMode.Refund)
		{
			return;
		}
		int num;
		switch (mode)
		{
		case DestroyMode.Vanish:
		case DestroyMode.QuestLogic:
			return;
		default:
			num = ((mode == DestroyMode.KillFinalizeLeavingsOnly) ? 1 : 0);
			break;
		case DestroyMode.KillFinalize:
			num = 1;
			break;
		}
		bool flag = (byte)num != 0;
		if (flag && diedThing.def.filthLeaving != null)
		{
			for (int i = leavingsRect.minZ; i <= leavingsRect.maxZ; i++)
			{
				for (int j = leavingsRect.minX; j <= leavingsRect.maxX; j++)
				{
					FilthMaker.TryMakeFilth(new IntVec3(j, 0, i), map, diedThing.def.filthLeaving, Rand.RangeInclusive(1, 3));
				}
			}
		}
		if (flag && diedThing.def.race != null && !diedThing.def.race.detritusLeavings.NullOrEmpty())
		{
			DetritusLeavingType detritusLeavingType = diedThing.def.race.detritusLeavings.RandomElement();
			if (Rand.Chance(detritusLeavingType.spawnChance))
			{
				CellFinder.TryFindRandomCellNear(diedThing.Position, map, 1, (IntVec3 c) => c.Standable(map), out var result);
				GenSpawn.Spawn(ThingMaker.MakeThing(detritusLeavingType.def), result, map).overrideGraphicIndex = detritusLeavingType.texOverrideIndex;
			}
		}
		ThingOwner<Thing> thingOwner = new ThingOwner<Thing>();
		if (flag)
		{
			List<ThingDefCountClass> list = new List<ThingDefCountClass>();
			Rand.PushState(diedThing.thingIDNumber);
			if (!(diedThing is Pawn { IsShambler: not false }) && Rand.Chance(diedThing.def.killedLeavingsChance))
			{
				if (diedThing.def.killedLeavings != null)
				{
					list.AddRange(diedThing.def.killedLeavings);
				}
				if (diedThing.HostileTo(Faction.OfPlayer) && !diedThing.def.killedLeavingsPlayerHostile.NullOrEmpty())
				{
					list.AddRange(diedThing.def.killedLeavingsPlayerHostile);
				}
				if (diedThing.def.killedLeavingsRanges != null)
				{
					foreach (ThingDefCountRangeClass killedLeavingsRange in diedThing.def.killedLeavingsRanges)
					{
						int num2 = Mathf.RoundToInt(killedLeavingsRange.countRange.RandomInRange);
						if (num2 > 0)
						{
							list.Add(new ThingDefCountClass(killedLeavingsRange.thingDef, num2));
						}
					}
				}
			}
			if (diedThing is ThingWithComps thingWithComps)
			{
				list.AddRange(thingWithComps.GetAdditionalLeavings(mode));
			}
			if (ModsConfig.AnomalyActive && diedThing is Pawn { IsMutant: not false } pawn2 && !pawn2.mutant.Def.killedLeavings.NullOrEmpty())
			{
				list.AddRange(pawn2.mutant.Def.killedLeavings);
			}
			for (int k = 0; k < list.Count; k++)
			{
				ThingDefCountClass thingDefCountClass = list[k];
				if (!thingDefCountClass.IsChanceBased || Rand.Chance(thingDefCountClass.DropChance))
				{
					Thing thing = ThingMaker.MakeThing(list[k].thingDef);
					thing.stackCount = list[k].count;
					thingOwner.TryAdd(thing);
				}
			}
			Rand.PopState();
		}
		if (CanBuildingLeaveResources(diedThing, mode) && mode != DestroyMode.KillFinalizeLeavingsOnly)
		{
			if (diedThing is Frame frame)
			{
				for (int num3 = frame.resourceContainer.Count - 1; num3 >= 0; num3--)
				{
					int num4 = GetBuildingResourcesLeaveCalculator(diedThing, mode)(frame.resourceContainer[num3].stackCount);
					if (num4 > 0)
					{
						frame.resourceContainer.TryTransferToContainer(frame.resourceContainer[num3], thingOwner, num4);
					}
				}
				frame.resourceContainer.ClearAndDestroyContents();
			}
			else
			{
				List<ThingDefCountClass> list2 = diedThing.CostListAdjusted();
				for (int l = 0; l < list2.Count; l++)
				{
					ThingDefCountClass thingDefCountClass2 = list2[l];
					if (thingDefCountClass2.thingDef == ThingDefOf.ReinforcedBarrel && !Find.Storyteller.difficulty.classicMortars)
					{
						CompRefuelable compRefuelable = diedThing.TryGetComp<CompRefuelable>();
						if (compRefuelable != null && compRefuelable.Props.fuelIsMortarBarrel && compRefuelable.FuelPercentOfMax < 0.5f)
						{
							continue;
						}
					}
					if (diedThing.def.building?.leavingsBlacklist != null && diedThing.def.building.leavingsBlacklist.Contains(thingDefCountClass2.thingDef))
					{
						continue;
					}
					int num5 = GetBuildingResourcesLeaveCalculator(diedThing, mode)(thingDefCountClass2.count);
					if (num5 > 0 && mode == DestroyMode.KillFinalize && thingDefCountClass2.thingDef.slagDef != null)
					{
						int count = thingDefCountClass2.thingDef.slagDef.smeltProducts.First((ThingDefCountClass pro) => pro.thingDef == ThingDefOf.Steel).count;
						int a = num5 / count;
						a = Mathf.Min(a, diedThing.def.Size.Area / 2);
						for (int m = 0; m < a; m++)
						{
							thingOwner.TryAdd(ThingMaker.MakeThing(thingDefCountClass2.thingDef.slagDef));
						}
						num5 -= a * count;
					}
					if (num5 > 0)
					{
						Thing thing2 = ThingMaker.MakeThing(thingDefCountClass2.thingDef);
						thing2.stackCount = num5;
						thingOwner.TryAdd(thing2);
					}
				}
			}
		}
		tmpKilledLeavings.Clear();
		List<IntVec3> list3 = leavingsRect.Cells.InRandomOrder().ToList();
		int num6 = 0;
		while (thingOwner.Count > 0)
		{
			if (mode == DestroyMode.KillFinalize && !map.areaManager.Home[list3[num6]] && !diedThing.def.forceLeavingsAllowed)
			{
				thingOwner[0].SetForbidden(value: true, warnOnFail: false);
			}
			if (!thingOwner.TryDrop(thingOwner[0], list3[num6], map, ThingPlaceMode.Near, out var lastResultingThing, null, nearPlaceValidator))
			{
				Log.Warning(string.Concat("Failed to place all leavings for destroyed thing ", diedThing, " at ", leavingsRect.CenterCell));
				break;
			}
			tmpKilledLeavings.Add(lastResultingThing);
			num6++;
			if (num6 >= list3.Count)
			{
				num6 = 0;
			}
		}
		listOfLeavingsOut?.AddRange(tmpKilledLeavings);
		if (mode == DestroyMode.KillFinalize && tmpKilledLeavings.Count > 0)
		{
			QuestUtility.SendQuestTargetSignals(diedThing.questTags, "KilledLeavingsLeft", diedThing.Named("DROPPER"), tmpKilledLeavings.Named("SUBJECT"));
		}
		tmpKilledLeavings.Clear();
	}

	public static void DoLeavingsFor(TerrainDef terrain, IntVec3 cell, Map map)
	{
		if (Current.ProgramState != ProgramState.Playing)
		{
			return;
		}
		ThingOwner<Thing> thingOwner = new ThingOwner<Thing>();
		List<ThingDefCountClass> list = terrain.CostListAdjusted(null);
		for (int i = 0; i < list.Count; i++)
		{
			ThingDefCountClass thingDefCountClass = list[i];
			int num = GenMath.RoundRandom((float)thingDefCountClass.count * terrain.resourcesFractionWhenDeconstructed);
			if (num > 0)
			{
				Thing thing = ThingMaker.MakeThing(thingDefCountClass.thingDef);
				thing.stackCount = num;
				thingOwner.TryAdd(thing);
			}
		}
		while (thingOwner.Count > 0)
		{
			if (!thingOwner.TryDrop(thingOwner[0], cell, map, ThingPlaceMode.Near, out var _))
			{
				Log.Warning(string.Concat("Failed to place all leavings for removed terrain ", terrain, " at ", cell));
				break;
			}
		}
	}

	public static bool CanBuildingLeaveResources(Thing destroyedThing, DestroyMode mode)
	{
		if (!(destroyedThing is Building))
		{
			return false;
		}
		if (mode == DestroyMode.Deconstruct && typeof(Frame).IsAssignableFrom(destroyedThing.GetType()))
		{
			mode = DestroyMode.Cancel;
		}
		return mode switch
		{
			DestroyMode.Vanish => false, 
			DestroyMode.WillReplace => false, 
			DestroyMode.KillFinalize => destroyedThing.def.leaveResourcesWhenKilled, 
			DestroyMode.Deconstruct => destroyedThing.def.resourcesFractionWhenDeconstructed != 0f, 
			DestroyMode.Cancel => true, 
			DestroyMode.FailConstruction => true, 
			DestroyMode.Refund => true, 
			DestroyMode.QuestLogic => false, 
			DestroyMode.KillFinalizeLeavingsOnly => false, 
			_ => throw new ArgumentException("Unknown destroy mode " + mode), 
		};
	}

	private static Func<int, int> GetBuildingResourcesLeaveCalculator(Thing destroyedThing, DestroyMode mode)
	{
		if (!CanBuildingLeaveResources(destroyedThing, mode))
		{
			return (int count) => 0;
		}
		if (mode == DestroyMode.Deconstruct && typeof(Frame).IsAssignableFrom(destroyedThing.GetType()))
		{
			mode = DestroyMode.Cancel;
		}
		return mode switch
		{
			DestroyMode.Vanish => (int count) => 0, 
			DestroyMode.WillReplace => (int count) => 0, 
			DestroyMode.KillFinalize => (int count) => GenMath.RoundRandom((float)count * 0.25f), 
			DestroyMode.Deconstruct => (int count) => Mathf.Min(GenMath.RoundRandom((float)count * destroyedThing.def.resourcesFractionWhenDeconstructed), count), 
			DestroyMode.Cancel => (int count) => GenMath.RoundRandom((float)count * 1f), 
			DestroyMode.FailConstruction => (int count) => GenMath.RoundRandom((float)count * 0.5f), 
			DestroyMode.Refund => (int count) => count, 
			DestroyMode.QuestLogic => (int count) => 0, 
			DestroyMode.KillFinalizeLeavingsOnly => (int count) => 0, 
			_ => throw new ArgumentException("Unknown destroy mode " + mode), 
		};
	}

	public static void DropFilthDueToDamage(Thing t, float damageDealt)
	{
		if (!t.def.useHitPoints || !t.Spawned || t.def.filthLeaving == null)
		{
			return;
		}
		CellRect cellRect = t.OccupiedRect().ExpandedBy(1);
		tmpCellsCandidates.Clear();
		foreach (IntVec3 item in cellRect)
		{
			if (item.InBounds(t.Map) && item.Walkable(t.Map))
			{
				tmpCellsCandidates.Add(item);
			}
		}
		if (tmpCellsCandidates.Any())
		{
			int num = GenMath.RoundRandom(damageDealt * Mathf.Min(1f / 60f, 1f / ((float)t.MaxHitPoints / 10f)));
			for (int i = 0; i < num; i++)
			{
				FilthMaker.TryMakeFilth(tmpCellsCandidates.RandomElement(), t.Map, t.def.filthLeaving);
			}
			tmpCellsCandidates.Clear();
		}
	}
}
