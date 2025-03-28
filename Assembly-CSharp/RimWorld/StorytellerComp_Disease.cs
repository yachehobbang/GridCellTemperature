using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RimWorld;

public class StorytellerComp_Disease : StorytellerComp
{
	private float CaravanDiseaseMTBFactor = 4f;

	protected StorytellerCompProperties_Disease Props => (StorytellerCompProperties_Disease)props;

	public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
	{
		if (!DebugSettings.enableRandomDiseases || target is World || Find.Storyteller.difficulty.diseaseIntervalFactor >= 100f)
		{
			yield break;
		}
		BiomeDef biome = Find.WorldGrid[target.Tile]?.biome;
		if (target is Map { IsPocketMap: not false } map)
		{
			biome = map.Biome;
		}
		float diseaseMtbDays = biome.diseaseMtbDays;
		diseaseMtbDays *= Find.Storyteller.difficulty.diseaseIntervalFactor;
		if (target is Caravan)
		{
			diseaseMtbDays *= CaravanDiseaseMTBFactor;
		}
		if (Rand.MTBEventOccurs(diseaseMtbDays, 60000f, 1000f))
		{
			IncidentParms parms = GenerateParms(Props.category, target);
			if (UsableIncidentsInCategory(Props.category, parms).TryRandomElementByWeight((IncidentDef d) => biome.CommonalityOfDisease(d), out var result))
			{
				yield return new FiringIncident(result, this, parms);
			}
		}
	}

	public override string ToString()
	{
		return base.ToString() + " " + Props.category;
	}
}
