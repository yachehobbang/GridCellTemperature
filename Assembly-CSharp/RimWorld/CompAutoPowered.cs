using Verse;

namespace RimWorld;

public abstract class CompAutoPowered : ThingComp
{
	public const string AutoPoweredWantsOffSignal = "AutoPoweredWantsOff";

	protected CompPowerTrader compPowerCached;

	private OverlayHandle? overlayNeedsPower;

	public bool AppearsPowered
	{
		get
		{
			if (compPowerCached != null && compPowerCached.PowerNet != null && compPowerCached.PowerNet.HasActivePowerSource && compPowerCached.PowerNet.CanPowerNow(compPowerCached))
			{
				return !parent.MapHeld.gameConditionManager.ElectricityDisabled(parent.MapHeld);
			}
			return false;
		}
	}

	public abstract bool WantsToBeOn { get; }

	protected void UpdateOverlays()
	{
		if (parent.Spawned)
		{
			parent.Map.overlayDrawer.Disable(parent, ref overlayNeedsPower);
			if (!parent.IsBrokenDown() && !AppearsPowered && !overlayNeedsPower.HasValue)
			{
				overlayNeedsPower = parent.Map.overlayDrawer.Enable(parent, OverlayTypes.NeedsPower);
			}
		}
	}
}
