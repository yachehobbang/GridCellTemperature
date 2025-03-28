using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld.Planet;

public class WITab_Terrain : WITab
{
	private Vector2 scrollPosition;

	private float lastDrawnHeight;

	private static readonly Vector2 WinSize = new Vector2(440f, 540f);

	public override bool IsVisible => base.SelTileID >= 0;

	public WITab_Terrain()
	{
		size = WinSize;
		labelKey = "TabTerrain";
		tutorTag = "Terrain";
	}

	protected override void FillTab()
	{
		Rect outRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
		Rect rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(lastDrawnHeight, outRect.height));
		Widgets.BeginScrollView(outRect, ref scrollPosition, rect);
		Text.Font = GameFont.Medium;
		Widgets.Label(rect, base.SelTile.biome.LabelCap);
		Rect rect2 = rect;
		rect2.yMin += 35f;
		rect2.height = 99999f;
		Text.Font = GameFont.Small;
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.verticalSpacing = 0f;
		listing_Standard.Begin(rect2);
		Tile selTile = base.SelTile;
		int selTileID = base.SelTileID;
		listing_Standard.Label(selTile.biome.description);
		listing_Standard.Gap(8f);
		listing_Standard.GapLine();
		if (!selTile.biome.implemented)
		{
			listing_Standard.Label(selTile.biome.LabelCap + " " + "BiomeNotImplemented".Translate());
		}
		listing_Standard.LabelDouble("Terrain".Translate(), selTile.hilliness.GetLabelCap());
		if (selTile.Roads != null)
		{
			listing_Standard.LabelDouble("Road".Translate(), selTile.Roads.Select((Tile.RoadLink roadlink) => roadlink.road.label).Distinct().ToCommaList(useAnd: true)
				.CapitalizeFirst());
		}
		if (selTile.Rivers != null)
		{
			listing_Standard.LabelDouble("River".Translate(), selTile.Rivers.MaxBy((Tile.RiverLink riverlink) => riverlink.river.degradeThreshold).river.LabelCap);
		}
		if (!Find.World.Impassable(selTileID))
		{
			StringBuilder stringBuilder = new StringBuilder();
			string rightLabel = (WorldPathGrid.CalculatedMovementDifficultyAt(selTileID, perceivedStatic: false, null, stringBuilder) * Find.WorldGrid.GetRoadMovementDifficultyMultiplier(selTileID, -1, stringBuilder)).ToString("0.#");
			if (WorldPathGrid.WillWinterEverAffectMovementDifficulty(selTileID) && WorldPathGrid.GetCurrentWinterMovementDifficultyOffset(selTileID, null) < 2f)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();
				stringBuilder.Append(" (");
				stringBuilder.Append("MovementDifficultyOffsetInWinter".Translate("+" + 2f.ToString("0.#")));
				stringBuilder.Append(")");
			}
			listing_Standard.LabelDouble("MovementDifficulty".Translate(), rightLabel, stringBuilder.ToString());
		}
		if (selTile.biome.canBuildBase)
		{
			listing_Standard.LabelDouble("StoneTypesHere".Translate(), (from rt in Find.World.NaturalRockTypesIn(selTileID)
				select rt.label).ToCommaList(useAnd: true).CapitalizeFirst());
		}
		listing_Standard.LabelDouble("Elevation".Translate(), selTile.elevation.ToString("F0") + "m");
		listing_Standard.GapLine();
		listing_Standard.LabelDouble("AvgTemp".Translate(), GenTemperature.GetAverageTemperatureLabel(selTileID));
		listing_Standard.LabelDouble("OutdoorGrowingPeriod".Translate(), Zone_Growing.GrowingQuadrumsDescription(selTileID));
		listing_Standard.LabelDouble("Rainfall".Translate(), selTile.rainfall.ToString("F0") + "mm");
		if (selTile.biome.foragedFood != null && selTile.biome.forageability > 0f)
		{
			listing_Standard.LabelDouble("Forageability".Translate(), selTile.biome.forageability.ToStringPercent() + " (" + selTile.biome.foragedFood.label + ")");
		}
		else
		{
			listing_Standard.LabelDouble("Forageability".Translate(), "0%");
		}
		listing_Standard.LabelDouble("AnimalsCanGrazeNow".Translate(), VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(selTileID) ? "Yes".Translate() : "No".Translate());
		if (ModsConfig.BiotechActive)
		{
			listing_Standard.GapLine();
			listing_Standard.LabelDouble("TilePollution".Translate(), Find.WorldGrid[selTileID].pollution.ToStringPercent(), "TerrainPollutionTip".Translate());
			string text = "";
			foreach (IGrouping<float, CurvePoint> item in from p in WorldPollutionUtility.NearbyPollutionOverDistanceCurve
				group p by p.y)
			{
				if (!text.NullOrEmpty())
				{
					text += "\n";
				}
				if (item.Count() > 1)
				{
					CurvePoint curvePoint = item.MinBy((CurvePoint p) => p.x);
					CurvePoint curvePoint2 = item.MaxBy((CurvePoint p) => p.x);
					text += (string)(" - " + curvePoint.x + "-" + curvePoint2.x + " " + "NearbyPollutionTilesAway".Translate() + ", ") + item.Key + "x " + "PollutionLower".Translate();
				}
				else
				{
					text += (string)(" - " + item.First().x + " " + "NearbyPollutionTilesAway".Translate() + ", ") + item.Key + "x " + "PollutionLower".Translate();
				}
			}
			TaggedString taggedString = "NearbyPollutionTip".Translate(4, text);
			float num = WorldPollutionUtility.CalculateNearbyPollutionScore(selTileID);
			if (num >= GameConditionDefOf.NoxiousHaze.minNearbyPollution)
			{
				float num2 = GameConditionDefOf.NoxiousHaze.mtbOverNearbyPollutionCurve.Evaluate(num);
				taggedString += "\n\n" + "NoxiousHazeInterval".Translate(num2);
			}
			else
			{
				taggedString += "\n\n" + "NoxiousHazeNeverOccurring".Translate();
			}
			listing_Standard.LabelDouble("TilePollutionNearby".Translate(), WorldPollutionUtility.CalculateNearbyPollutionScore(selTileID).ToStringByStyle(ToStringStyle.FloatTwo), taggedString);
		}
		listing_Standard.GapLine();
		listing_Standard.LabelDouble("AverageDiseaseFrequency".Translate(), string.Format("{0} {1}", (60f / selTile.biome.diseaseMtbDays).ToString("F1"), "PerYear".Translate()));
		listing_Standard.LabelDouble("TimeZone".Translate(), GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(selTileID).x).ToStringWithSign());
		StringBuilder stringBuilder2 = new StringBuilder();
		Rot4 rot = Find.World.CoastDirectionAt(selTileID);
		if (rot.IsValid)
		{
			stringBuilder2.AppendWithComma(("HasCoast" + rot.ToString()).Translate());
		}
		if (Find.World.HasCaves(selTileID))
		{
			stringBuilder2.AppendWithComma("HasCaves".Translate());
		}
		if (stringBuilder2.Length > 0)
		{
			listing_Standard.LabelDouble("SpecialFeatures".Translate(), stringBuilder2.ToString().CapitalizeFirst());
		}
		if (Prefs.DevMode)
		{
			listing_Standard.LabelDouble("Debug world tile ID", selTileID.ToString());
		}
		lastDrawnHeight = rect2.y + listing_Standard.CurHeight;
		listing_Standard.End();
		Widgets.EndScrollView();
	}
}
