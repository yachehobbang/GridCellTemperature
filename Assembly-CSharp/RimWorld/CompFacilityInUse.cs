using System.Collections.Generic;
using System.Text;
using Verse;

namespace RimWorld;

public class CompFacilityInUse : ThingComp
{
	[Unsaved(false)]
	private bool operatingAtHighPower;

	[Unsaved(false)]
	private Effecter effecterInUse;

	public CompProperties_FacilityInUse Props => props as CompProperties_FacilityInUse;

	public override void PostDeSpawn(Map map)
	{
		base.PostDeSpawn(map);
		effecterInUse?.Cleanup();
		effecterInUse = null;
	}

	public override void CompTick()
	{
		base.CompTick();
		DoTick();
	}

	public override void CompTickRare()
	{
		base.CompTickRare();
		DoTick();
	}

	public override void CompTickLong()
	{
		base.CompTickLong();
		DoTick();
	}

	private void DoTick()
	{
		List<Thing> list = parent.TryGetComp<CompFacility>()?.LinkedBuildings;
		if (list == null)
		{
			return;
		}
		CompPowerTrader compPowerTrader = parent.TryGetComp<CompPowerTrader>();
		Thing thing = null;
		foreach (Thing item in list)
		{
			if (compPowerTrader.PowerOn && BuildingInUse(item))
			{
				thing = item;
				break;
			}
		}
		bool flag = thing != null;
		operatingAtHighPower = false;
		if (Props.inUsePowerConsumption.HasValue)
		{
			float num = compPowerTrader.Props.PowerConsumption;
			if (flag)
			{
				num = Props.inUsePowerConsumption.Value;
				operatingAtHighPower = true;
			}
			compPowerTrader.PowerOutput = 0f - num;
		}
		if (Props.effectInUse == null)
		{
			return;
		}
		if (flag)
		{
			if (effecterInUse == null)
			{
				effecterInUse = Props.effectInUse.Spawn();
				effecterInUse.Trigger(parent, thing);
			}
			effecterInUse.EffectTick(parent, thing);
		}
		if (!flag && effecterInUse != null)
		{
			effecterInUse.Cleanup();
			effecterInUse = null;
		}
	}

	private bool BuildingInUse(Thing building)
	{
		if (building is Building_Bed { AnyOccupants: not false })
		{
			return true;
		}
		return false;
	}

	public override string CompInspectStringExtra()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("PowerConsumptionMode".Translate() + ": ");
		if (operatingAtHighPower)
		{
			stringBuilder.Append("PowerConsumptionHigh".Translate().CapitalizeFirst());
		}
		else
		{
			stringBuilder.Append("PowerConsumptionLow".Translate().CapitalizeFirst());
		}
		return stringBuilder.ToString();
	}
}
