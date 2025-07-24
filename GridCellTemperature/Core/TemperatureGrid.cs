using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GridCellTemperature.Core
{
	public class TemperatureGrid : IExposable
	{
		public static readonly Dictionary<int, TemperatureGrid> temperatureGrids = [];

		private readonly float[] _temperatures1;

		private readonly float[] _temperatures2;

		private readonly bool[] _airCellGrid;

		private readonly float?[] _ventilationGrids;

		private readonly float[] _pushHeatGrid;

		private readonly Queue<(IntVec3 cell, float energy)> _pushHeatWaitQueue = new();

		private int _currentIndex = 0;

		private readonly Map _map;

		private bool _dataLock = false;

		private class RoomTemperature
		{
			public float Temperature = 0;
			public float TemperatureOffset = 0f;
			public readonly List<IntVec3> Cells = [];
			public bool IsValid = false;
		}

		private readonly Dictionary<int, RoomTemperature> _roomTemperatures = [];

		private const float MaxTemperature = 1000f;
		private const float MinTemperature = -273.15f;

		private bool _resetTemperatureFlag = false;

		private string _cachedOriginTemperatureString;

		private float _cachedCellTemperatureStringForTemperature;
		private string _cachedCellTemperatureString;

		private string _cachedFinalTemperatureString;

		private static bool _isRunningSimulation = false;
		private static float _tickStart = 0f;
		private static float _tickTime = 0f;

		public Map Map
		{
			get { return _map; }
		}

		private float[] CurrentTemperatures
		{
			get
			{
				if (_currentIndex == 0)
				{
					return _temperatures1;
				}
				else
				{
					return _temperatures2;
				}
			}
		}

		private float[] NextTemperatures
		{
			get
			{
				if (_currentIndex == 0)
				{
					return _temperatures2;
				}
				else
				{
					return _temperatures1;
				}
			}
		}
		public TemperatureGrid(Map map)
		{
			this._map = map;
			var num = map.cellIndices.NumGridCells;
			_temperatures1 = new float[num];
			_temperatures2 = new float[num];
			_airCellGrid = new bool[num];
			_ventilationGrids = new float?[num];
			_pushHeatGrid = new float[num];

			temperatureGrids.SetOrAdd(map.uniqueID, this);

			_resetTemperatureFlag = true;
		}

		public static TemperatureGrid GetTemperatureGrid(Map map)
		{
			if (map != null && temperatureGrids.TryGetValue(map.uniqueID, out var grid))
			{
				return grid;
			}

			return null;
		}

		public void OnFinalizeInit()
		{
			if (!_resetTemperatureFlag)
			{
				return;
			}
			_resetTemperatureFlag = false;

			var cellIndices = _map.cellIndices;
			// 모드가 중간에 추가되었으면 기존 온도로 초기화 시켜준다
			var outdoorTemperature = _map.mapTemperature.OutdoorTemp;
			var temperatures = CurrentTemperatures;
			for (var i = 0; i < temperatures.Length; i++)
			{
				temperatures[i] = outdoorTemperature;
			}
			foreach (var room in _map.regionGrid.AllRooms)
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

		public static void RunTemperatureSimulation()
		{
			if (_isRunningSimulation)
			{
				return;
			}
			_isRunningSimulation = true;

			if (Settings.viewSimTickTime.Value)
			{
				_tickStart = Time.realtimeSinceStartup;
			}

			foreach (var (map, grid) in temperatureGrids)
			{
				grid.PreSimulationTick();
			}

			Task.Run(AsyncTemperatureSimulation);
		}

		private static Task AsyncTemperatureSimulation()
		{
			try
			{
				List<Task> tasks = [];
				foreach (var (map, grid) in temperatureGrids)
				{
					tasks.Add(grid.AsyncSimulationTick());
				}

				Task.WaitAll([.. tasks]);
			}
			catch (Exception e)
			{
				Mod.Warning(e.ToString());
			}
			finally
			{
				_isRunningSimulation = false;
			}

			if (Settings.viewSimTickTime.Value)
			{
				_tickTime = Time.realtimeSinceStartup - _tickStart;
			}

			return Task.CompletedTask;
		}

		public void GetTemperatureString(ref string __result)
		{
			var c = UI.MouseCell();

			if (c.x < 0 || c.z < 0 || c.x >= Map.Size.x || c.z >= Map.Size.z)
			{
				return;
			}

			var cellTemperature = GetTemperature(c);

			var cellTemperature1 = Mathf.RoundToInt(GenTemperature.CelsiusTo(_cachedCellTemperatureStringForTemperature, Prefs.TemperatureMode));
			var cellTemperature2 = Mathf.RoundToInt(GenTemperature.CelsiusTo(cellTemperature, Prefs.TemperatureMode));

			var isDirtyText = _cachedOriginTemperatureString != __result;
			_cachedOriginTemperatureString = __result;

			if (cellTemperature1 != cellTemperature2)
			{
				_cachedCellTemperatureStringForTemperature = cellTemperature;
				_cachedCellTemperatureString = cellTemperature.ToStringTemperature("F0");

				isDirtyText = true;
			}

			if (isDirtyText)
			{
				_cachedFinalTemperatureString = _cachedCellTemperatureString + " | " + _cachedOriginTemperatureString;
			}


			if (Settings.viewSimTickTime.Value)
			{
				__result = $"{_tickTime:F4} | {_cachedFinalTemperatureString}";
			}
			else
			{
				__result = _cachedFinalTemperatureString;
			}
		}

		public static float GetTemperature(Map map, IntVec3 cell)
		{
			var grid = GetTemperatureGrid(map);
			if (grid == null)
			{
				return map.mapTemperature.OutdoorTemp;
			}

			return grid.GetTemperature(cell);
		}
		public float GetTemperature(IntVec3 cell)
		{
			var index = CellIndicesUtility.CellToIndex(cell, _map.Size.x);
			return CurrentTemperatures[index];
		}

		public static float GetRoomAverageTemperature(Map map, int roomId)
		{
			var grid = GetTemperatureGrid(map);
			if (grid == null)
			{
				return map.mapTemperature.OutdoorTemp;
			}

			return grid.GetRoomAverageTemperature(roomId);
		}
		public float GetRoomAverageTemperature(int roomId)
		{
			if (_roomTemperatures.TryGetValue(roomId, out var item))
			{
				return item.Temperature;
			}
			return _map.mapTemperature.OutdoorTemp;
		}

		public void SetRoomAverageTemperature(int roomId, float temperature)
		{
			if (_roomTemperatures.TryGetValue(roomId, out var item))
			{
				// 초기화 상황이 아니면 세팅된 온도의 차이만큼 방 전체온도를 낮춘다
				var d = temperature - item.Temperature;
				item.TemperatureOffset += d;
				return;
			}

			item = new RoomTemperature();
			_roomTemperatures.Add(roomId, item);
			item.Temperature = temperature;
		}

		public static void PushHeat(Map map, IntVec3 cell, float energy)
		{
			var grid = GetTemperatureGrid(map);
			if (grid == null)
			{
				return;
			}

			grid.PushHeat(cell, energy);
		}

		public void PushHeat(IntVec3 cell, float energy)
		{
			// nexttick이 비동기로 처리되어서 처리중이면 끝날때 까지 기다렸다가 반영함
			if (_dataLock)
			{
				_pushHeatWaitQueue.Enqueue((cell, energy));
				return;
			}

			var index = CellToIndex(cell.x, cell.z, _map.Size);
			if (index == null)
			{
				return;
			}

			var baseHeatTransferCoefficient = Settings.baseHeatTransferCoefficient.Value;
			_pushHeatGrid[index.Value] += energy / baseHeatTransferCoefficient * baseHeatTransferCoefficient;
		}

		public static void SetVentilationCell(Map map, IntVec3 c, float? value)
		{
			var grid = GetTemperatureGrid(map);
			if (grid == null)
			{
				return;
			}

			grid.SetVentilationCell(c, value);
		}

		public void SetVentilationCell(IntVec3 c, float? value)
		{
			var index = CellIndicesUtility.CellToIndex(c, _map.Size.x);
			_ventilationGrids[index] = value;
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				_resetTemperatureFlag = false;
			}

			var curTemperatures = CurrentTemperatures;
			var temperatures = new float[curTemperatures.Length];

			Array.Copy(curTemperatures, temperatures, curTemperatures.Length);

			const float ExposeConstant = 100f;
			MapExposeUtility.ExposeInt(_map, (IntVec3 c) => (int)(temperatures[_map.cellIndices.CellToIndex(c)] * ExposeConstant), delegate (IntVec3 c, int val)
			{
				temperatures[_map.cellIndices.CellToIndex(c)] = val / ExposeConstant;
			}, "gridTemperatures");

			Array.Copy(temperatures, curTemperatures, temperatures.Length);
		}

		protected void PreSimulationTick()
		{
			var mapSizeX = _map.Size.x;

			foreach (var (roomId, roomTemperature) in _roomTemperatures)
			{
				roomTemperature.IsValid = false;
			}

			Array.Clear(_airCellGrid, 0, _airCellGrid.Length);

			foreach (var room in _map.regionGrid.AllRooms)
			{
				var cells = room.Cells;

				if (!room.UsesOutdoorTemperature && !room.PsychologicallyOutdoors)
				{
					if (!_roomTemperatures.TryGetValue(room.ID, out RoomTemperature roomTemperature))
					{
						roomTemperature = new RoomTemperature();
						_roomTemperatures.Add(room.ID, roomTemperature);
					}

					roomTemperature.IsValid = true;
					roomTemperature.Cells.Clear();
					roomTemperature.Cells.AddRange(cells);
				}

				foreach (var cell in cells)
				{
					var index = CellIndicesUtility.CellToIndex(cell, mapSizeX);
					_airCellGrid[index] = true;
				}
			}

			foreach (var key in _roomTemperatures.Where(kv => !kv.Value.IsValid).Select(kv => kv.Key).ToList())
			{
				_roomTemperatures.Remove(key);
			}

			foreach (var (cell, energy) in _pushHeatWaitQueue)
			{
				PushHeat(cell, energy);
			}
			_pushHeatWaitQueue.Clear();
		}

		protected Task AsyncSimulationTick()
		{
			var size = _map.Size;

			var prev = CurrentTemperatures;
			var next = NextTemperatures;
			var outdoorTemperature = _map.mapTemperature.OutdoorTemp;
			var roofGrid = _map.roofGrid;

			_dataLock = true;
			{
				Parallel.For(0, _pushHeatGrid.Length,
					i =>
					{
						var heat = _pushHeatGrid[i];
						if (heat == 0f)
						{
							return;
						}

						_pushHeatGrid[i] = 0f;

						if (!_airCellGrid[i])
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

				Parallel.ForEach(_roomTemperatures,
					(item) =>
					{
						var roomTemperature = item.Value;
						float sum = 0f;
						var rt = roomTemperature.Temperature;
						var offset = roomTemperature.TemperatureOffset;
						roomTemperature.TemperatureOffset = 0f;
						var coefficient = Settings.roomDiffusivity.Value;
						foreach (var cell in roomTemperature.Cells)
						{
							var cellIndex = CellIndicesUtility.CellToIndex(cell, size.x);
							var temperature = next[cellIndex] + offset;
							if (coefficient > 0f)
							{
								var d = rt - temperature;
								temperature += d * coefficient;
							}

							next[cellIndex] = temperature;
							sum += temperature;
						}

						var cellCount = roomTemperature.Cells.Count;
						if (cellCount > 0)
						{
							var newRoomAvgTemperature = sum / cellCount;
							roomTemperature.Temperature = newRoomAvgTemperature;
						}
					});

				_currentIndex = (_currentIndex + 1) % 2;
			}
			_dataLock = false;

			var temperatureDrawer = _map.mapTemperature.Drawer as TemperatureCellBoolDrawer;
			if (temperatureDrawer != null)
			{
				temperatureDrawer.MarkTemperatureAsDirty();
			}

			return Task.CompletedTask;
		}

		private unsafe float NextCellTemperature(int x, int y,
			float[] tempGrid, IntVec3 size, RoofGrid roofGrid, float outdoorTemperature)
		{
			var index = CellToIndex(x, y, size);

			var cellTemperature = tempGrid[index.Value];

			var ventilation = _ventilationGrids[index.Value];

			bool cellIsWall;
			if (ventilation.HasValue)
			{
				cellIsWall = false;
			}
			else
			{
				cellIsWall = !_airCellGrid[index.Value];
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

			var baseHeatTransferCoefficient = Settings.baseHeatTransferCoefficient.Value;

			var cellEnergy = cellTemperature - outdoorTemperature;
			var cellEnergySign = Mathf.Sign(cellEnergy);
			var cellEnergyValue = Mathf.Abs(cellEnergy);
			for (var c = 1; c < baseHeatTransferCoefficient; c++)
			{
				cellEnergyValue *= cellEnergyValue;
			}
			cellEnergyValue *= cellEnergySign;

			var sum = 0f;
			for (var i = 0; i < 4; i++)
			{
				var weight = weights[i];
				
				var targetCellEnergy = temperatures[i] - outdoorTemperature;
				var targetCellEnergySign = Mathf.Sign(targetCellEnergy);
				var targetCellEnergyValue = Mathf.Abs(targetCellEnergy);
				for (var c = 1; c < baseHeatTransferCoefficient; c++)
				{
					targetCellEnergyValue *= targetCellEnergyValue;
					
					weight *= weights[i];
				}
				targetCellEnergyValue *= targetCellEnergySign;

				var d = (targetCellEnergyValue - cellEnergyValue) * weight;
				sum += d;
			}
			var newValue = cellEnergyValue + sum * 0.2f;

			var resultSign = Mathf.Sign(newValue);
			newValue = Mathf.Abs(newValue);
			for (var c = 1; c < baseHeatTransferCoefficient; c++)
			{
				newValue = Mathf.Sqrt(newValue);
			}
			
			newValue *= 1f - roofWeight;

			var result = newValue * Mathf.Sign(resultSign) + outdoorTemperature;

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
				var ventilation = _ventilationGrids[index.Value];
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
					isWall = !_airCellGrid[index.Value];
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
