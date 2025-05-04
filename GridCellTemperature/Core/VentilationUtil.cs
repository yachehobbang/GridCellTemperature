using RimWorld;

namespace GridCellTemperature.Core
{
	public static class VentilationUtil
	{
		public static void UpdateDoor(Building_Door door)
		{
			if (door == null)
			{
				return;
			}
			var grid = TemperatureGrid.GetTemperatureGrid(door.Map);
			if (grid == null)
			{
				return;
			}

			if (door.Spawned && door.Open)
			{
				if (door.Open)
				{
					grid.SetVentilationCell(door.Position, 1f);
				}
				else
				{
					grid.SetVentilationCell(door.Position, 0.25f);
				}
			}
			else
			{
				grid.SetVentilationCell(door.Position, null);
			}
		}

		public static void UpdateVent(Building_Vent vent)
		{
			var grid = TemperatureGrid.GetTemperatureGrid(vent.Map);
			if (grid == null)
			{
				return;
			}

			if (vent.Spawned && FlickUtility.WantsToBeOn(vent))
			{
				grid.SetVentilationCell(vent.Position, 1f);
			}
			else
			{
				grid.SetVentilationCell(vent.Position, null);
			}
		}
	}
}
