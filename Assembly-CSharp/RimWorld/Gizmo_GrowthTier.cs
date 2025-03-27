using UnityEngine;
using Verse;

namespace RimWorld;

[StaticConstructorOnStartup]
public class Gizmo_GrowthTier : Gizmo
{
	private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(GenUI.FillableBar_Empty);

	private const float Spacing = 8f;

	private const float LabelWidthPercent = 0.55f;

	private const float BarMarginY = 2f;

	private const int GrowthTierTooltipId = 837825001;

	private Pawn child;

	private Texture2D barTex;

	private float Width => 190f;

	private int GrowthTier => child.ageTracker.GrowthTier;

	public override bool Visible
	{
		get
		{
			if (!child.IsColonistPlayerControlled && !child.IsPrisonerOfColony)
			{
				return child.IsSlaveOfColony;
			}
			return true;
		}
	}

	public override float GetWidth(float maxWidth)
	{
		return Width;
	}

	public Gizmo_GrowthTier(Pawn child)
	{
		this.child = child;
		barTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1254902f, 46f / 85f, 0f));
		Order = -100f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(8f);
		Widgets.DrawWindowBackground(rect);
		Rect rect3 = new Rect(rect2.x, rect2.y, rect2.width, rect2.height / 2f);
		Rect rect4 = new Rect(rect2.x, rect3.yMax, rect2.width, rect3.height);
		rect3.yMax -= 2f;
		rect4.yMin += 2f;
		DrawGrowthTier(rect3);
		DrawLearning(rect4);
		return new GizmoResult(GizmoState.Clear);
	}

	private string GrowthTierTooltip(Rect rect, int tier)
	{
		TaggedString taggedString = ("StatsReport_GrowthTier".Translate() + ": ").AsTipTitle() + tier + "\n" + "StatsReport_GrowthTierDesc".Translate().Colorize(ColoredText.SubtleGrayColor) + "\n\n";
		if (child.ageTracker.AtMaxGrowthTier)
		{
			taggedString += ("MaxTier".Translate() + ": ").AsTipTitle() + "MaxTierDesc".Translate(child.Named("PAWN"));
		}
		else
		{
			taggedString += ("ProgressToNextGrowthTier".Translate() + ": ").AsTipTitle() + Mathf.FloorToInt(child.ageTracker.growthPoints).ToString() + " / " + GrowthUtility.GrowthTierPointsRequirements[tier + 1];
			if (child.ageTracker.canGainGrowthPoints)
			{
				taggedString += string.Format(" (+{0})", "PerDay".Translate(child.ageTracker.GrowthPointsPerDay.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
			}
		}
		if (child.ageTracker.AgeBiologicalYears < 13)
		{
			int num = 0;
			for (int i = child.ageTracker.AgeBiologicalYears + 1; i <= 13; i++)
			{
				if (GrowthUtility.IsGrowthBirthday(i))
				{
					num = i;
					break;
				}
			}
			taggedString += "\n\n" + ("NextGrowthMomentAt".Translate() + ": ").AsTipTitle() + num;
		}
		taggedString += "\n\n" + ("ThisGrowthTier".Translate(tier) + ":").AsTipTitle();
		if (GrowthUtility.PassionGainsPerTier[tier] > 0)
		{
			taggedString += "\n  - " + "NumPassionsFromOptions".Translate(GrowthUtility.PassionGainsPerTier[tier], GrowthUtility.PassionChoicesPerTier[tier]);
		}
		taggedString += "\n  - " + "NumTraitsFromOptions".Translate(GrowthUtility.TraitGainsPerTier[tier], GrowthUtility.TraitChoicesPerTier[tier]);
		if (!child.ageTracker.AtMaxGrowthTier)
		{
			taggedString += "\n\n" + ("NextGrowthTier".Translate(tier + 1) + ":").AsTipTitle();
			if (GrowthUtility.PassionGainsPerTier[tier + 1] > 0)
			{
				taggedString += "\n  - " + "NumPassionsFromOptions".Translate(GrowthUtility.PassionGainsPerTier[tier + 1], GrowthUtility.PassionChoicesPerTier[tier + 1]);
			}
			taggedString += "\n  - " + "NumTraitsFromOptions".Translate(GrowthUtility.TraitGainsPerTier[tier + 1], GrowthUtility.TraitChoicesPerTier[tier + 1]);
		}
		return taggedString.Resolve();
	}

	private void DrawGrowthTier(Rect rect)
	{
		int growthTier = GrowthTier;
		Rect rect2 = rect;
		rect2.xMax = rect.x + rect.width * 0.55f;
		string label = (string)("StatsReport_GrowthTier".Translate() + ": ") + growthTier;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect2, label);
		Text.Anchor = TextAnchor.UpperLeft;
		float percentToNextGrowthTier = child.ageTracker.PercentToNextGrowthTier;
		Rect rect3 = rect;
		rect3.xMin = rect2.xMax;
		rect3.yMin += 2f;
		rect3.yMax -= 2f;
		Widgets.FillableBar(rect3, percentToNextGrowthTier, barTex, EmptyBarTex, doBorder: true);
		Text.Anchor = TextAnchor.MiddleCenter;
		float num = GrowthUtility.GrowthTierPointsRequirements[GrowthUtility.GrowthTierPointsRequirements.Length - 1];
		string label2 = (child.ageTracker.AtMaxGrowthTier ? (num + " / " + num) : (Mathf.FloorToInt(child.ageTracker.growthPoints).ToString() + " / " + GrowthUtility.GrowthTierPointsRequirements[growthTier + 1]));
		Widgets.Label(rect3, label2);
		Text.Anchor = TextAnchor.UpperLeft;
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
			string text = GrowthTierTooltip(rect, growthTier);
			TooltipHandler.TipRegion(rect, new TipSignal(text, child.thingIDNumber ^ 0x31F031E9));
		}
	}

	private void DrawLearning(Rect rect)
	{
		if (child.needs.learning != null)
		{
			Rect rect2 = rect;
			rect2.xMax = rect.x + rect.width * 0.55f;
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rect2, NeedDefOf.Learning.LabelCap);
			Text.Anchor = TextAnchor.UpperLeft;
			Rect rect3 = rect;
			rect3.xMin = rect2.xMax;
			rect3.yMin += 2f;
			rect3.yMax -= 2f;
			Widgets.FillableBar(rect3, child.needs.learning.CurLevelPercentage, Widgets.BarFullTexHor, EmptyBarTex, doBorder: true);
			Text.Anchor = TextAnchor.MiddleCenter;
			string label = child.needs.learning.CurLevelPercentage.ToStringPercent();
			Widgets.Label(rect3, label);
			Text.Anchor = TextAnchor.UpperLeft;
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawHighlight(rect);
				TooltipHandler.TipRegion(rect, child.needs.learning.GetTipString());
			}
		}
	}
}
