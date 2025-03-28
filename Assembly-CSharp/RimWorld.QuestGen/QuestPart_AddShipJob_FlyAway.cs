using RimWorld.Planet;
using Verse;

namespace RimWorld.QuestGen;

public class QuestPart_AddShipJob_FlyAway : QuestPart_AddShipJob
{
	public int destinationTile = -1;

	public TransportPodsArrivalAction arrivalAction;

	public TransportShipDropMode dropMode = TransportShipDropMode.All;

	public override ShipJob GetShipJob()
	{
		ShipJob_FlyAway obj = (ShipJob_FlyAway)ShipJobMaker.MakeShipJob(shipJobDef);
		obj.destinationTile = destinationTile;
		obj.arrivalAction = arrivalAction;
		obj.dropMode = dropMode;
		return obj;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref destinationTile, "destinationTile", 0);
		Scribe_Deep.Look(ref arrivalAction, "arrivalAction");
		Scribe_Values.Look(ref dropMode, "dropMode", TransportShipDropMode.None);
	}
}
