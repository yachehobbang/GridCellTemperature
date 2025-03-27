using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class ITab_PenFood : ITab_PenBase
{
	private static readonly Vector2 WinSize = new Vector2(500f, 500f);

	private const int StatLineIndent = 8;

	private const int StatLabelColumnWidth = 210;

	private const float AboveTableMargin = 10f;

	private Vector2 animalPaneScrollPos;

	private readonly List<PenFoodCalculator.PenAnimalInfo> tmpAnimalInfos = new List<PenFoodCalculator.PenAnimalInfo>();

	public ITab_PenFood()
	{
		size = WinSize;
		labelKey = "TabPenFood";
	}

	public override void OnOpen()
	{
		base.OnOpen();
		animalPaneScrollPos = Vector2.zero;
	}

	protected override void FillTab()
	{
		CompAnimalPenMarker selectedCompAnimalPenMarker = base.SelectedCompAnimalPenMarker;
		Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
		if (selectedCompAnimalPenMarker.PenState.Unenclosed)
		{
			Widgets.NoneLabelCenteredVertically(rect, "(" + "PenFoodTab_NotEnclosed".Translate() + ")");
			return;
		}
		PenFoodCalculator penFoodCalculator = selectedCompAnimalPenMarker.PenFoodCalculator;
		Widgets.BeginGroup(rect);
		float curY = 0f;
		DrawTopPane(ref curY, rect.width, penFoodCalculator);
		float height = rect.height - curY;
		DrawAnimalPane(ref curY, rect.width, height, penFoodCalculator, selectedCompAnimalPenMarker.parent.Map);
		Widgets.EndGroup();
	}

	private void DrawTopPane(ref float curY, float width, PenFoodCalculator calc)
	{
		float num = calc.SumNutritionConsumptionPerDay - calc.NutritionPerDayToday;
		bool flag = num > 0f;
		DrawStatLine("PenSizeLabel".Translate(), calc.PenSizeDescription(), ref curY, width, null, null);
		DrawStatLine("PenFoodTab_NaturalNutritionGrowthRate".Translate(), PenFoodCalculator.NutritionPerDayToString(calc.NutritionPerDayToday), ref curY, width, calc.NaturalGrowthRateTooltip, flag ? new Color?(Color.red) : ((Color?)null));
		DrawStatLine("PenFoodTab_TotalNutritionConsumptionRate".Translate(), PenFoodCalculator.NutritionPerDayToString(calc.SumNutritionConsumptionPerDay), ref curY, width, calc.TotalConsumedToolTop, flag ? new Color?(Color.red) : ((Color?)null));
		if (!(calc.sumStockpiledNutritionAvailableNow > 0f))
		{
			return;
		}
		DrawStatLine("PenFoodTab_StockpileTotal".Translate(), PenFoodCalculator.NutritionToString(calc.sumStockpiledNutritionAvailableNow, withUnits: false), ref curY, width, calc.StockpileToolTip, null);
		if (flag)
		{
			int num2 = Mathf.FloorToInt(calc.sumStockpiledNutritionAvailableNow / num);
			DrawStatLine("PenFoodTab_StockpileEmptyDays".Translate(), num2.ToString(), ref curY, width, () => "PenFoodTab_StockpileEmptyDaysDescription".Translate(), Color.red);
		}
	}

	private void DrawStatLine(string label, string value, ref float curY, float width, Func<string> toolipGetter = null, Color? valueColor = null)
	{
		float lineHeight = Text.LineHeight;
		Rect rect = new Rect(8f, curY, width, lineHeight);
		rect.SplitVertically(210f, out var left, out var right);
		Widgets.Label(left, label);
		GUI.color = valueColor ?? Color.white;
		Widgets.Label(right, value);
		GUI.color = Color.white;
		if (Mouse.IsOver(rect) && toolipGetter != null)
		{
			Widgets.DrawHighlight(rect);
			TooltipHandler.TipRegion(rect, toolipGetter, Gen.HashCombineInt(10192384, label.GetHashCode()));
		}
		curY += lineHeight;
	}

	private void DrawAnimalPane(ref float curYOuter, float width, float height, PenFoodCalculator calc, Map map)
	{
		float cellWidth2 = width - 328f;
		float curY = curYOuter;
		float num = curY;
		float num2 = Mathf.Max(Text.LineHeight, 27f);
		float num3 = Text.LineHeightOf(GameFont.Small) + 10f;
		float num4 = num2;
		Rect rect = new Rect(0f, curY, width, height - (curY - num) - num4);
		rect.SplitHorizontally(num3, out var top, out var bottom);
		float x2 = top.x;
		curY = top.y;
		DrawIconCell(null, ref x2, 53f, num3);
		DrawCell("PenFoodTab_AnimalType".Translate(), ref x2, cellWidth2, num3, TextAnchor.LowerLeft, null, null);
		DrawCell("PenFoodTab_Count".Translate(), ref x2, 100f, num3, TextAnchor.LowerCenter, null, null);
		DrawCell("PenFoodTab_NutritionConsumedPerDay_ColumLabel".Translate(), ref x2, 120f, num3, TextAnchor.LowerCenter, null, () => "PenFoodTab_NutritionConsumedPerDay_ColumnTooltip".Translate());
		GUI.color = Widgets.SeparatorLineColor;
		Widgets.DrawLineHorizontal(0f, top.yMax - 1f, width);
		GUI.color = Color.white;
		tmpAnimalInfos.Clear();
		tmpAnimalInfos.AddRange(calc.ActualAnimalInfos);
		tmpAnimalInfos.AddRange(calc.ComputeExampleAnimals(base.SelectedCompAnimalPenMarker.ForceDisplayedAnimalDefs));
		Rect viewRect = new Rect(bottom.x, bottom.y, bottom.width - 16f, (float)tmpAnimalInfos.Count * num2);
		Widgets.BeginScrollView(bottom, ref animalPaneScrollPos, viewRect);
		curY = viewRect.y;
		int num5 = 0;
		foreach (PenFoodCalculator.PenAnimalInfo info2 in tmpAnimalInfos)
		{
			float x3 = viewRect.x;
			Rect rect2 = new Rect(x3, curY, viewRect.width, num2);
			if (num5 % 2 == 1)
			{
				Widgets.DrawLightHighlight(rect2);
			}
			DrawIconCell(info2.animalDef, ref x3, 53f, num2);
			DrawCell(info2.animalDef.LabelCap, ref x3, cellWidth2, num2, TextAnchor.MiddleLeft, null, null);
			if (!info2.example)
			{
				DrawCell(info2.TotalCount.ToString(), ref x3, 100f, num2, TextAnchor.MiddleCenter, null, null);
				DrawCell(PenFoodCalculator.NutritionPerDayToString(info2.TotalNutritionConsumptionPerDay, withUnits: false), ref x3, 120f, num2, TextAnchor.MiddleCenter, null, null);
			}
			else
			{
				float num6 = SimplifiedPastureNutritionSimulator.NutritionConsumedPerDay(info2.animalDef);
				int num7 = Mathf.FloorToInt(calc.NutritionPerDayToday / num6);
				DrawCell("max".Translate() + " " + num7.ToString(), ref x3, 100f, num2, TextAnchor.MiddleCenter, Color.grey, null);
				DrawCell("PenFoodTab_NutritionConsumedEachAnimalLabel".Translate(PenFoodCalculator.NutritionPerDayToString(num6, withUnits: false).Named("CONSUMEDAMOUNT")), ref x3, 120f, num2, TextAnchor.MiddleCenter, Color.grey, null);
				DrawExampleAnimalControls(info2, ref x3, 27f, num2);
			}
			if (Mouse.IsOver(rect2))
			{
				Widgets.DrawHighlight(rect2);
				TooltipHandler.TipRegion(rect2, () => info2.ToolTip(calc), 9477435);
			}
			curY += rect2.height;
			num5++;
		}
		Widgets.EndScrollView();
		Rect rect3 = new Rect(rect.x, Mathf.Min(rect.yMax, curY), rect.width, num4);
		Widgets.Dropdown(rect3.LeftPart(0.35f), calc, (PenFoodCalculator calculator) => (ThingDef)null, MenuGenerator, "PenFoodTab_AddAnimal".Translate());
		curY = rect3.yMax;
		curYOuter = curY;
		void DrawCell(string label, ref float x, float cellWidth, float cellHeight, TextAnchor anchor, Color? color, Func<string> tooltip)
		{
			if (label != null)
			{
				Rect rect5 = new Rect(x, curY, cellWidth, cellHeight);
				Text.Anchor = anchor;
				if (color.HasValue)
				{
					GUI.color = color.Value;
				}
				Widgets.Label(rect5, label);
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
				if (tooltip != null && Mouse.IsOver(rect5))
				{
					Widgets.DrawHighlight(rect5);
					TooltipHandler.TipRegion(rect5, tooltip, 7578334);
				}
			}
			x += cellWidth + 4f;
		}
		void DrawExampleAnimalControls(PenFoodCalculator.PenAnimalInfo info, ref float x, float cellWidth, float cellHeight)
		{
			if (Widgets.ButtonImage(new Rect(x, curY, cellWidth, cellHeight), TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
			{
				RemoveAnimal(calc, info);
			}
			x += cellWidth + 4f;
		}
		void DrawIconCell(ThingDef thingDef, ref float x, float cellWidth, float cellHeight)
		{
			if (thingDef != null)
			{
				Rect rect4 = new Rect(x, curY, 27f, 27f);
				Widgets.ThingIcon(rect4, thingDef, null, null, 1f, null, null);
				rect4.x += 29f;
				Widgets.InfoCardButton(rect4.x, rect4.y + 2f, thingDef);
			}
			x += cellWidth + 4f;
		}
		IEnumerable<Widgets.DropdownMenuElement<ThingDef>> MenuGenerator(PenFoodCalculator calculator)
		{
			foreach (ThingDef animal in map.plantGrowthRateCalculator.GrazingAnimals)
			{
				if (!base.SelectedCompAnimalPenMarker.ForceDisplayedAnimalDefs.Contains(animal))
				{
					Widgets.DropdownMenuElement<ThingDef> dropdownMenuElement = default(Widgets.DropdownMenuElement<ThingDef>);
					dropdownMenuElement.option = new FloatMenuOption(animal.LabelCap, delegate
					{
						AddExampleAnimal(calculator, animal);
					}, animal, null, forceBasicStyle: false, MenuOptionPriority.Default, null, null, 0f, null, null, playSelectionSound: true, 0, null);
					dropdownMenuElement.payload = animal;
					yield return dropdownMenuElement;
				}
			}
		}
	}

	private void RemoveAnimal(PenFoodCalculator calc, PenFoodCalculator.PenAnimalInfo info)
	{
		base.SelectedCompAnimalPenMarker.RemoveForceDisplayedAnimal(info.animalDef);
	}

	private void AddExampleAnimal(PenFoodCalculator calc, ThingDef animal)
	{
		base.SelectedCompAnimalPenMarker.AddForceDisplayedAnimal(animal);
	}
}
