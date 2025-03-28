using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld;

public class StatWorker_MarketValue : StatWorker
{
	public const float ValuePerWork = 0.0036f;

	private const float DefaultGuessStuffCost = 2f;

	public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
	{
		if (req.HasThing && req.Thing is Pawn)
		{
			return base.GetValueUnfinalized(StatRequest.For(req.BuildableDef, req.StuffDef), applyPostProcess) * PriceUtility.PawnQualityPriceFactor((Pawn)req.Thing) + PriceUtility.PawnQualityPriceOffset((Pawn)req.Thing);
		}
		if (req.StatBases.StatListContains(StatDefOf.MarketValue))
		{
			if (stat != StatDefOf.MarketValue)
			{
				return StatDefOf.MarketValue.Worker.GetValueUnfinalized(req);
			}
			return base.GetValueUnfinalized(req);
		}
		if (!req.HasThing || !(req.Thing is IFixedBaseMarketValue { BaseMarketValue: var baseMarketValue }))
		{
			if (req.HasThing && req.Thing.StyleSourcePrecept is Precept_Relic)
			{
				return CalculatedBaseMarketValue(req.BuildableDef, ThingDefOf.Steel);
			}
			return CalculatedBaseMarketValue(req.BuildableDef, req.StuffDef);
		}
		return baseMarketValue;
	}

	public static float CalculatedBaseMarketValue(BuildableDef def, ThingDef stuffDef)
	{
		float num = 0f;
		RecipeDef recipeDef = null;
		recipeDef = CalculableRecipe(def);
		float num2;
		int num3;
		if (recipeDef != null)
		{
			num2 = recipeDef.workAmount;
			num3 = recipeDef.products[0].count;
			if (recipeDef.ingredients != null)
			{
				for (int i = 0; i < recipeDef.ingredients.Count; i++)
				{
					IngredientCount ingredientCount = recipeDef.ingredients[i];
					int num4 = ingredientCount.CountRequiredOfFor(ingredientCount.FixedIngredient, recipeDef);
					num += (float)num4 * ingredientCount.FixedIngredient.BaseMarketValue;
				}
			}
		}
		else
		{
			num2 = Mathf.Max(def.GetStatValueAbstract(StatDefOf.WorkToMake, stuffDef), def.GetStatValueAbstract(StatDefOf.WorkToBuild, stuffDef));
			num3 = 1;
			if (def.CostList != null)
			{
				for (int j = 0; j < def.CostList.Count; j++)
				{
					ThingDefCountClass thingDefCountClass = def.CostList[j];
					num += (float)thingDefCountClass.count * thingDefCountClass.thingDef.BaseMarketValue;
				}
			}
			if (def.CostStuffCount > 0)
			{
				num = ((stuffDef == null) ? (num + (float)def.CostStuffCount * 2f) : (num + (float)def.CostStuffCount / stuffDef.VolumePerUnit * stuffDef.GetStatValueAbstract(StatDefOf.MarketValue)));
			}
		}
		if (num2 > 2f)
		{
			num += num2 * 0.0036f;
		}
		return num / (float)num3;
	}

	public static RecipeDef CalculableRecipe(BuildableDef def)
	{
		if (def.CostList.NullOrEmpty() && def.CostStuffCount <= 0)
		{
			List<RecipeDef> allDefsListForReading = DefDatabase<RecipeDef>.AllDefsListForReading;
			for (int i = 0; i < allDefsListForReading.Count; i++)
			{
				RecipeDef recipeDef = allDefsListForReading[i];
				if (recipeDef.products == null || recipeDef.products.Count != 1 || recipeDef.products[0].thingDef != def)
				{
					continue;
				}
				for (int j = 0; j < recipeDef.ingredients.Count; j++)
				{
					if (!recipeDef.ingredients[j].IsFixedIngredient)
					{
						return null;
					}
				}
				return recipeDef;
			}
		}
		return null;
	}

	public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
	{
		if (req.HasThing && req.Thing is Pawn)
		{
			Pawn pawn = (Pawn)req.Thing;
			StringBuilder stringBuilder = new StringBuilder(base.GetExplanationUnfinalized(req, numberSense));
			PriceUtility.PawnQualityPriceFactor(pawn, stringBuilder);
			PriceUtility.PawnQualityPriceOffset(pawn, stringBuilder);
			return stringBuilder.ToString();
		}
		if (req.StatBases.StatListContains(StatDefOf.MarketValue))
		{
			return base.GetExplanationUnfinalized(req, numberSense);
		}
		_ = req.StuffDef;
		return "StatsReport_MarketValueFromStuffsAndWork".TranslateSimple().TrimEnd('.') + ": " + CalculatedBaseMarketValue(req.BuildableDef, (!req.HasThing || !(req.Thing.StyleSourcePrecept is Precept_Relic)) ? req.StuffDef : ThingDefOf.Steel).ToStringByStyle(stat.ToStringStyleUnfinalized, numberSense);
	}
}
