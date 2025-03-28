using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public class SignalAction_Ambush : SignalAction
{
	public float points;

	public SignalActionAmbushType ambushType;

	public IntVec3 spawnNear = IntVec3.Invalid;

	public CellRect spawnAround;

	public bool spawnPawnsOnEdge;

	public bool useDropPods;

	private const int PawnsDelayAfterSpawnTicks = 120;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref points, "points", 0f);
		Scribe_Values.Look(ref ambushType, "ambushType", SignalActionAmbushType.Normal);
		Scribe_Values.Look(ref spawnNear, "spawnNear");
		Scribe_Values.Look(ref spawnAround, "spawnAround");
		Scribe_Values.Look(ref spawnPawnsOnEdge, "spawnPawnsOnEdge", defaultValue: false);
		Scribe_Values.Look(ref useDropPods, "useDropPods", defaultValue: false);
	}

	protected override void DoAction(SignalArgs args)
	{
		if (points <= 0f)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		foreach (Pawn item in GenerateAmbushPawns())
		{
			IntVec3 result;
			if (spawnPawnsOnEdge)
			{
				if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.Standable(base.Map) && !x.Fogged(base.Map) && base.Map.reachability.CanReachColony(x), base.Map, CellFinder.EdgeRoadChance_Ignore, out result))
				{
					Find.WorldPawns.PassToWorld(item);
					break;
				}
			}
			else if (!SiteGenStepUtility.TryFindSpawnCellAroundOrNear(spawnAround, spawnNear, base.Map, out result))
			{
				Find.WorldPawns.PassToWorld(item);
				break;
			}
			if (useDropPods)
			{
				DropPodUtility.DropThingsNear(result, base.Map, Gen.YieldSingle(item));
			}
			else
			{
				GenSpawn.Spawn(item, result, base.Map);
				if (!spawnPawnsOnEdge)
				{
					for (int i = 0; i < 10; i++)
					{
						FleckMaker.ThrowAirPuffUp(item.DrawPos, base.Map);
					}
				}
			}
			list.Add(item);
		}
		if (!list.Any())
		{
			return;
		}
		if (ambushType == SignalActionAmbushType.Manhunters)
		{
			for (int j = 0; j < list.Count; j++)
			{
				list[j].health.AddHediff(HediffDefOf.Scaria, null, null);
				list[j].mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
			}
		}
		else
		{
			Faction faction = list[0].Faction;
			LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), base.Map, list);
		}
		if (!spawnPawnsOnEdge && !useDropPods)
		{
			for (int k = 0; k < list.Count; k++)
			{
				list[k].jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 120), JobCondition.None, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, null);
				list[k].Rotation = Rot4.Random;
			}
		}
		Find.LetterStack.ReceiveLetter("LetterLabelAmbushInExistingMap".Translate(), "LetterAmbushInExistingMap".Translate(Faction.OfPlayer.def.pawnsPlural).CapitalizeFirst(), LetterDefOf.ThreatBig, list);
	}

	private IEnumerable<Pawn> GenerateAmbushPawns()
	{
		if (ambushType == SignalActionAmbushType.Manhunters)
		{
			if (!AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(points, base.Map.Tile, out var animalKind) && !AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(points, -1, out animalKind))
			{
				return Enumerable.Empty<Pawn>();
			}
			return AggressiveAnimalIncidentUtility.GenerateAnimals(animalKind, base.Map.Tile, points);
		}
		Faction faction = ((ambushType != SignalActionAmbushType.Mechanoids) ? ((base.Map.ParentFaction != null && base.Map.ParentFaction.HostileTo(Faction.OfPlayer)) ? base.Map.ParentFaction : Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: false)) : Faction.OfMechanoids);
		if (faction == null)
		{
			return Enumerable.Empty<Pawn>();
		}
		return PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
		{
			groupKind = PawnGroupKindDefOf.Combat,
			tile = base.Map.Tile,
			faction = faction,
			points = Mathf.Max(points, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat))
		});
	}
}
