using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public class IncidentWorker_ShamblerSwarmAnimals : IncidentWorker_ShamblerSwarm
{
	private const float ChimeraSpawnChance = 0.02f;

	private PawnKindDef animalKind;

	protected override List<Pawn> GenerateEntities(IncidentParms parms, float points)
	{
		Map map = (Map)parms.target;
		bool polluted = Rand.Value < WildAnimalSpawner.PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(Find.WorldGrid[map.Tile].pollution);
		if (!map.Biome.AllWildAnimals.Where((PawnKindDef a) => (!(map.mapTemperature.SeasonAcceptableFor(a.race) && polluted)) ? (map.Biome.CommonalityOfAnimal(a) > 0f) : (map.Biome.CommonalityOfPollutionAnimal(a) > 0f)).TryRandomElement(out animalKind))
		{
			return null;
		}
		List<Pawn> list = new List<Pawn>();
		int num = Mathf.RoundToInt(points / animalKind.combatPower);
		for (int i = 0; i < num; i++)
		{
			PawnKindDef chimera = animalKind;
			if (Rand.Chance(0.02f))
			{
				chimera = PawnKindDefOf.Chimera;
			}
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(chimera, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
			MutantUtility.SetFreshPawnAsMutant(pawn, MutantDefOf.Shambler);
			list.Add(pawn);
		}
		SetupShamblerHediffs(list, ShamblerLifespanTicksRange);
		return list;
	}

	protected override void SendLetter(IncidentParms parms, List<Pawn> entities)
	{
		TaggedString baseLetterText = "LetterShamblerAnimalsArrived".Translate(NamedArgumentUtility.Named(animalKind, "ANIMALKIND"));
		int num = entities.Count((Pawn e) => e.kindDef == PawnKindDefOf.Chimera);
		if (num == 1)
		{
			baseLetterText += "\n\n" + "LetterText_ShamblerChimera".Translate();
		}
		else if (num > 1)
		{
			baseLetterText += "\n\n" + "LetterText_ShamblerChimeraPlural".Translate();
		}
		SendStandardLetter("LetterLabelShamblerAnimalsArrived".Translate(NamedArgumentUtility.Named(animalKind, "ANIMALKIND")), baseLetterText, def.letterDef, parms, entities);
	}
}
