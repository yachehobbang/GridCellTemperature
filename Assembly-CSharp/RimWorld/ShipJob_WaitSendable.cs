using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace RimWorld;

public class ShipJob_WaitSendable : ShipJob_Wait
{
	public MapParent destination;

	private bool sentMessage;

	protected override bool ShouldEnd => false;

	public override bool HasDestination => destination != null;

	public override IEnumerable<Gizmo> GetJobGizmos()
	{
		return Enumerable.Empty<Gizmo>();
	}

	protected override void SendAway()
	{
		if (targetPlayerSettlement && (destination == null || !destination.HasMap))
		{
			Settlement settlement = null;
			foreach (Settlement settlement2 in Find.World.worldObjects.Settlements)
			{
				if (settlement2.HasMap && settlement2.Faction == Faction.OfPlayer && (settlement == null || settlement2.Map.mapPawns.ColonistCount > settlement.Map.mapPawns.ColonistCount))
				{
					settlement = settlement2;
				}
			}
			if (settlement == null)
			{
				if (!sentMessage)
				{
					Messages.Message("ShipNoSettlementToReturnTo".Translate(), transportShip.shipThing, MessageTypeDefOf.CautionInput);
					sentMessage = true;
				}
				return;
			}
			destination = settlement;
		}
		ShipJob_FlyAway shipJob_FlyAway = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(ShipJobDefOf.FlyAway);
		shipJob_FlyAway.destinationTile = destination.Tile;
		shipJob_FlyAway.arrivalAction = new TransportPodsArrivalAction_TransportShip(destination, transportShip);
		shipJob_FlyAway.dropMode = TransportShipDropMode.None;
		transportShip.SetNextJob(shipJob_FlyAway);
		transportShip.TryGetNextJob();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref destination, "destination");
		Scribe_Values.Look(ref sentMessage, "sentMessage", defaultValue: false);
	}
}
