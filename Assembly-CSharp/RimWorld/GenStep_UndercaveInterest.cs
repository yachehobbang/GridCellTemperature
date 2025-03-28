using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public class GenStep_UndercaveInterest : GenStep
{
	private enum UnderCaveInterestKind
	{
		MushroomPatch,
		ChemfuelGenerator,
		InsectHive,
		CorpseGear,
		CorpsePile,
		SleepingFleshbeasts
	}

	private static readonly IntRange InterestPointCountRange = new IntRange(3, 5);

	private const int InterestPointSize = 20;

	private const float MinDistApart = 10f;

	private const float PatchDensity = 0.7f;

	private static readonly IntRange PatchSizeRange = new IntRange(50, 70);

	private static readonly IntRange ChemfuelCountRange = new IntRange(3, 5);

	private static readonly IntRange ChemfuelStackCountRange = new IntRange(10, 20);

	private const int ChemfuelStackMaxDist = 2;

	private const int ChemfuelPuddleSize = 20;

	private static readonly IntRange JellyCountRange = new IntRange(2, 3);

	private static readonly IntRange JellyStackCountRange = new IntRange(15, 40);

	private const int SpawnRadius = 3;

	private static readonly IntRange GlowPodCountRange = new IntRange(1, 2);

	private const int FilthAreaSize = 20;

	private const float FilthSpawnChance = 0.3f;

	private static readonly IntRange CorpseAgeRangeDays = new IntRange(15, 120);

	private const int GearDist = 1;

	private static readonly IntRange GearStackCountRange = new IntRange(2, 5);

	private static readonly IntRange CorpseCountRange = new IntRange(3, 6);

	private const int CorpseSpawnRadius = 4;

	private static readonly IntRange NumFleshbeastsRange = new IntRange(2, 4);

	private const int SleepingFleshbeastSpawnRadius = 4;

	public override int SeedPart => 26098423;

	public override void Generate(Map map, GenStepParams parms)
	{
		Thing pitGateExit = map.listerThings.ThingsOfDef(ThingDefOf.PitGateExit).FirstOrDefault();
		Pawn dreadmeld = map.mapPawns.AllPawnsSpawned.FirstOrDefault((Pawn p) => p.kindDef == PawnKindDefOf.Dreadmeld);
		int randomInRange = InterestPointCountRange.RandomInRange;
		List<IntVec3> interestPoints = new List<IntVec3>();
		for (int i = 0; i < randomInRange; i++)
		{
			if (CellFinder.TryFindRandomCell(map, delegate(IntVec3 c)
			{
				if (!c.Standable(map))
				{
					return false;
				}
				if (c.DistanceToEdge(map) <= 5)
				{
					return false;
				}
				if (pitGateExit != null && c.InHorDistOf(pitGateExit.Position, 10f))
				{
					return false;
				}
				if (dreadmeld != null && c.InHorDistOf(dreadmeld.Position, 10f))
				{
					return false;
				}
				return !interestPoints.Any((IntVec3 p) => c.InHorDistOf(p, 10f));
			}, out var result))
			{
				interestPoints.Add(result);
			}
		}
		foreach (IntVec3 item in interestPoints)
		{
			UnderCaveInterestKind underCaveInterestKind = Gen.RandomEnumValue<UnderCaveInterestKind>(disallowFirstValue: false);
			foreach (IntVec3 item2 in GridShapeMaker.IrregularLump(item, map, 20))
			{
				foreach (Thing item3 in item2.GetThingList(map).ToList())
				{
					if (item3.def.destroyable && ((item2.GetEdifice(map)?.def?.building?.isNaturalRock ?? false) || item2.GetEdifice(map)?.def == ThingDefOf.Fleshmass))
					{
						item3.Destroy();
					}
				}
			}
			switch (underCaveInterestKind)
			{
			case UnderCaveInterestKind.MushroomPatch:
				GenerateMushroomPatch(map, item);
				break;
			case UnderCaveInterestKind.ChemfuelGenerator:
				GenerateChemfuel(map, item);
				break;
			case UnderCaveInterestKind.InsectHive:
				GenerateHive(map, item);
				break;
			case UnderCaveInterestKind.CorpseGear:
				GenerateCorpseGear(map, item);
				break;
			case UnderCaveInterestKind.CorpsePile:
				GenerateCorpsePile(map, item);
				break;
			case UnderCaveInterestKind.SleepingFleshbeasts:
				GenerateSleepingFleshbeasts(map, item);
				break;
			}
		}
	}

	private void GenerateMushroomPatch(Map map, IntVec3 cell)
	{
		List<ThingDef> source = new List<ThingDef>
		{
			ThingDefOf.Plant_HealrootWild,
			ThingDefOf.Glowstool,
			ThingDefOf.Bryolux,
			ThingDefOf.Agarilux
		};
		foreach (IntVec3 item in GridShapeMaker.IrregularLump(cell, map, PatchSizeRange.RandomInRange))
		{
			map.terrainGrid.SetTerrain(item, TerrainDefOf.SoilRich);
			if (item.GetPlant(map) == null && item.GetCover(map) == null && item.GetEdifice(map) == null && Rand.Chance(0.7f))
			{
				((Plant)GenSpawn.Spawn(ThingMaker.MakeThing(source.RandomElement()), item, map)).Growth = Mathf.Clamp01(WildPlantSpawner.InitialGrowthRandomRange.RandomInRange);
			}
		}
	}

	private void GenerateChemfuel(Map map, IntVec3 cell)
	{
		GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.AncientGenerator ?? ThingDefOf.ChemfuelPoweredGenerator), cell, map);
		int randomInRange = ChemfuelCountRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			if (CellFinder.TryFindRandomCellNear(cell, map, 2, (IntVec3 c) => c.Standable(map), out var result))
			{
				Thing thing = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
				thing.stackCount = ChemfuelStackCountRange.RandomInRange;
				GenSpawn.Spawn(thing, result, map);
			}
		}
		foreach (IntVec3 item in GridShapeMaker.IrregularLump(cell, map, 20))
		{
			if (item.GetEdifice(map) == null)
			{
				FilthMaker.TryMakeFilth(item, map, ThingDefOf.Filth_Fuel);
			}
		}
	}

	private void GenerateHive(Map map, IntVec3 cell)
	{
		(GenSpawn.Spawn(ThingDefOf.Hive, cell, map) as Hive).GetComp<CompSpawnerHives>().canSpawnHives = false;
		int randomInRange = JellyCountRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			if (CellFinder.TryFindRandomCellNear(cell, map, 3, (IntVec3 c) => c.Standable(map), out var result))
			{
				Thing thing = ThingMaker.MakeThing(ThingDefOf.InsectJelly);
				thing.stackCount = JellyStackCountRange.RandomInRange;
				thing.SetForbidden(value: true);
				GenSpawn.Spawn(thing, result, map);
			}
		}
		int randomInRange2 = GlowPodCountRange.RandomInRange;
		for (int j = 0; j < randomInRange2; j++)
		{
			if (CellFinder.TryFindRandomCellNear(cell, map, 3, (IntVec3 c) => c.Standable(map), out var result2))
			{
				GenSpawn.Spawn(ThingDefOf.GlowPod, result2, map);
			}
		}
		foreach (IntVec3 item in GridShapeMaker.IrregularLump(cell, map, 20))
		{
			if (item.GetEdifice(map) == null && Rand.Chance(0.3f))
			{
				FilthMaker.TryMakeFilth(item, map, ThingDefOf.Filth_Slime);
			}
		}
	}

	private void GenerateCorpseGear(Map map, IntVec3 cell)
	{
		List<ThingDef> source = new List<ThingDef>
		{
			ThingDefOf.MedicineIndustrial,
			ThingDefOf.MealSurvivalPack
		};
		Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out var faction, tryMedievalOrBetter: true);
		Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Drifter, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
		pawn.health.SetDead();
		Corpse corpse = pawn.MakeCorpse(null, null);
		corpse.Age = Mathf.RoundToInt(CorpseAgeRangeDays.RandomInRange * 60000);
		corpse.GetComp<CompRottable>().RotProgress += corpse.Age;
		Find.WorldPawns.PassToWorld(pawn);
		GenSpawn.Spawn(corpse, cell, map);
		if (CellFinder.TryFindRandomCellNear(cell, map, 1, (IntVec3 c) => c.Standable(map), out var result))
		{
			Thing thing = ThingMaker.MakeThing(source.RandomElement());
			thing.stackCount = GearStackCountRange.RandomInRange;
			GenSpawn.Spawn(thing, result, map);
		}
	}

	private void GenerateCorpsePile(Map map, IntVec3 cell)
	{
		int randomInRange = CorpseCountRange.RandomInRange;
		Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out var faction, tryMedievalOrBetter: true);
		int age = Mathf.RoundToInt(CorpseAgeRangeDays.RandomInRange * 60000);
		for (int i = 0; i < randomInRange; i++)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Drifter, faction, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null));
			pawn.Kill(null);
			pawn.Corpse.Age = age;
			pawn.Corpse.GetComp<CompRottable>().RotProgress += pawn.Corpse.Age;
			if (CellFinder.TryFindRandomCellNear(cell, map, 4, (IntVec3 c) => c.Standable(map), out var result))
			{
				GenSpawn.Spawn(pawn.Corpse, result, map);
			}
		}
	}

	private void GenerateSleepingFleshbeasts(Map map, IntVec3 cell)
	{
		int randomInRange = NumFleshbeastsRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			Pawn newThing = PawnGenerator.GeneratePawn(PawnKindDefOf.Fingerspike, Faction.OfEntities);
			if (CellFinder.TryFindRandomCellNear(cell, map, 4, (IntVec3 c) => c.Standable(map) && c.GetFirstPawn(map) == null, out var result) && GenSpawn.Spawn(newThing, result, map).TryGetComp(out CompCanBeDormant comp))
			{
				comp.ToSleep();
			}
		}
	}
}
