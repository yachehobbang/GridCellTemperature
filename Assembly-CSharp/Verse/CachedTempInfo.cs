namespace Verse;

public struct CachedTempInfo
{
	public int roomID;

	public int numCells;

	public float temperature;

	public static CachedTempInfo NewCachedTempInfo()
	{
		CachedTempInfo result = default(CachedTempInfo);
		result.Reset();
		return result;
	}

	public void Reset()
	{
		roomID = -1;
		numCells = 0;
		temperature = 0f;
	}

	public CachedTempInfo(int roomID, int numCells, float temperature)
	{
		this.roomID = roomID;
		this.numCells = numCells;
		this.temperature = temperature;
	}
}
