namespace Verse;

public abstract class RoomRoleWorker
{
	public virtual string PostProcessedLabel(string baseLabel, Room room)
	{
		return baseLabel;
	}

	public abstract float GetScore(Room room);
}
