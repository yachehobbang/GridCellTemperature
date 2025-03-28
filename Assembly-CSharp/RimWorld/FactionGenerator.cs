using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class FactionGenerator
{
	private const int MaxPreferredFactionNameLength = 20;

	private static readonly FloatRange SettlementsPer100kTiles = new FloatRange(75f, 85f);

	public static IEnumerable<FactionDef> ConfigurableFactions
	{
		get
		{
			foreach (FactionDef item in from f in DefDatabase<FactionDef>.AllDefs
				where f.maxConfigurableAtWorldCreation > 0
				orderby f.configurationListOrderPriority
				select f)
			{
				yield return item;
			}
		}
	}

	public static void GenerateFactionsIntoWorld(List<FactionDef> factions = null)
	{
		if (factions != null)
		{
			foreach (FactionDef faction2 in factions)
			{
				AddFactionToManager(faction2);
			}
		}
		else
		{
			IOrderedEnumerable<FactionDef> orderedEnumerable = DefDatabase<FactionDef>.AllDefs.OrderBy((FactionDef x) => x.hidden);
			foreach (FactionDef facDef in orderedEnumerable)
			{
				if (!orderedEnumerable.Any((FactionDef x) => x.requiredCountAtGameStart > 0 && x.replacesFaction == facDef))
				{
					for (int i = 0; i < facDef.requiredCountAtGameStart; i++)
					{
						AddFactionToManager(facDef);
					}
				}
			}
		}
		IEnumerable<Faction> source = Find.World.factionManager.AllFactionsListForReading.Where((Faction x) => !x.def.isPlayer && !x.Hidden && !x.temporary);
		if (source.Any())
		{
			int num = GenMath.RoundRandom((float)Find.WorldGrid.TilesCount / 100000f * SettlementsPer100kTiles.RandomInRange * Find.World.info.overallPopulation.GetScaleFactor());
			num -= Find.WorldObjects.Settlements.Count;
			for (int j = 0; j < num; j++)
			{
				Faction faction = source.RandomElementByWeight((Faction x) => x.def.settlementGenerationWeight);
				Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
				settlement.SetFaction(faction);
				settlement.Tile = TileFinder.RandomSettlementTileFor(faction);
				settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
				Find.WorldObjects.Add(settlement);
			}
		}
		Find.IdeoManager.SortIdeos();
	}

	private static void AddFactionToManager(FactionDef facDef)
	{
		CreateFactionAndAddToManager(facDef);
	}

	public static void CreateFactionAndAddToManager(FactionDef facDef)
	{
		if (facDef.fixedIdeo)
		{
			IdeoGenerationParms ideoGenerationParms = new IdeoGenerationParms(facDef, forceNoExpansionIdeo: false, null, null, name: facDef.ideoName, styles: facDef.styles, deities: facDef.deityPresets, hidden: facDef.hiddenIdeo, description: facDef.ideoDescription, forcedMemes: facDef.forcedMemes, classicExtra: false, forceNoWeaponPreference: false, forNewFluidIdeo: false, fixedIdeo: true, requiredPreceptsOnly: facDef.requiredPreceptsOnly);
			Find.FactionManager.Add(NewGeneratedFaction(new FactionGeneratorParms(facDef, ideoGenerationParms, null)));
		}
		else
		{
			Find.FactionManager.Add(NewGeneratedFaction(new FactionGeneratorParms(facDef, default(IdeoGenerationParms), null)));
		}
	}

	public static Faction NewGeneratedFaction(FactionGeneratorParms parms)
	{
		FactionDef factionDef = parms.factionDef;
		parms.ideoGenerationParms.forFaction = factionDef;
		Faction faction = new Faction();
		faction.def = factionDef;
		faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
		faction.colorFromSpectrum = NewRandomColorFromSpectrum(faction);
		faction.hidden = parms.hidden;
		if (factionDef.humanlikeFaction)
		{
			faction.ideos = new FactionIdeosTracker(faction);
			if (!faction.IsPlayer || !ModsConfig.IdeologyActive || !Find.GameInitData.startedFromEntry)
			{
				faction.ideos.ChooseOrGenerateIdeo(parms.ideoGenerationParms);
			}
		}
		if (!factionDef.isPlayer)
		{
			if (factionDef.fixedName != null)
			{
				faction.Name = factionDef.fixedName;
			}
			else
			{
				string text = "";
				for (int i = 0; i < 10; i++)
				{
					string text2 = NameGenerator.GenerateName(faction.def.factionNameMaker, Find.FactionManager.AllFactionsVisible.Select((Faction fac) => fac.Name));
					if (text2.Length <= 20)
					{
						text = text2;
					}
				}
				if (text.NullOrEmpty())
				{
					text = NameGenerator.GenerateName(faction.def.factionNameMaker, Find.FactionManager.AllFactionsVisible.Select((Faction fac) => fac.Name));
				}
				faction.Name = text;
			}
		}
		foreach (Faction item in Find.FactionManager.AllFactionsListForReading)
		{
			faction.TryMakeInitialRelationsWith(item);
		}
		if (!faction.Hidden && !factionDef.isPlayer)
		{
			Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
			settlement.SetFaction(faction);
			settlement.Tile = TileFinder.RandomSettlementTileFor(faction);
			settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement);
			Find.WorldObjects.Add(settlement);
		}
		faction.TryGenerateNewLeader();
		return faction;
	}

	public static Faction NewGeneratedFactionWithRelations(FactionDef facDef, List<FactionRelation> relations, bool hidden = false)
	{
		return NewGeneratedFactionWithRelations(new FactionGeneratorParms(facDef, default(IdeoGenerationParms), hidden), relations);
	}

	public static Faction NewGeneratedFactionWithRelations(FactionGeneratorParms parms, List<FactionRelation> relations)
	{
		Faction faction = NewGeneratedFaction(parms);
		for (int i = 0; i < relations.Count; i++)
		{
			faction.SetRelation(relations[i]);
		}
		return faction;
	}

	public static float NewRandomColorFromSpectrum(Faction faction)
	{
		float num = -1f;
		float result = 0f;
		for (int i = 0; i < 20; i++)
		{
			float value = Rand.Value;
			float num2 = 1f;
			List<Faction> allFactionsListForReading = Find.FactionManager.AllFactionsListForReading;
			for (int j = 0; j < allFactionsListForReading.Count; j++)
			{
				Faction faction2 = allFactionsListForReading[j];
				if (faction2.def == faction.def)
				{
					float num3 = Mathf.Abs(value - faction2.colorFromSpectrum);
					if (num3 < num2)
					{
						num2 = num3;
					}
				}
			}
			if (num2 > num)
			{
				num = num2;
				result = value;
			}
		}
		return result;
	}
}
