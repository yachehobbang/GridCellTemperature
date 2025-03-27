using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld.Planet;

public static class DaysWorthOfFoodCalculator
{
	private static List<Pawn> tmpPawns = new List<Pawn>();

	private static List<ThingDefCount> tmpThingDefCounts = new List<ThingDefCount>();

	private static List<ThingCount> tmpThingCounts = new List<ThingCount>();

	public const float InfiniteDaysWorthOfFood = 600f;

	private static List<float> tmpDaysWorthOfFoodForPawn = new List<float>();

	private static List<ThingDefCount> tmpFood = new List<ThingDefCount>();

	private static List<ThingDefCount> tmpFood2 = new List<ThingDefCount>();

	private static List<Pair<int, int>> tmpTicksToArrive = new List<Pair<int, int>>();

	private static List<float> cachedNutritionBetweenHungryAndFed = new List<float>();

	private static List<int> cachedTicksUntilHungryWhenFed = new List<int>();

	private static List<float> cachedMaxFoodLevel = new List<float>();

	private static HashSet<Pawn> babiesWithFeeders = new HashSet<Pawn>();

	private static List<Pawn> tmpLactatingPawns = new List<Pawn>(16);

	private static float ApproxDaysWorthOfFood(List<Pawn> pawns, List<ThingDefCount> extraFood, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction, WorldPath path = null, float nextTileCostLeft = 0f, int caravanTicksPerMove = 3300, bool assumeCaravanMoving = true)
	{
		if (!AnyFoodEatingPawn(pawns))
		{
			return 600f;
		}
		if (!assumeCaravanMoving)
		{
			path = null;
		}
		tmpFood.Clear();
		if (extraFood != null)
		{
			int i = 0;
			for (int count = extraFood.Count; i < count; i++)
			{
				ThingDefCount item = extraFood[i];
				if (item.ThingDef.IsNutritionGivingIngestible && item.Count > 0)
				{
					tmpFood.Add(item);
				}
			}
		}
		int j = 0;
		for (int count2 = pawns.Count; j < count2; j++)
		{
			Pawn pawn2 = pawns[j];
			if (InventoryCalculatorsUtility.ShouldIgnoreInventoryOf(pawn2, ignoreInventory))
			{
				continue;
			}
			ThingOwner<Thing> innerContainer = pawn2.inventory.innerContainer;
			int k = 0;
			for (int count3 = innerContainer.Count; k < count3; k++)
			{
				Thing thing = innerContainer[k];
				if (thing.def.IsNutritionGivingIngestible)
				{
					tmpFood.Add(new ThingDefCount(thing.def, thing.stackCount));
				}
			}
		}
		tmpFood2.Clear();
		tmpFood2.AddRange(tmpFood);
		tmpFood.Clear();
		int l = 0;
		for (int count4 = tmpFood2.Count; l < count4; l++)
		{
			ThingDefCount item2 = tmpFood2[l];
			bool flag = false;
			int m = 0;
			for (int count5 = tmpFood.Count; m < count5; m++)
			{
				ThingDefCount thingDefCount = tmpFood[m];
				if (thingDefCount.ThingDef == item2.ThingDef)
				{
					tmpFood[m] = thingDefCount.WithCount(thingDefCount.Count + item2.Count);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				tmpFood.Add(item2);
			}
		}
		tmpDaysWorthOfFoodForPawn.Clear();
		int n = 0;
		for (int count6 = pawns.Count; n < count6; n++)
		{
			tmpDaysWorthOfFoodForPawn.Add(0f);
		}
		int ticksAbs = Find.TickManager.TicksAbs;
		tmpTicksToArrive.Clear();
		if (path != null && path.Found)
		{
			CaravanArrivalTimeEstimator.EstimatedTicksToArriveToEvery(tile, path.LastNode, path, nextTileCostLeft, caravanTicksPerMove, ticksAbs, tmpTicksToArrive);
		}
		cachedNutritionBetweenHungryAndFed.Clear();
		cachedTicksUntilHungryWhenFed.Clear();
		cachedMaxFoodLevel.Clear();
		int num = 0;
		for (int count7 = pawns.Count; num < count7; num++)
		{
			Pawn pawn3 = pawns[num];
			if (pawn3.RaceProps.EatsFood && pawn3.needs.food != null)
			{
				Need_Food food = pawn3.needs.food;
				cachedNutritionBetweenHungryAndFed.Add(food.NutritionBetweenHungryAndFed);
				cachedTicksUntilHungryWhenFed.Add(food.TicksUntilHungryWhenFedIgnoringMalnutrition);
				cachedMaxFoodLevel.Add(food.MaxLevel);
			}
			else
			{
				cachedNutritionBetweenHungryAndFed.Add(0f);
				cachedTicksUntilHungryWhenFed.Add(0);
				cachedMaxFoodLevel.Add(0f);
			}
		}
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		bool flag2 = false;
		WorldGrid worldGrid = Find.WorldGrid;
		babiesWithFeeders.Clear();
		tmpLactatingPawns.Clear();
		tmpLactatingPawns.AddRange(pawns);
		tmpLactatingPawns.RemoveAll((Pawn pawn) => !ChildcareUtility.CanBreastfeed(pawn, out var _));
		int count8 = tmpLactatingPawns.Count;
		for (int num5 = 0; num5 < count8; num5++)
		{
			for (int num6 = 0; num6 < 1; num6++)
			{
				tmpLactatingPawns.Add(tmpLactatingPawns[num5]);
			}
		}
		foreach (Pawn pawn4 in pawns)
		{
			if (ChildcareUtility.CanSuckle(pawn4, out var _))
			{
				ChildcareUtility.BreastfeedFailReason? reason3;
				int num7 = tmpLactatingPawns.FindIndex((Pawn feeder) => ChildcareUtility.CanMomBreastfeedBaby(feeder, pawn4, out reason3) && pawn4.mindState.AutofeedSetting(feeder) != AutofeedMode.Never);
				if (num7 >= 0)
				{
					tmpLactatingPawns[num7] = null;
					babiesWithFeeders.Add(pawn4);
				}
			}
		}
		bool flag3;
		do
		{
			flag3 = false;
			int num8 = ticksAbs + (int)(num3 * 60000f);
			int num9 = ((path != null) ? CaravanArrivalTimeEstimator.TileIllBeInAt(num8, tmpTicksToArrive, ticksAbs) : tile);
			bool flag4 = CaravanNightRestUtility.WouldBeRestingAt(num9, num8);
			float progressPerTick = ForagedFoodPerDayCalculator.GetProgressPerTick(assumeCaravanMoving && !flag4, flag4);
			float num10 = 1f / progressPerTick;
			bool flag5 = VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsAt(num9, num8);
			float num11 = num3 - num2;
			if (num11 > 0f)
			{
				num4 += num11 * 60000f;
				if (num4 >= num10)
				{
					BiomeDef biome = worldGrid[num9].biome;
					int num12 = Mathf.RoundToInt(ForagedFoodPerDayCalculator.GetForagedFoodCountPerInterval(pawns, biome, faction));
					ThingDef foragedFood = biome.foragedFood;
					while (num4 >= num10)
					{
						num4 -= num10;
						if (num12 <= 0)
						{
							continue;
						}
						bool flag6 = false;
						for (int num13 = tmpFood.Count - 1; num13 >= 0; num13--)
						{
							ThingDefCount thingDefCount2 = tmpFood[num13];
							if (thingDefCount2.ThingDef == foragedFood)
							{
								tmpFood[num13] = thingDefCount2.WithCount(thingDefCount2.Count + num12);
								flag6 = true;
								break;
							}
						}
						if (!flag6)
						{
							tmpFood.Add(new ThingDefCount(foragedFood, num12));
						}
					}
				}
			}
			num2 = num3;
			int num14 = 0;
			for (int count9 = pawns.Count; num14 < count9; num14++)
			{
				Pawn pawn5 = pawns[num14];
				if (!pawn5.RaceProps.EatsFood || pawn5.needs?.food == null || babiesWithFeeders.Contains(pawn5))
				{
					continue;
				}
				if (flag5 && VirtualPlantsUtility.CanEverEatVirtualPlants(pawn5))
				{
					if (tmpDaysWorthOfFoodForPawn[num14] < num3)
					{
						tmpDaysWorthOfFoodForPawn[num14] = num3;
					}
					else
					{
						tmpDaysWorthOfFoodForPawn[num14] += 0.45f;
					}
					flag3 = true;
				}
				else
				{
					float num15 = cachedNutritionBetweenHungryAndFed[num14];
					int num16 = cachedTicksUntilHungryWhenFed[num14];
					do
					{
						int num17 = BestEverEdibleFoodIndexFor(pawn5, tmpFood);
						if (num17 < 0)
						{
							if (tmpDaysWorthOfFoodForPawn[num14] < num3)
							{
								flag2 = true;
							}
							break;
						}
						ThingDefCount thingDefCount3 = tmpFood[num17];
						float num18 = Mathf.Min(thingDefCount3.ThingDef.ingestible.CachedNutrition, num15);
						float num19 = num18 / num15 * (float)num16 / 60000f;
						int num20 = Mathf.Min(Mathf.CeilToInt(Mathf.Min(0.2f, cachedMaxFoodLevel[num14]) / num18), thingDefCount3.Count);
						tmpDaysWorthOfFoodForPawn[num14] += num19 * (float)num20;
						tmpFood[num17] = thingDefCount3.WithCount(thingDefCount3.Count - num20);
						flag3 = true;
					}
					while (tmpDaysWorthOfFoodForPawn[num14] < num3);
				}
				if (flag2)
				{
					break;
				}
				num3 = Mathf.Max(num3, tmpDaysWorthOfFoodForPawn[num14]);
			}
		}
		while (!(!flag3 || flag2) && !(num3 > 601f));
		float num21 = 600f;
		int num22 = 0;
		for (int count10 = pawns.Count; num22 < count10; num22++)
		{
			if (pawns[num22].RaceProps.EatsFood && pawns[num22].needs?.food != null && !babiesWithFeeders.Contains(pawns[num22]))
			{
				num21 = Mathf.Min(num21, tmpDaysWorthOfFoodForPawn[num22]);
			}
		}
		return num21;
	}

	public static float ApproxDaysWorthOfFood(Caravan caravan)
	{
		return ApproxDaysWorthOfFood(caravan.PawnsListForReading, null, caravan.Tile, IgnorePawnsInventoryMode.DontIgnore, caravan.Faction, caravan.pather.curPath, caravan.pather.nextTileCostLeft, caravan.TicksPerMove, caravan.pather.Moving && !caravan.pather.Paused);
	}

	public static float ApproxDaysWorthOfFood(List<TransferableOneWay> transferables, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction, WorldPath path = null, float nextTileCostLeft = 0f, int caravanTicksPerMove = 3300)
	{
		tmpThingDefCounts.Clear();
		tmpPawns.Clear();
		for (int i = 0; i < transferables.Count; i++)
		{
			TransferableOneWay transferableOneWay = transferables[i];
			if (!transferableOneWay.HasAnyThing)
			{
				continue;
			}
			if (transferableOneWay.AnyThing is Pawn)
			{
				for (int j = 0; j < transferableOneWay.CountToTransfer; j++)
				{
					Pawn pawn = (Pawn)transferableOneWay.things[j];
					if (pawn.RaceProps.EatsFood && pawn.needs?.food != null)
					{
						tmpPawns.Add(pawn);
					}
				}
			}
			else if (!(transferableOneWay.AnyThing is Corpse t) || t.GetRotStage() == RotStage.Fresh)
			{
				tmpThingDefCounts.Add(new ThingDefCount(transferableOneWay.ThingDef, transferableOneWay.CountToTransfer));
			}
		}
		float result = ApproxDaysWorthOfFood(tmpPawns, tmpThingDefCounts, tile, ignoreInventory, faction, path, nextTileCostLeft, caravanTicksPerMove);
		tmpThingDefCounts.Clear();
		tmpPawns.Clear();
		return result;
	}

	public static float ApproxDaysWorthOfFoodLeftAfterTransfer(List<TransferableOneWay> transferables, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction, WorldPath path = null, float nextTileCostLeft = 0f, int caravanTicksPerMove = 3300)
	{
		tmpThingDefCounts.Clear();
		tmpPawns.Clear();
		for (int i = 0; i < transferables.Count; i++)
		{
			TransferableOneWay transferableOneWay = transferables[i];
			if (!transferableOneWay.HasAnyThing)
			{
				continue;
			}
			if (transferableOneWay.AnyThing is Pawn)
			{
				for (int num = transferableOneWay.things.Count - 1; num >= transferableOneWay.CountToTransfer; num--)
				{
					Pawn pawn = (Pawn)transferableOneWay.things[num];
					if (pawn.RaceProps.EatsFood && pawn.needs?.food != null)
					{
						tmpPawns.Add(pawn);
					}
				}
			}
			else
			{
				tmpThingDefCounts.Add(new ThingDefCount(transferableOneWay.ThingDef, transferableOneWay.MaxCount - transferableOneWay.CountToTransfer));
			}
		}
		float result = ApproxDaysWorthOfFood(tmpPawns, tmpThingDefCounts, tile, ignoreInventory, faction, path, nextTileCostLeft, caravanTicksPerMove);
		tmpThingDefCounts.Clear();
		tmpPawns.Clear();
		return result;
	}

	public static float ApproxDaysWorthOfFood(List<Pawn> pawns, List<Thing> potentiallyFood, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction)
	{
		tmpThingDefCounts.Clear();
		tmpPawns.Clear();
		for (int i = 0; i < pawns.Count; i++)
		{
			Pawn pawn = pawns[i];
			if (pawn.RaceProps.EatsFood && pawn.needs?.food != null)
			{
				tmpPawns.Add(pawn);
			}
		}
		for (int j = 0; j < potentiallyFood.Count; j++)
		{
			tmpThingDefCounts.Add(new ThingDefCount(potentiallyFood[j].def, potentiallyFood[j].stackCount));
		}
		float result = ApproxDaysWorthOfFood(tmpPawns, tmpThingDefCounts, tile, ignoreInventory, faction);
		tmpThingDefCounts.Clear();
		tmpPawns.Clear();
		return result;
	}

	public static float ApproxDaysWorthOfFood(List<Pawn> pawns, List<ThingCount> potentiallyFood, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction)
	{
		tmpThingDefCounts.Clear();
		for (int i = 0; i < potentiallyFood.Count; i++)
		{
			if (potentiallyFood[i].Count > 0)
			{
				tmpThingDefCounts.Add(new ThingDefCount(potentiallyFood[i].Thing.def, potentiallyFood[i].Count));
			}
		}
		float result = ApproxDaysWorthOfFood(pawns, tmpThingDefCounts, tile, ignoreInventory, faction);
		tmpThingDefCounts.Clear();
		return result;
	}

	public static float ApproxDaysWorthOfFoodLeftAfterTradeableTransfer(List<Thing> allCurrentThings, List<Tradeable> tradeables, int tile, IgnorePawnsInventoryMode ignoreInventory, Faction faction)
	{
		tmpThingCounts.Clear();
		TransferableUtility.SimulateTradeableTransfer(allCurrentThings, tradeables, tmpThingCounts);
		tmpPawns.Clear();
		tmpThingDefCounts.Clear();
		for (int num = tmpThingCounts.Count - 1; num >= 0; num--)
		{
			if (tmpThingCounts[num].Count > 0)
			{
				if (tmpThingCounts[num].Thing is Pawn pawn)
				{
					if (pawn.RaceProps.EatsFood && pawn.needs?.food != null)
					{
						tmpPawns.Add(pawn);
					}
				}
				else
				{
					tmpThingDefCounts.Add(new ThingDefCount(tmpThingCounts[num].Thing.def, tmpThingCounts[num].Count));
				}
			}
		}
		tmpThingCounts.Clear();
		float result = ApproxDaysWorthOfFood(tmpPawns, tmpThingDefCounts, tile, ignoreInventory, faction);
		tmpPawns.Clear();
		tmpThingDefCounts.Clear();
		return result;
	}

	public static bool AnyFoodEatingPawn(List<Pawn> pawns)
	{
		int i = 0;
		for (int count = pawns.Count; i < count; i++)
		{
			if (pawns[i].RaceProps.EatsFood && pawns[i].needs?.food != null)
			{
				return true;
			}
		}
		return false;
	}

	private static int BestEverEdibleFoodIndexFor(Pawn pawn, List<ThingDefCount> food)
	{
		int num = -1;
		float num2 = 0f;
		int i = 0;
		for (int count = food.Count; i < count; i++)
		{
			if (food[i].Count <= 0)
			{
				continue;
			}
			ThingDef thingDef = food[i].ThingDef;
			if (CaravanPawnsNeedsUtility.CanEatForNutritionEver(thingDef, pawn))
			{
				float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(thingDef, pawn, thingDef.ingestible.CachedNutrition);
				if (num < 0 || foodScore > num2)
				{
					num = i;
					num2 = foodScore;
				}
			}
		}
		return num;
	}
}
