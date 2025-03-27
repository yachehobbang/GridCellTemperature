using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace RimWorld;

public class ShipJob_Unload : ShipJob
{
	public TransportShipDropMode dropMode = TransportShipDropMode.All;

	private List<Thing> droppedThings = new List<Thing>();

	public bool unforbidAll = true;

	public static readonly IntVec3 DropoffSpotOffset = IntVec3.South * 2;

	private const int DropInterval = 60;

	protected override bool ShouldEnd => dropMode == TransportShipDropMode.None;

	public override bool ShowGizmos => false;

	private Thing ShipThing => transportShip.shipThing;

	public override bool TryStart()
	{
		if (!transportShip.ShipExistsAndIsSpawned)
		{
			return false;
		}
		return base.TryStart();
	}

	public override void Tick()
	{
		base.Tick();
		if (ShipThing.IsHashIntervalTick(60))
		{
			Drop();
		}
	}

	private void Drop()
	{
		Thing thingToDrop = null;
		float num = 0f;
		Map map = ShipThing.Map;
		for (int i = 0; i < transportShip.TransporterComp.innerContainer.Count; i++)
		{
			Thing thing = transportShip.TransporterComp.innerContainer[i];
			float dropPriority = GetDropPriority(thing);
			if (dropPriority > num)
			{
				thingToDrop = thing;
				num = dropPriority;
			}
		}
		if (thingToDrop != null)
		{
			IntVec3 dropLoc = ShipThing.Position + DropoffSpotOffset;
			if (!transportShip.TransporterComp.innerContainer.TryDrop(thingToDrop, dropLoc, map, ThingPlaceMode.Near, out var _, null, delegate(IntVec3 c)
			{
				if (c.Fogged(map))
				{
					return false;
				}
				return (!(thingToDrop is Pawn { Downed: not false }) || c.GetFirstPawn(map) == null) ? true : false;
			}, !(thingToDrop is Pawn)))
			{
				return;
			}
			transportShip.TransporterComp.Notify_ThingRemoved(thingToDrop);
			droppedThings.Add(thingToDrop);
			if (unforbidAll)
			{
				thingToDrop.SetForbidden(value: false, warnOnFail: false);
			}
			if (thingToDrop is Pawn pawn2)
			{
				if (pawn2.IsColonist && pawn2.Spawned && !map.IsPlayerHome)
				{
					pawn2.drafter.Drafted = true;
				}
				if (pawn2.guest != null && pawn2.guest.IsPrisoner)
				{
					pawn2.guest.WaitInsteadOfEscapingForDefaultTicks();
				}
			}
		}
		else
		{
			transportShip.TransporterComp.TryRemoveLord(map);
			End();
		}
	}

	private float GetDropPriority(Thing t)
	{
		if (t is Pawn p)
		{
			if (droppedThings.Contains(t))
			{
				return 0f;
			}
			if (dropMode == TransportShipDropMode.NonRequired && transportShip.ShuttleComp.IsRequired(t))
			{
				return 0f;
			}
			Lord lord = p.GetLord();
			if (lord?.CurLordToil != null && lord.CurLordToil is LordToil_EnterShuttleOrLeave lordToil_EnterShuttleOrLeave && lordToil_EnterShuttleOrLeave.shuttle == ShipThing)
			{
				return 0f;
			}
			if (!p.AnimalOrWildMan())
			{
				return 1f;
			}
			return 0.5f;
		}
		return 0.25f;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref droppedThings, "droppedThings", LookMode.Reference);
		Scribe_Values.Look(ref dropMode, "dropMode", TransportShipDropMode.None);
		Scribe_Values.Look(ref unforbidAll, "unforbidAll", defaultValue: true);
	}
}
