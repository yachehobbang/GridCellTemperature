using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class AggressiveAnimalIncidentUtility
{
	public const int MinAnimalCount = 2;

	public const int MaxAnimalCount = 100;

	public const float MinPoints = 70f;

	public static float AnimalWeight(PawnKindDef animal, float points)
	{
		points = Mathf.Max(points, 70f);
		if (animal.combatPower * 2f > points)
		{
			return 0f;
		}
		int num = Mathf.Min(Mathf.RoundToInt(points / animal.combatPower), 100);
		return Mathf.Clamp01(Mathf.InverseLerp(100f, 10f, num));
	}

	public static bool TryFindAggressiveAnimalKind(float points, int tile, out PawnKindDef animalKind)
	{
		List<PawnKindDef> animals = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef k) => CanArriveManhunter(k) && (tile == -1 || Find.World.tileTemperatures.SeasonAndOutdoorTemperatureAcceptableFor(tile, k.race))).ToList();
		return TryGetAnimalFromList(points, animals, out animalKind);
	}

	public static bool TryFindAggressiveAnimalKind(float points, Map map, out PawnKindDef animalKind)
	{
		bool polluted = ModsConfig.BiotechActive && map.Tile != -1 && Rand.Value < WildAnimalSpawner.PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(Find.WorldGrid[map.Tile].pollution);
		List<PawnKindDef> animals = map.Biome.AllWildAnimals.Where((PawnKindDef k) => CanArriveWithPollution(k, map, polluted) && CanArriveManhunter(k)).ToList();
		if (TryGetAnimalFromList(points, animals, out animalKind))
		{
			return true;
		}
		animals = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef k) => CanArriveManhunter(k) && CanArriveWithPollution(k, map, polluted) && (map.Tile == -1 || Find.World.tileTemperatures.SeasonAndOutdoorTemperatureAcceptableFor(map.Tile, k.race))).ToList();
		return TryGetAnimalFromList(points, animals, out animalKind);
	}

	private static bool TryGetAnimalFromList(float points, List<PawnKindDef> animals, out PawnKindDef animalKind)
	{
		if (animals.Any())
		{
			if (animals.TryRandomElementByWeight((PawnKindDef a) => AnimalWeight(a, points), out animalKind))
			{
				return true;
			}
			if (points > animals.Min((PawnKindDef a) => a.combatPower) * 2f)
			{
				animalKind = animals.MaxBy((PawnKindDef a) => a.combatPower);
				return true;
			}
		}
		animalKind = null;
		return false;
	}

	public static int GetAnimalsCount(PawnKindDef animalKind, float points)
	{
		return Mathf.Clamp(Mathf.RoundToInt(points / animalKind.combatPower), 2, 100);
	}

	public static List<Pawn> GenerateAnimals(PawnKindDef animalKind, int tile, float points, int animalCount = 0)
	{
		List<Pawn> list = new List<Pawn>();
		int num = ((animalCount > 0) ? animalCount : GetAnimalsCount(animalKind, points));
		for (int i = 0; i < num; i++)
		{
			Pawn item = PawnGenerator.GeneratePawn(new PawnGenerationRequest(animalKind, null, PawnGenerationContext.NonPlayer, tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
			list.Add(item);
		}
		return list;
	}

	public static List<Pawn> GenerateAnimals(List<PawnKindDef> animalKinds, int tile)
	{
		List<Pawn> list = new List<Pawn>();
		foreach (PawnKindDef animalKind in animalKinds)
		{
			Pawn item = PawnGenerator.GeneratePawn(new PawnGenerationRequest(animalKind, null, PawnGenerationContext.NonPlayer, tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
			list.Add(item);
		}
		return list;
	}

	[DebugOutput]
	public static void ManhunterResults()
	{
		List<PawnKindDef> candidates = (from k in DefDatabase<PawnKindDef>.AllDefs.Where(CanArriveManhunter)
			orderby 0f - k.combatPower
			select k).ToList();
		List<float> list = new List<float>();
		for (int i = 0; i < 30; i++)
		{
			list.Add(20f * Mathf.Pow(1.25f, i));
		}
		DebugTables.MakeTablesDialog(list, (float points) => points.ToString("F0") + " pts", candidates, (PawnKindDef candidate) => candidate.defName + " (" + candidate.combatPower.ToString("F0") + ")", delegate(float points, PawnKindDef candidate)
		{
			float num = candidates.Sum((PawnKindDef k) => AnimalWeight(k, points));
			float num2 = AnimalWeight(candidate, points);
			return (num2 != 0f) ? ((num2 * 100f / num).ToString("F0") + $"%, {Mathf.Max(Mathf.RoundToInt(points / candidate.combatPower), 1)}") : "0%";
		});
	}

	private static bool CanArriveManhunter(PawnKindDef kind)
	{
		if (kind.RaceProps.Animal && kind.canArriveManhunter)
		{
			return kind.RaceProps.CanPassFences;
		}
		return false;
	}

	private static bool CanArriveWithPollution(PawnKindDef k, Map map, bool polluted)
	{
		if (!polluted)
		{
			return map.Biome.CommonalityOfAnimal(k) > 0f;
		}
		return map.Biome.CommonalityOfPollutionAnimal(k) > 0f;
	}
}
