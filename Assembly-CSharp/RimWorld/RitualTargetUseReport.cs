using Verse;

namespace RimWorld;

public struct RitualTargetUseReport
{
	public bool canUse;

	public string failReason;

	public bool ShouldShowGizmo
	{
		get
		{
			if (!canUse)
			{
				return !failReason.NullOrEmpty();
			}
			return true;
		}
	}

	public static implicit operator RitualTargetUseReport(bool canUse)
	{
		RitualTargetUseReport result = default(RitualTargetUseReport);
		result.canUse = canUse;
		result.failReason = null;
		return result;
	}

	public static implicit operator RitualTargetUseReport(string failReason)
	{
		RitualTargetUseReport result = default(RitualTargetUseReport);
		result.canUse = false;
		result.failReason = failReason;
		return result;
	}

	public static implicit operator RitualTargetUseReport(TaggedString failReason)
	{
		RitualTargetUseReport result = default(RitualTargetUseReport);
		result.canUse = false;
		result.failReason = failReason;
		return result;
	}
}
