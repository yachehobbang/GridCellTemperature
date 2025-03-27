namespace RimWorld;

public abstract class BaseCreepJoinerDownsideWorker : BaseCreepJoinerWorker
{
	public virtual bool CanOccur()
	{
		return true;
	}
}
