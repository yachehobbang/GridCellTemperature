using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public abstract class RoomContentsDeadBody : RoomContentsWorker
{
	private static readonly FloatRange CorpseAgeDaysRange = new FloatRange(3f, 30f);

	private static readonly IntRange SurvivalPacksCountRange = new IntRange(0, 2);

	private static readonly IntRange BloodFilthRange = new IntRange(1, 5);

	private static readonly List<PawnKindDef> bodyKinds = new List<PawnKindDef>();

	protected abstract ThingDef KillerThing { get; }

	protected abstract DamageDef DamageType { get; }

	protected abstract Tool ToolUsed { get; }

	public override void FillRoom(Map map, LayoutRoom room)
	{
		Corpse corpse = SpawnCorpse(map, room);
		int randomInRange = SurvivalPacksCountRange.RandomInRange;
		for (int i = 0; i < randomInRange; i++)
		{
			GenDrop.TryDropSpawn(ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack), corpse.Position, map, ThingPlaceMode.Near, out var _);
		}
		int randomInRange2 = BloodFilthRange.RandomInRange;
		for (int j = 0; j < randomInRange2; j++)
		{
			FilthMaker.TryMakeFilth(corpse.Position, map, ThingDefOf.Filth_Blood);
		}
	}

	protected Corpse SpawnCorpse(Map map, LayoutRoom room)
	{
		if (!room.TryGetRandomCellInRoom(map, out var cell))
		{
			return null;
		}
		return SpawnCorpse(cell, map);
	}

	protected Corpse SpawnCorpse(IntVec3 cell, Map map)
	{
		if (bodyKinds.Empty())
		{
			bodyKinds.AddRange(new PawnKindDef[3]
			{
				PawnKindDefOf.Pirate,
				PawnKindDefOf.Villager,
				PawnKindDefOf.PirateBoss
			});
		}
		PawnKindDef kind = bodyKinds.RandomElement();
		int deadTicks = Mathf.RoundToInt(CorpseAgeDaysRange.RandomInRange * 60000f);
		return SpawnCorpse(cell, kind, deadTicks, map, null);
	}

	protected Corpse SpawnCorpse(IntVec3 cell, PawnKindDef kind, int deadTicks, Map map, float? fixedAge = null, bool forceNoGear = false)
	{
		bool forceNoGear2 = forceNoGear;
		Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, !forceNoGear, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, fixedAge, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: false, dontGiveWeapon: false, onlyUseForcedBackstories: false, -1, 0, forceNoGear2));
		HealthUtility.SimulateKilled(pawn, DamageType, KillerThing, ToolUsed);
		pawn.Corpse.Age = deadTicks;
		pawn.relations.hidePawnRelations = true;
		pawn.Corpse.GetComp<CompRottable>().RotProgress += pawn.Corpse.Age;
		return (Corpse)GenSpawn.Spawn(pawn.Corpse, cell, map);
	}
}
