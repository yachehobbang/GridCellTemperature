using GridCellTemperature.Core;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace GridCellTemperature.Access
{
	[HarmonyPatch(typeof(GenTemperature), "TryGetTemperatureForCell")]
	public static class GenTemperature_TryGetTemperatureForCell
	{
		public static bool Prefix(IntVec3 c, Map map, out float tempResult, ref bool __result)
		{
			tempResult = TemperatureGrid.GetTemperature(map, c);
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(Map), nameof(Map.ConstructComponents))]
	public static class Map_ConstructComponents
	{
		public static void Postfix(Map __instance)
		{
			new TemperatureGrid(__instance);
		}
	}

	[HarmonyPatch(typeof(Map), nameof(Map.ExposeData))]
	public static class Map_ExposeComponents
	{
		public static void Postfix(Map __instance)
		{
			var grid = TemperatureGrid.GetTemperatureGrid(__instance);
			if (grid == null)
			{
				return;
			}

			Scribe_Deep.Look(ref grid, "TemperatureGrid", __instance);
		}
	}

	[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
	public static class Map_FinalizeInit
	{
		public static void Postfix(Map __instance)
		{
			var grid = TemperatureGrid.GetTemperatureGrid(__instance);
			if (grid == null)
			{
				return;
			}

			grid.OnFinalizeInit();
		}
	}

	[HarmonyPatch(typeof(Map), nameof(Map.Dispose))]
	public static class Map_Dispose
	{
		public static void Postfix(Map __instance)
		{
			var grids = TemperatureGrid.TemperatureGrids;
			grids.Remove(__instance.uniqueID);
		}
	}

	[HarmonyPatch(typeof(World), "WorldTick")]
	public static class World_WorldTick
	{
		public static void Postfix(World __instance)
		{
			if ((Find.TickManager.TicksGame + 17) % 60 == 0)
			{
				TemperatureGrid.RunTemperatureSimulation();
			}
		}
	}

	[HarmonyPatch(typeof(GenTemperature), nameof(GenTemperature.PushHeat), new[] { typeof(IntVec3), typeof(Map), typeof(float) })]
	public static class GenTemperature_PushHeat
	{
		public static bool Prefix(IntVec3 c, Map map, float energy)
		{
			TemperatureGrid.PushHeat(map, c, energy);

			return false;
		}
	}

	[HarmonyPatch(typeof(GlobalControls), "TemperatureString")]
	public static class GlobalControls_TemperatureString
	{
		public static void Postfix(GlobalControls __instance, ref string __result)
		{
			var grid = TemperatureGrid.GetTemperatureGrid(Find.CurrentMap);
			if (grid == null)
			{
				return;
			}

			grid.GetTemperatureString(ref __result);
		}
	}

	[HarmonyPatch(typeof(MapTemperature), nameof(MapTemperature.GetCellBool))]
	public static class MapTemperature_GetCellBool
	{
		private static FieldInfo mapFieldInfo = AccessTools.Field(typeof(MapTemperature), "map");
		public static bool Prefix(MapTemperature __instance, int index, ref bool __result)
		{
			var map = (Map)mapFieldInfo.GetValue(__instance);
			__result = !map.fogGrid.IsFogged(index);
			return false;
		}
	}

	[HarmonyPatch(typeof(MapTemperature), nameof(MapTemperature.MapTemperatureTick))]
	public static class MapTemperature_MapTemperatureTick
	{
		public static bool Prefix(MapTemperature __instance)
		{
			// 타일 기반온도라 필요없는 함수
			// 쓸데없는 로직이 돌지 않게 하기 위해서 기존 로직 빼버린다
			return false;
		}
	}

	[HarmonyPatch(typeof(MapTemperature), nameof(MapTemperature.Notify_ThingSpawned))]
	public static class MapTemperature_Notify_ThingSpawned
	{
		public static bool Prefix(MapTemperature __instance, Thing thing)
		{
			// 타일 기반온도라 필요없는 함수
			// 쓸데없는 로직이 돌지 않게 하기 위해서 기존 로직 빼버린다
			return false;
		}
	}

	[HarmonyPatch(typeof(MapTemperature), nameof(MapTemperature.Drawer), MethodType.Getter)]
	public static class MapTemperature_Drawer_Getter
	{
		private static FieldInfo drawerIntField = AccessTools.Field(typeof(MapTemperature), "drawerInt");
		private static FieldInfo mapFieldInfo = AccessTools.Field(typeof(MapTemperature), "map");
		public static bool Prefix(MapTemperature __instance, ref CellBoolDrawer __result)
		{
			var drawerInt = (CellBoolDrawer)drawerIntField.GetValue(__instance);
			if (drawerInt == null)
			{
				var map = (Map)mapFieldInfo.GetValue(__instance);
				drawerInt = new TemperatureCellBoolDrawer(__instance, map.Size.x, map.Size.z, 3600);
				drawerIntField.SetValue(__instance, drawerInt);
			}
			__result = drawerInt;
			return false;
		}
	}

	[HarmonyPatch(typeof(SteadyEnvironmentEffects), "DoCellSteadyEffects")]
	public static class SteadyEnvironmentEffects_DoCellSteadyEffects
	{
		private static readonly FieldInfo mapFieldInfo = AccessTools.Field(typeof(SteadyEnvironmentEffects), "map");
		private static readonly FieldInfo snowRateFieldInfo = AccessTools.Field(typeof(SteadyEnvironmentEffects), "snowRate");
		private static readonly FieldInfo rainRateFieldInfo = AccessTools.Field(typeof(SteadyEnvironmentEffects), "rainRate");
		private static readonly FieldInfo AutoIgnitionTemperatureRangeFieldInfo = AccessTools.Field(typeof(SteadyEnvironmentEffects), "AutoIgnitionTemperatureRange");

		private static readonly MethodInfo tryDoDeteriorateMethodInfo = AccessTools.Method(typeof(SteadyEnvironmentEffects), "TryDoDeteriorate");
		private static readonly MethodInfo meltAmountAtMethodInfo = AccessTools.Method(typeof(SteadyEnvironmentEffects), "MeltAmountAt");

		private static readonly object[] cacheParams = new object[1];

		public static bool Prefix(SteadyEnvironmentEffects __instance, IntVec3 c)
		{
			var map = (Map)mapFieldInfo.GetValue(__instance);

			Room room = c.GetRoom(map);
			bool flag = map.roofGrid.Roofed(c);
			bool flag2 = room?.UsesOutdoorTemperature ?? false;

			var temperature = GenTemperature.GetTemperatureForCell(c, map);

			if (temperature > 0f)
			{
				cacheParams[0] = temperature;
				var meltAmount = (float)meltAmountAtMethodInfo.Invoke(__instance, cacheParams);
				map.snowGrid.AddDepth(c, 0f - meltAmount);
			}

			if (room == null || flag2)
			{
				var snowRate = (float)snowRateFieldInfo.GetValue(__instance);
				if (!flag && snowRate > 0.001f)
				{
					__instance.AddFallenSnowAt(c, 0.046f * map.weatherManager.SnowRate);
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
						var rainRate = (float)rainRateFieldInfo.GetValue(__instance);
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
						tryDoDeteriorateMethodInfo.Invoke(__instance, new object[] { thing, flag, flag2, terrain });
					}
				}
			}

			if (temperature > 0f)
			{
				var AutoIgnitionTemperatureRange = (FloatRange)AutoIgnitionTemperatureRangeFieldInfo.GetValue(__instance);
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
			map.gameConditionManager.DoSteadyEffects(c, map);
			GasUtility.DoSteadyEffects(c, map);

			return false;
		}
	}

	[HarmonyPatch(typeof(Room), nameof(Room.Temperature), MethodType.Getter)]
	public static class Room_Temperature_Getter
	{
		public static bool Prefix(Room __instance, ref float __result)
		{
			if (__instance.UsesOutdoorTemperature)
			{
				__result = __instance.Map.mapTemperature.OutdoorTemp;
				return false;
			}

			__result = TemperatureGrid.GetRoomAverageTemperature(__instance.Map, __instance.ID);
			return false;
		}
	}

	[HarmonyPatch(typeof(Room), nameof(Room.Temperature), MethodType.Setter)]
	public static class Room_Temperature_Setter
	{
		public static bool Prefix(Room __instance, float value)
		{
			var grid = TemperatureGrid.GetTemperatureGrid(__instance.Map);
			if (grid == null)
			{
				return false;
			}

			grid.SetRoomAverageTemperature(__instance.ID, value);
			return false;
		}
	}

	[HarmonyPatch(typeof(Building_Cooler), nameof(Building_Cooler.TickRare))]
	public static class Building_Cooler_TickRare
	{
		public static bool Prefix(Building_Cooler __instance)
		{
			var compPowerTrader = __instance.compPowerTrader;
			if (!compPowerTrader.PowerOn)
			{
				return false;
			}
			IntVec3 intVec = __instance.Position + IntVec3.South.RotatedBy(__instance.Rotation);
			IntVec3 intVec2 = __instance.Position + IntVec3.North.RotatedBy(__instance.Rotation);

			var map = __instance.Map;
			var flag = false;
			var compTempControl = __instance.compTempControl;
			if (!intVec2.Impassable(map) && !intVec.Impassable(map))
			{
				var room = intVec.GetRoom(map);
				var cellTemperature = (room == null || room.UsesOutdoorTemperature) ? TemperatureGrid.GetTemperature(map, intVec) : room.Temperature;

				flag = cellTemperature > compTempControl.TargetTemperature;
			}

			if (flag)
			{
				var energy = compTempControl.Props.energyPerSecond * 4.1666665f;
				var grid = TemperatureGrid.GetTemperatureGrid(map);
				grid.PushHeat(intVec, energy);
				grid.PushHeat(intVec2, -energy * 1.25f);

				compPowerTrader.PowerOutput = 0f - compPowerTrader.Props.PowerConsumption;
			}
			else
			{
				compPowerTrader.PowerOutput = (0f - compPowerTrader.Props.PowerConsumption) * compTempControl.Props.lowPowerConsumptionFactor;
			}

			compTempControl.operatingAtHighPower = flag;

			return false;
		}
	}

	[HarmonyPatch(typeof(Building_Heater), nameof(Building_Cooler.TickRare))]
	public static class Building_Heater_TickRare
	{
		public static bool Prefix(Building_Heater __instance)
		{
			var compPowerTrader = __instance.compPowerTrader;
			if (compPowerTrader.PowerOn)
			{
				var map = __instance.Map;
				var compTempControl = __instance.compTempControl;

				var room = __instance.Position.GetRoom(map);
				var cellTemperature = (room == null || room.UsesOutdoorTemperature) ? TemperatureGrid.GetTemperature(map, __instance.Position) : room.Temperature;
				bool flag = cellTemperature < compTempControl.TargetTemperature;

				float energy = compTempControl.Props.energyPerSecond * 4.1666665f;
				CompProperties_Power props = compPowerTrader.Props;
				if (flag)
				{
					TemperatureGrid.PushHeat(map, __instance.Position, energy);
					compPowerTrader.PowerOutput = 0f - props.PowerConsumption;
				}
				else
				{
					compPowerTrader.PowerOutput = (0f - props.PowerConsumption) * compTempControl.Props.lowPowerConsumptionFactor;
				}
				compTempControl.operatingAtHighPower = flag;
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(TemperatureCache), nameof(TemperatureCache.TryGetAverageCachedRoomTemp))]
	public static class TemperatureCache_TryGetAverageCachedRoomTemp
	{
		public static bool Prefix(TemperatureCache __instance, Room r, out float result, ref bool __result)
		{
			// 타일 기반온도라 필요없는 함수
			// 쓸데없는 로직이 돌지 않게 하기 위해서 기존 로직 빼버린다
			result = 0f;
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(RoomTempTracker), nameof(RoomTempTracker.EqualizeTemperature))]
	public static class RoomTempTracker_EqualizeTemperature
	{
		public static bool Prefix(RoomTempTracker __instance)
		{
			// 타일 기반온도라 필요없는 함수
			// 쓸데없는 로직이 돌지 않게 하기 위해서 기존 로직 빼버린다
			return false;
		}
	}

	[HarmonyPatch(typeof(GenTemperature), nameof(GenTemperature.EqualizeTemperaturesThroughBuilding))]
	public static class GenTemperature_EqualizeTemperaturesThroughBuilding
	{
		public static bool Prefix(Building b, float rate, bool twoWay)
		{
			// 타일 기반온도라 필요없는 함수
			// 쓸데없는 로직이 돌지 않게 하기 위해서 기존 로직 빼버린다
			return false;
		}
	}

	[HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
	public static class Building_SpawnSetup
	{
		public static void Postfix(Building __instance)
		{
			var door = __instance as Building_Door;
			if (door != null)
			{
				VentilationUtil.UpdateDoor(door);
				return;
			}
			var vent = __instance as Building_Vent;
			if (vent != null)
			{
				VentilationUtil.UpdateVent(vent);
			}
		}
	}

	[HarmonyPatch(typeof(Building), nameof(Building.DeSpawn))]
	public static class Building_DeSpawn
	{
		public static void Postfix(Building __instance, DestroyMode mode)
		{
			var door = __instance as Building_Door;
			if (door != null)
			{
				VentilationUtil.UpdateDoor(door);
				return;
			}
			var vent = __instance as Building_Vent;
			if (vent != null)
			{
				VentilationUtil.UpdateVent(vent);
			}
		}
	}

	[HarmonyPatch(typeof(Building_Vent), "TickRare")]
	public static class Building_Vent_TickRare
	{
		public static void Postfix(Building_Vent __instance)
		{
			VentilationUtil.UpdateVent(__instance);
		}
	}

	[HarmonyPatch(typeof(Building_Door), "DoorOpen")]
	public static class Building_Door_DoorOpen
	{
		public static void Postfix(Building_Door __instance, int ticksToClose)
		{
			VentilationUtil.UpdateDoor(__instance);
		}
	}

	[HarmonyPatch(typeof(Building_Door), "DoorTryClose")]
	public static class Building_Door_DoorTryClose
	{
		public static void Postfix(Building_Door __instance, ref bool __result)
		{
			VentilationUtil.UpdateDoor(__instance);
		}
	}

	[HarmonyPatch(typeof(CellBoolDrawer), nameof(CellBoolDrawer.CellBoolDrawerUpdate))]
	public static class CellBoolDrawer_CellBoolDrawerUpdate
	{
		public static bool Prefix(CellBoolDrawer __instance)
		{
			var temperatureCellBoolDrawer = __instance as TemperatureCellBoolDrawer;
			if (temperatureCellBoolDrawer != null)
			{
				temperatureCellBoolDrawer.TemperatureCellBoolDrawer_CellBoolDrawerUpdate();
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(CellBoolDrawer), nameof(CellBoolDrawer.RegenerateMesh))]
	public static class CellBoolDrawer_RegenerateMesh
	{
		public static bool Prefix(CellBoolDrawer __instance)
		{
			var temperatureCellBoolDrawer = __instance as TemperatureCellBoolDrawer;
			if (temperatureCellBoolDrawer != null)
			{
				temperatureCellBoolDrawer.TemperatureCellBoolDrawer_RegenerateMesh();
				return false;
			}

			return true;
		}
	}
}