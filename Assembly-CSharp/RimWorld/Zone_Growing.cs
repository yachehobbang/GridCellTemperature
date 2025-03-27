using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld;

public class Zone_Growing : Zone, IPlantToGrowSettable
{
	private ThingDef plantDefToGrow;

	public bool allowSow = true;

	public bool allowCut = true;

	public override bool IsMultiselectable => true;

	protected override Color NextZoneColor => ZoneColorUtility.NextGrowingZoneColor();

	IEnumerable<IntVec3> IPlantToGrowSettable.Cells => base.Cells;

	public ThingDef PlantDefToGrow
	{
		get
		{
			if (plantDefToGrow == null)
			{
				if (PollutionUtility.SettableEntirelyPolluted(this))
				{
					plantDefToGrow = ThingDefOf.Plant_Toxipotato;
				}
				else
				{
					plantDefToGrow = ThingDefOf.Plant_Potato;
				}
			}
			return plantDefToGrow;
		}
	}

	public Zone_Growing()
	{
	}

	public Zone_Growing(ZoneManager zoneManager)
		: base("GrowingZone".Translate(), zoneManager)
	{
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.Look(ref plantDefToGrow, "plantDefToGrow");
		Scribe_Values.Look(ref allowSow, "allowSow", defaultValue: true);
		Scribe_Values.Look(ref allowCut, "allowCut", defaultValue: true);
	}

	public void ContentsStatistics(out int totalPlants, out float averagePlantAgeTicks, out int oldestPlantAgeTicks, out float averagePlantGrowth, out float maxPlantGrowth)
	{
		averagePlantAgeTicks = 0f;
		totalPlants = 0;
		oldestPlantAgeTicks = 0;
		averagePlantGrowth = 0f;
		maxPlantGrowth = 0f;
		foreach (IntVec3 cell in base.Cells)
		{
			foreach (Thing thing in cell.GetThingList(base.Map))
			{
				if (thing.def == plantDefToGrow && thing is Plant plant)
				{
					totalPlants++;
					averagePlantAgeTicks += plant.Age;
					oldestPlantAgeTicks = Mathf.Max(oldestPlantAgeTicks, plant.Age);
					averagePlantGrowth += plant.Growth;
					maxPlantGrowth = Mathf.Max(maxPlantGrowth, plant.Growth);
				}
			}
		}
		averagePlantGrowth /= totalPlants;
		averagePlantAgeTicks /= totalPlants;
	}

	public override string GetInspectString()
	{
		StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());
		stringBuilder.AppendLine();
		if (!base.Cells.NullOrEmpty())
		{
			ContentsStatistics(out var totalPlants, out var averagePlantAgeTicks, out var oldestPlantAgeTicks, out var averagePlantGrowth, out var maxPlantGrowth);
			if (totalPlants > 0)
			{
				string arg = (averagePlantAgeTicks / 3600000f).ToStringApproxAge();
				string arg2 = ((float)oldestPlantAgeTicks / 3600000f).ToStringApproxAge();
				stringBuilder.AppendLine(string.Format("{0}: {1} {2}", "Contains".Translate().CapitalizeFirst(), totalPlants, Find.ActiveLanguageWorker.Pluralize(plantDefToGrow.label, totalPlants)));
				stringBuilder.AppendLine(string.Format("{0}: {1} ({2})", "AveragePlantAge".Translate().CapitalizeFirst(), arg, "PercentGrowth".Translate(averagePlantGrowth.ToStringPercent())));
				stringBuilder.AppendLine(string.Format("{0}: {1} ({2})", "OldestPlantAge".Translate().CapitalizeFirst(), arg2, "PercentGrowth".Translate(maxPlantGrowth.ToStringPercent())));
			}
			IntVec3 c = base.Cells.First();
			if (c.UsesOutdoorTemperature(base.Map))
			{
				stringBuilder.AppendLine("OutdoorGrowingPeriod".Translate() + ": " + GrowingQuadrumsDescription(base.Map.Tile));
			}
			if (PlantUtility.GrowthSeasonNow(c, base.Map, forSowing: true))
			{
				stringBuilder.Append("GrowSeasonHereNow".Translate());
			}
			else
			{
				stringBuilder.Append("CannotGrowBadSeasonTemperature".Translate());
			}
		}
		return stringBuilder.ToString();
	}

	public static string GrowingQuadrumsDescription(int tile)
	{
		List<Twelfth> list = GenTemperature.TwelfthsInAverageTemperatureRange(tile, 6f, 42f);
		if (list.NullOrEmpty())
		{
			return "NoGrowingPeriod".Translate();
		}
		if (list.Count == 12)
		{
			return "GrowYearRound".Translate();
		}
		return "PeriodDays".Translate(list.Count * 5 + "/" + 60) + " (" + QuadrumUtility.QuadrumsRangeLabel(list) + ")";
	}

	public override void AddCell(IntVec3 c)
	{
		base.AddCell(c);
		foreach (Thing item in base.Map.thingGrid.ThingsListAt(c))
		{
			Designator_PlantsHarvestWood.PossiblyWarnPlayerImportantPlantDesignateCut(item);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		yield return PlantToGrowSettableUtility.SetPlantToGrowCommand(this);
		Command_Toggle command_Toggle = new Command_Toggle();
		command_Toggle.defaultLabel = "CommandAllowSow".Translate();
		command_Toggle.defaultDesc = "CommandAllowSowDesc".Translate();
		command_Toggle.hotKey = KeyBindingDefOf.Command_ItemForbid;
		command_Toggle.icon = TexCommand.ForbidOff;
		command_Toggle.isActive = () => allowSow;
		command_Toggle.toggleAction = delegate
		{
			allowSow = !allowSow;
		};
		yield return command_Toggle;
		Command_Toggle command_Toggle2 = new Command_Toggle();
		command_Toggle2.defaultLabel = "CommandAllowCut".Translate();
		command_Toggle2.defaultDesc = "CommandAllowCutDesc".Translate();
		command_Toggle2.icon = Designator_PlantsCut.IconTex;
		command_Toggle2.isActive = () => allowCut;
		command_Toggle2.toggleAction = delegate
		{
			allowCut = !allowCut;
		};
		yield return command_Toggle2;
	}

	public override IEnumerable<Gizmo> GetZoneAddGizmos()
	{
		yield return DesignatorUtility.FindAllowedDesignator<Designator_ZoneAdd_Growing_Expand>();
	}

	public ThingDef GetPlantDefToGrow()
	{
		return PlantDefToGrow;
	}

	public void SetPlantDefToGrow(ThingDef plantDef)
	{
		plantDefToGrow = plantDef;
	}

	public bool CanAcceptSowNow()
	{
		return true;
	}
}
