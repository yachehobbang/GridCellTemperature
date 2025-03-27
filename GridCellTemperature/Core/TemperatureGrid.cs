﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GridCellTemperature.Core
{
	public class TemperatureGrid : IExposable
	{
		public static TemperatureGrid TemperatureGridInstance;

		private readonly float[] temperatures1;

		private readonly float[] temperatures2;

		private readonly bool[] airCellGrid;

		private readonly float?[] ventilationGrids;

		private readonly float[] pushHeatGrid;

		private Queue<(IntVec3 cell, float energy)> PushHeatWaitQueue = new Queue<(IntVec3 cell, float energy)>();

		private int currentIndex = 0;

		private readonly Map map;

		private bool isProcessing = false;

		private bool isAsyncProcessing = false;

		private class RoomTemperature
		{
			public float Temperature = 0;
			public float TemperatureOffset = 0f;
			public float coefficient = 0f;
			public List<IntVec3> Cells = new List<IntVec3>();
			public bool UsesOutdoorTemperature = false;
			public bool IsValid = false;
		}

		private readonly Dictionary<int, RoomTemperature> roomTemperatures = new Dictionary<int, RoomTemperature>();

		private const float MaxTemperature = 1000f;
		private const float MinTemperature = -273.15f;

		public bool ResetTemperatureFlag = false;

		private string cachedOriginTemperatureString;

		private float cachedCellTemperatureStringForTemperature;
		private string cachedCellTemperatureString;

		private string cachedFinalTemperatureString;

		private float _tickStart = 0f;
		private float _tickTime = 0f;

		public Map Map
		{
			get { return map; }
		}

		private float[] CurrentTemperatures
		{
			get
			{
				if (currentIndex == 0)
				{
					return temperatures1;
				}
				else
				{
					return temperatures2;
				}
			}
		}

		private float[] NextTemperatures
		{
			get
			{
				if (currentIndex == 0)
				{
					return temperatures2;
				}
				else
				{
					return temperatures1;
				}
			}
		}
		public TemperatureGrid(Map map)
		{
			this.map = map;
			var num = map.cellIndices.NumGridCells;
			temperatures1 = new float[num];
			temperatures2 = new float[num];
			airCellGrid = new bool[num];
			ventilationGrids = new float?[num];
			pushHeatGrid = new float[num];

			ResetTemperatureFlag = true;
		}

		public void OnFinalizeInit()
		{
			if (!ResetTemperatureFlag)
			{
				return;
			}
			ResetTemperatureFlag = false;

			var cellIndices = map.cellIndices;
			// 모드가 중간에 추가되었으면 기존 온도로 초기화 시켜준다
			var outdoorTemperature = map.mapTemperature.OutdoorTemp;
			var temperatures = CurrentTemperatures;
			for (var i = 0; i < temperatures.Length; i++)
			{
				temperatures[i] = outdoorTemperature;
			}
			foreach (var room in map.regionGrid.allRooms)
			{
				if (room.UsesOutdoorTemperature)
				{
					continue;
				}

				var roomTemperature = room.Temperature;
				foreach (var cell in room.Cells)
				{
					var index = cellIndices.CellToIndex(cell);
					temperatures[index] = roomTemperature;
				}
			}
		}

		public void MarkAsDirtyTemperatureGrid()
		{
			if (isAsyncProcessing)
			{
				return;
			}

			if (Settings.viewSimTickTime.Value)
			{
				_tickStart = Time.realtimeSinceStartup;
			}

			var mapSizeX = map.Size.x;

			foreach (var (roomId, roomTemperature) in roomTemperatures)
			{
				roomTemperature.IsValid = false;
			}

			Array.Clear(airCellGrid, 0, airCellGrid.Length);

			foreach (var room in map.regionGrid.allRooms)
			{
				var cells = room.Cells;

				if (!roomTemperatures.TryGetValue(room.ID, out RoomTemperature roomTemperature))
				{
					roomTemperature = new RoomTemperature();
					roomTemperatures.Add(room.ID, roomTemperature);
				}

				roomTemperature.IsValid = true;
				roomTemperature.Cells.Clear();
				roomTemperature.Cells.AddRange(cells);
				roomTemperature.UsesOutdoorTemperature = room.UsesOutdoorTemperature;

				foreach (var cell in cells)
				{
					var index = CellIndicesUtility.CellToIndex(cell, mapSizeX);
					airCellGrid[index] = true;
				}
			}

			foreach (var (cell, energy) in PushHeatWaitQueue)
			{
				PushHeat_Impl(cell, energy);
			}
			PushHeatWaitQueue.Clear();

			isAsyncProcessing = true;
			Task.Run(AsyncTickTask);
		}

		public void GetTemperatureString(ref string __result)
		{
			var c = UI.MouseCell();

			if (c.x < 0 || c.z < 0 || c.x >= Map.Size.x || c.z >= Map.Size.z)
			{
				return;
			}

			var cellTemperature = GetTemperature(c);

			var cellTemperature1 = Mathf.RoundToInt(GenTemperature.CelsiusTo(cachedCellTemperatureStringForTemperature, Prefs.TemperatureMode));
			var cellTemperature2 = Mathf.RoundToInt(GenTemperature.CelsiusTo(cellTemperature, Prefs.TemperatureMode));

			var isDirtyText = cachedOriginTemperatureString != __result;
			cachedOriginTemperatureString = __result;

			if (cellTemperature1 != cellTemperature2)
			{
				cachedCellTemperatureStringForTemperature = cellTemperature;
				cachedCellTemperatureString = cellTemperature.ToStringTemperature("F0");

				isDirtyText = true;
			}

			if (isDirtyText)
			{
				cachedFinalTemperatureString = cachedCellTemperatureString + " | " + cachedOriginTemperatureString;
			}


			if (Settings.viewSimTickTime.Value)
			{
				__result = $"{_tickTime:F4} | {cachedFinalTemperatureString}";
			}
			else
			{
				__result = cachedFinalTemperatureString;
			}
		}

		public Task AsyncTickTask()
		{
			try
			{
				NextTick();
			}
			catch (Exception e)
			{
				Mod.Warning(e.ToString());
			}
			finally
			{
				isAsyncProcessing = false;
			}

			if (Settings.viewSimTickTime.Value)
			{
				_tickTime = Time.realtimeSinceStartup - _tickStart;
			}
			return Task.CompletedTask;
		}

		public static float GetTemperature(Map map, IntVec3 cell)
		{
			var grid = TemperatureGridInstance;
			if (grid == null)
			{
				return map.mapTemperature.OutdoorTemp;
			}

			return grid.GetTemperature(cell);
		}
		public float GetTemperature(IntVec3 cell)
		{
			var index = CellIndicesUtility.CellToIndex(cell, map.Size.x);
			return CurrentTemperatures[index];
		}

		public static float GetRoomAverageTemperature(Map map, int roomId)
		{
			var grid = TemperatureGridInstance;
			if (grid == null)
			{
				return map.mapTemperature.OutdoorTemp;
			}

			return grid.GetRoomAverageTemperature(roomId);
		}
		public float GetRoomAverageTemperature(int roomId)
		{
			if (roomTemperatures.TryGetValue(roomId, out var item))
			{
				return item.Temperature;
			}
			return map.mapTemperature.OutdoorTemp;
		}

		public void SetRoomAverageTemperature(int roomId, float temperature)
		{
			if (roomTemperatures.TryGetValue(roomId, out var item))
			{
				// 초기화 상황이 아니면 세팅된 온도의 차이만큼 방 전체온도를 낮춘다
				var d = temperature - item.Temperature;
				item.TemperatureOffset += d;
			}
			else
			{
				item = new RoomTemperature();
				roomTemperatures.Add(roomId, item);
			}
			item.Temperature = temperature;
		}

		public static void PushHeat(IntVec3 cell, float energy)
		{
			var grid = TemperatureGridInstance;
			if (grid == null)
			{
				return;
			}

			grid.PushHeat_Impl(cell, energy);
		}

		public void PushHeat_Impl(IntVec3 cell, float energy)
		{
			// nexttick이 비동기도 처리되어서 처리중이면 끝날때 까지 기다렸다가 반영함
			if (isProcessing)
			{
				PushHeatWaitQueue.Enqueue((cell, energy));
				return;
			}

			var index = CellToIndex(cell.x, cell.z, map.Size);
			if (index == null)
			{
				return;
			}

			pushHeatGrid[index.Value] += energy;
		}

		public static void SetVentilationCell(IntVec3 c, float? value)
		{
			var grid = TemperatureGridInstance;
			if (grid == null)
			{
				return;
			}

			grid.SetVentilationCell_Impl(c, value);
		}

		public void SetVentilationCell_Impl(IntVec3 c, float? value)
		{
			var index = CellIndicesUtility.CellToIndex(c, map.Size.x);
			ventilationGrids[index] = value;
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				ResetTemperatureFlag = false;
			}

			var curTemperatures = CurrentTemperatures;
			var temperatures = new float[curTemperatures.Length];

			Array.Copy(curTemperatures, temperatures, curTemperatures.Length);

			const float ExposeConstant = 100f;
			MapExposeUtility.ExposeInt(map, (IntVec3 c) => (int)(temperatures[map.cellIndices.CellToIndex(c)] * ExposeConstant), delegate (IntVec3 c, int val)
			{
				temperatures[map.cellIndices.CellToIndex(c)] = val / ExposeConstant;
			}, "gridTemperatures");

			Array.Copy(temperatures, CurrentTemperatures, temperatures.Length);
		}

		public void NextTick()
		{
			var size = map.Size;

			var prev = CurrentTemperatures;
			var next = NextTemperatures;
			var outdoorTemperature = map.mapTemperature.OutdoorTemp;
			var roofGrid = map.roofGrid;

			isProcessing = true;
			{
				Parallel.For(0, pushHeatGrid.Length,
					i =>
					{
						var heat = pushHeatGrid[i];
						if (heat == 0f)
						{
							return;
						}

						const float MaxHeatPushPerTick = 20f;

						var absHeat = Math.Abs(heat);
						if (absHeat > MaxHeatPushPerTick)
						{
							heat = Mathf.Sign(heat) * Mathf.Max(absHeat * 0.4f, MaxHeatPushPerTick);
							pushHeatGrid[i] -= heat;
						}
						else
						{
							pushHeatGrid[i] = 0f;
						}

						if (!airCellGrid[i])
						{
							heat *= Settings.wallDiffusivity.Value;
						}
						prev[i] += heat;
					});

				Parallel.For(0, prev.Length,
					i =>
					{
						var x = i % size.x;
						var y = i / size.x;

						var temperature = NextCellTemperature(x, y, prev, size, roofGrid, outdoorTemperature);
						next[i] = temperature;
					});

				Parallel.ForEach(roomTemperatures,
					(item) =>
					{
						var roomTemperature = item.Value;
						if (roomTemperature.UsesOutdoorTemperature)
						{
							return;
						}

						float sum = 0f;
						var rt = roomTemperature.Temperature;
						var offset = roomTemperature.TemperatureOffset;
						roomTemperature.TemperatureOffset = 0f;
						foreach (var cell in roomTemperature.Cells)
						{
							var cellIndex = CellIndicesUtility.CellToIndex(cell, size.x);
							var temperature = next[cellIndex] + offset;
							if (roomTemperature.coefficient > 0f)
							{
								var d = rt - temperature;
								temperature += d * roomTemperature.coefficient;
							}

							next[cellIndex] = temperature;
							sum += temperature;
						}

						var cellCount = roomTemperature.Cells.Count;
						if (cellCount > 0)
						{
							var newRoomAvgTemperature = sum / cellCount;
							roomTemperature.Temperature = newRoomAvgTemperature;
							roomTemperature.coefficient = Settings.roomDiffusivity.Value;
						}
					});

				foreach (var key in roomTemperatures.Where(kv => !kv.Value.IsValid).Select(kv => kv.Key).ToList())
				{
					roomTemperatures.Remove(key);
				}

				currentIndex = (currentIndex + 1) % 2;
			}
			isProcessing = false;

			var temperatureDrawer = map.mapTemperature.Drawer as TemperatureCellBoolDrawer;
			if (temperatureDrawer != null)
			{
				temperatureDrawer.MarkTemperatureAsDirty();
			}
		}

		private unsafe float NextCellTemperature(int x, int y,
			float[] tempGrid, IntVec3 size, RoofGrid roofGrid, float outdoorTemperature)
		{
			var index = CellToIndex(x, y, size);

			var cellTemperature = tempGrid[index.Value];

			var ventilation = ventilationGrids[index.Value];

			bool cellIsWall;
			if (ventilation.HasValue)
			{
				cellIsWall = false;
			}
			else
			{
				cellIsWall = !airCellGrid[index.Value];
			}

			var roofDef = roofGrid.RoofAt(index.Value);

			float roofWeight;
			if (roofDef == null)
			{
				roofWeight = Settings.skyDiffusivity.Value;
			}
			else
			{
				roofWeight = roofDef.isThickRoof ? Settings.thickRoofDiffusivity.Value : Settings.roofDiffusivity.Value;
			}

			if (cellIsWall)
			{
				roofWeight = Mathf.Min(roofWeight, Settings.roofDiffusivity.Value);
			}

			var temperatures = stackalloc float[4];
			var weights = stackalloc float[4];
			(temperatures[0], weights[0]) = GetCellInfo(x + 1, y, size, cellIsWall, tempGrid, outdoorTemperature);
			(temperatures[1], weights[1]) = GetCellInfo(x, y + 1, size, cellIsWall, tempGrid, outdoorTemperature);
			(temperatures[2], weights[2]) = GetCellInfo(x - 1, y, size, cellIsWall, tempGrid, outdoorTemperature);
			(temperatures[3], weights[3]) = GetCellInfo(x, y - 1, size, cellIsWall, tempGrid, outdoorTemperature);

			var cellEnergy = cellTemperature - outdoorTemperature;
			var cellEnergyNormal = Mathf.Pow(Mathf.Abs(cellEnergy), Settings.baseHeatTransferCoefficient.Value) * Mathf.Sign(cellEnergy);
			var sum = 0f;
			for (var i = 0; i < 4; i++)
			{
				var targetCellEnergy = temperatures[i] - outdoorTemperature;
				var targetCellEnergyNormal = Mathf.Pow(Mathf.Abs(targetCellEnergy), Settings.baseHeatTransferCoefficient.Value) * Mathf.Sign(targetCellEnergy);

				var d = (targetCellEnergyNormal - cellEnergyNormal) * weights[i];
				sum += d;
			}
			var newValue = cellEnergyNormal + sum * 0.2f;
			newValue *= 1f - roofWeight;
			var result = Mathf.Pow(Mathf.Abs(newValue), 1f / Settings.baseHeatTransferCoefficient.Value) * Mathf.Sign(newValue) + outdoorTemperature;

			if (result > MaxTemperature)
			{
				result = MaxTemperature;
			}
			else if (result < MinTemperature)
			{
				result = MinTemperature;
			}

			return result;
		}

		private (float temperature, float weight) GetCellInfo(int x, int y, IntVec3 size,
			bool baseCellIsWall,
			float[] tempGrid,
			float defaultTemperature)
		{
			var index = CellToIndex(x, y, size);
			float temperature;
			bool isWall;
			if (!index.HasValue)
			{
				temperature = defaultTemperature;
				isWall = false;
			}
			else
			{
				temperature = tempGrid[index.Value];
				var ventilation = ventilationGrids[index.Value];
				// 공기 통로가 있는 곳은(벤트, 열린문) 따로 처리
				if (ventilation.HasValue)
				{
					isWall = false;
					if (!baseCellIsWall)
					{
						return (temperature, ventilation.Value);
					}
				}
				else
				{
					isWall = !airCellGrid[index.Value];
				}
			}

			if (baseCellIsWall == isWall)
			{
				if (baseCellIsWall)
				{
					return (temperature, Settings.wallMassDiffusivity.Value);
				}
				else
				{
					return (temperature, Settings.airDiffusivity.Value);
				}
			}
			else
			{
				return (temperature, Settings.wallDiffusivity.Value);
			}
		}

		private int? CellToIndex(int x, int y, IntVec3 size)
		{
			if (x < 0 || y < 0 || x >= size.x || y >= size.z)
			{
				return null;
			}

			return CellIndicesUtility.CellToIndex(x, y, size.x);
		}
	}
}
