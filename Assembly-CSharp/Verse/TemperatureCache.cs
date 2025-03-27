using System.Collections.Generic;

namespace Verse;

public sealed class TemperatureCache : IExposable
{
	private Map map;

	internal TemperatureSaveLoad temperatureSaveLoad;

	public CachedTempInfo[] tempCache;

	private HashSet<int> processedRoomIDs = new HashSet<int>();

	private List<CachedTempInfo> relevantTempInfoList = new List<CachedTempInfo>();

	public TemperatureCache(Map map)
	{
		this.map = map;
		tempCache = new CachedTempInfo[map.cellIndices.NumGridCells];
		temperatureSaveLoad = new TemperatureSaveLoad(map);
	}

	public void ResetTemperatureCache()
	{
		int numGridCells = map.cellIndices.NumGridCells;
		for (int i = 0; i < numGridCells; i++)
		{
			tempCache[i].Reset();
		}
	}

	public void ExposeData()
	{
		temperatureSaveLoad.DoExposeWork();
	}

	public void ResetCachedCellInfo(IntVec3 c)
	{
		tempCache[map.cellIndices.CellToIndex(c)].Reset();
	}

	private void SetCachedCellInfo(IntVec3 c, CachedTempInfo info)
	{
		tempCache[map.cellIndices.CellToIndex(c)] = info;
	}

	public void TryCacheRegionTempInfo(IntVec3 c, Region reg)
	{
		Room room = reg.Room;
		if (room != null)
		{
			SetCachedCellInfo(c, new CachedTempInfo(room.ID, room.CellCount, room.Temperature));
		}
	}

	public bool TryGetAverageCachedRoomTemp(Room r, out float result)
	{
		CellIndices cellIndices = map.cellIndices;
		foreach (IntVec3 cell in r.Cells)
		{
			CachedTempInfo item = map.temperatureCache.tempCache[cellIndices.CellToIndex(cell)];
			if (item.numCells > 0 && !processedRoomIDs.Contains(item.roomID))
			{
				relevantTempInfoList.Add(item);
				processedRoomIDs.Add(item.roomID);
			}
		}
		int num = 0;
		float num2 = 0f;
		foreach (CachedTempInfo relevantTempInfo in relevantTempInfoList)
		{
			num += relevantTempInfo.numCells;
			num2 += relevantTempInfo.temperature * (float)relevantTempInfo.numCells;
		}
		result = num2 / (float)num;
		bool result2 = !relevantTempInfoList.NullOrEmpty();
		processedRoomIDs.Clear();
		relevantTempInfoList.Clear();
		return result2;
	}
}
