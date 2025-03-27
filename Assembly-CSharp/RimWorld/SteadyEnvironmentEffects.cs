using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace RimWorld;

public class SteadyEnvironmentEffects
{
	private Map map;

	private ModuleBase snowNoise;

	private int cycleIndex;

	private float outdoorMeltAmount;

	private float snowRate;

	private float rainRate;

	private const float MapFractionCheckPerTick = 0.0006f;

	private const float RainFireCheckInterval = 97f;

	private const float RainFireChanceOverall = 0.02f;

	private const float RainFireChancePerBuilding = 0.2f;

	private const float SnowFallRateFactor = 0.046f;

	private const float SnowMeltRateFactor = 0.0058f;

	private static readonly FloatRange AutoIgnitionTemperatureRange = new FloatRange(240f, 1000f);

	private const float AutoIgnitionChanceFactor = 0.7f;

	private const float FireGlowRate = 0.33f;

	public SteadyEnvironmentEffects(Map map)
	{
		this.map = map;
	}

	public void SteadyEnvironmentEffectsTick()
	{
		if ((float)Find.TickManager.TicksGame % 97f == 0f && Rand.Chance(0.02f))
		{
			RollForRainFire();
		}
		outdoorMeltAmount = MeltAmountAt(map.mapTemperature.OutdoorTemp);
		snowRate = map.weatherManager.SnowRate;
		rainRate = map.weatherManager.RainRate;
		int num = Mathf.CeilToInt((float)map.Area * 0.0006f);
		int area = map.Area;
		for (int i = 0; i < num; i++)
		{
			if (cycleIndex >= area)
			{
				cycleIndex = 0;
			}
			IntVec3 c = map.cellsInRandomOrder.Get(cycleIndex);
			DoCellSteadyEffects(c);
			cycleIndex++;
		}
	}

	private void DoCellSteadyEffects(IntVec3 c)
	{
		Room room = c.GetRoom(map);
		bool flag = map.roofGrid.Roofed(c);
		bool flag2 = room?.UsesOutdoorTemperature ?? false;
		if (room == null || flag2)
		{
			if (outdoorMeltAmount > 0f)
			{
				map.snowGrid.AddDepth(c, 0f - outdoorMeltAmount);
			}
			if (!flag && snowRate > 0.001f)
			{
				AddFallenSnowAt(c, 0.046f * map.weatherManager.SnowRate);
			}
		}
		if (room != null)
		{
			TerrainDef terrain = c.GetTerrain(map);
			List<Thing> thingList = c.GetThingList(map);
			for (int num = thingList.Count - 1; num >= 0; num--)
			{
				Thing thing = thingList[num];
				if (thing is Filth filth)
				{
					if (!flag && thing.def.filth.rainWashes && Rand.Chance(rainRate))
					{
						filth.ThinFilth();
					}
					if (filth.DisappearAfterTicks != 0 && filth.TicksSinceThickened > filth.DisappearAfterTicks && !filth.Destroyed)
					{
						filth.Destroy();
					}
				}
				else
				{
					TryDoDeteriorate(thing, flag, flag2, terrain);
				}
			}
			if (!flag2)
			{
				float temperature = room.Temperature;
				if (temperature > 0f)
				{
					float num2 = MeltAmountAt(temperature);
					if (num2 > 0f)
					{
						map.snowGrid.AddDepth(c, 0f - num2);
					}
					if (temperature > AutoIgnitionTemperatureRange.min)
					{
						float value = Rand.Value;
						if (value < AutoIgnitionTemperatureRange.InverseLerpThroughRange(temperature) * 0.7f && Rand.Chance(FireUtility.ChanceToStartFireIn(c, map)))
						{
							FireUtility.TryStartFireIn(c, map, 0.1f, null);
						}
						if (value < 0.33f)
						{
							FleckMaker.ThrowHeatGlow(c, map, 2.3f);
						}
					}
				}
			}
		}
		map.gameConditionManager.DoSteadyEffects(c, map);
		GasUtility.DoSteadyEffects(c, map);
	}

	public static bool ProtectedByEdifice(IntVec3 c, Map map)
	{
		Building edifice = c.GetEdifice(map);
		if (edifice != null && edifice.def.building != null && edifice.def.building.preventDeteriorationOnTop)
		{
			return true;
		}
		return false;
	}

	private float MeltAmountAt(float temperature)
	{
		if (temperature < 0f)
		{
			return 0f;
		}
		if (temperature < 10f)
		{
			return temperature * temperature * 0.0058f * 0.1f;
		}
		return temperature * 0.0058f;
	}

	public void AddFallenSnowAt(IntVec3 c, float baseAmount)
	{
		if (snowNoise == null)
		{
			snowNoise = new Perlin(0.03999999910593033, 2.0, 0.5, 5, Rand.Range(0, 651431), QualityMode.Medium);
		}
		float value = snowNoise.GetValue(c);
		value += 1f;
		value *= 0.5f;
		if (value < 0.5f)
		{
			value = 0.5f;
		}
		float depthToAdd = baseAmount * value;
		map.snowGrid.AddDepth(c, depthToAdd);
	}

	public static float FinalDeteriorationRate(Thing t, List<string> reasons = null)
	{
		if (t.Spawned)
		{
			Room room = t.GetRoom();
			return FinalDeteriorationRate(t, t.Position.Roofed(t.Map), room?.UsesOutdoorTemperature ?? false, t.Position.GetTerrain(t.Map), reasons);
		}
		if (t.SpawnedOrAnyParentSpawned && !t.def.canDeteriorateUnspawned)
		{
			return 0f;
		}
		return FinalDeteriorationRate(t, roofed: false, roomUsesOutdoorTemperature: false, null, reasons);
	}

	public static float FinalDeteriorationRate(Thing t, bool roofed, bool roomUsesOutdoorTemperature, TerrainDef terrain, List<string> reasons = null)
	{
		if (!t.def.CanEverDeteriorate)
		{
			return 0f;
		}
		Map mapHeld = t.MapHeld;
		float num = t.GetStatValue(StatDefOf.DeteriorationRate);
		Genepack genepack = t as Genepack;
		if (ModsConfig.BiotechActive && genepack != null && !genepack.Deteriorating)
		{
			num = 0f;
		}
		if (num <= 0f)
		{
			return 0f;
		}
		if (reasons != null)
		{
			if (t.def.deteriorateFromEnvironmentalEffects)
			{
				if (!roofed)
				{
					reasons.Add("DeterioratingUnroofed".Translate());
					if (mapHeld.weatherManager.RainRate > 0f)
					{
						reasons.Add("DeterioratingRaining".Translate());
					}
				}
				if (roomUsesOutdoorTemperature)
				{
					reasons.Add("DeterioratingOutdoors".Translate());
				}
				if (terrain != null && terrain.extraDeteriorationFactor != 0f)
				{
					reasons.Add(terrain.label);
				}
			}
			if (ModsConfig.BiotechActive && genepack != null && genepack.Deteriorating)
			{
				reasons.Add("NotInGeneBank".Translate());
			}
		}
		return num;
	}

	private void TryDoDeteriorate(Thing t, bool roofed, bool roomUsesOutdoorTemperature, TerrainDef terrain)
	{
		if (t is Corpse corpse && corpse.InnerPawn.apparel != null)
		{
			List<Apparel> wornApparel = corpse.InnerPawn.apparel.WornApparel;
			for (int i = 0; i < wornApparel.Count; i++)
			{
				TryDoDeteriorate(wornApparel[i], roofed, roomUsesOutdoorTemperature, terrain);
			}
		}
		float num = FinalDeteriorationRate(t, roofed, roomUsesOutdoorTemperature, terrain);
		if (!(num < 0.001f) && Rand.Chance(num / 36f))
		{
			IntVec3 position = t.Position;
			Map map = t.Map;
			bool sendMessage = t.IsInAnyStorage();
			DoDeteriorationDamage(t, position, map, sendMessage);
		}
	}

	public static void DoDeteriorationDamage(Thing t, IntVec3 pos, Map map, bool sendMessage)
	{
		t.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1f));
		if (sendMessage && t.Destroyed && t.def.messageOnDeteriorateInStorage)
		{
			Messages.Message("MessageDeterioratedAway".Translate(t.Label), new TargetInfo(pos, map), MessageTypeDefOf.NegativeEvent);
		}
	}

	private void RollForRainFire()
	{
		if (Rand.Chance(0.2f * (float)map.listerBuildings.allBuildingsColonistElecFire.Count * map.weatherManager.RainRate))
		{
			Building building = map.listerBuildings.allBuildingsColonistElecFire.RandomElement();
			if (!map.roofGrid.Roofed(building.Position))
			{
				ShortCircuitUtility.TryShortCircuitInRain(building);
			}
		}
	}
}
