using System.Linq;
using RimWorld;
using UnityEngine;

namespace Verse;

public static class SimplifiedPastureNutritionSimulator
{
	public const float UnderEstimateNutritionFactor = 0.85f;

	public static float NutritionProducedPerDay(BiomeDef biome, ThingDef plantDef, float averageGrowthRate, float mapRespawnChance)
	{
		if (Mathf.Approximately(averageGrowthRate, 0f))
		{
			return 0f;
		}
		float num = biome.wildPlantRegrowDays / mapRespawnChance;
		float num2 = plantDef.plant.growDays / averageGrowthRate * plantDef.plant.harvestMinGrowth;
		return plantDef.GetStatValueAbstract(StatDefOf.Nutrition) * PlantUtility.NutritionFactorFromGrowth(plantDef, plantDef.plant.harvestMinGrowth) / (num + num2) * biome.CommonalityPctOfPlant(plantDef) * 0.85f;
	}

	public static float NutritionConsumedPerDay(Pawn animal)
	{
		return NutritionConsumedPerDay(animal.def, animal.ageTracker.CurLifeStage);
	}

	public static float NutritionConsumedPerDay(ThingDef animalDef)
	{
		LifeStageAge lifeStageAge = animalDef.race.lifeStageAges.Last();
		return NutritionConsumedPerDay(animalDef, lifeStageAge.def);
	}

	public static float NutritionConsumedPerDay(ThingDef animalDef, LifeStageDef lifeStageDef)
	{
		return Need_Food.BaseHungerRate(lifeStageDef, animalDef) * 60000f;
	}
}
