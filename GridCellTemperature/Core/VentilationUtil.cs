using RimWorld;

namespace GridCellTemperature.Core
{
	public static class VentilationUtil
	{
		public static void UpdateDoor(Building_Door door)
		{
			if (door.Spawned && door.Open)
			{
				if (door.Open)
				{
					TemperatureGrid.SetVentilationCell(door.Position, 0.25f);
				}
				else
				{
					TemperatureGrid.SetVentilationCell(door.Position, 1f);
				}
			}
			else
			{
				TemperatureGrid.SetVentilationCell(door.Position, null);
			}
		}

		public static void UpdateVent(Building_Vent vent)
		{
			if (vent.Spawned && FlickUtility.WantsToBeOn(vent))
			{
				TemperatureGrid.SetVentilationCell(vent.Position, 1f);
			}
			else
			{
				TemperatureGrid.SetVentilationCell(vent.Position, null);
			}
		}
	}
}
