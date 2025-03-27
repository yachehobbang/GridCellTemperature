using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class StockGenerator_Slaves : StockGenerator
{
	private bool respectPopulationIntent;

	public PawnKindDef slaveKindDef;

	public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
	{
		if (respectPopulationIntent && Rand.Value > StorytellerUtilityPopulation.PopulationIntent)
		{
			yield break;
		}
		if (faction != null && faction.ideos != null)
		{
			bool flag = true;
			foreach (Ideo allIdeo in faction.ideos.AllIdeos)
			{
				if (!allIdeo.IdeoApprovesOfSlavery())
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				yield break;
			}
		}
		int count = countRange.RandomInRange;
		for (int i = 0; i < count; i++)
		{
			if (!Find.FactionManager.AllFactionsVisible.Where((Faction fac) => fac != Faction.OfPlayer && fac.def.humanlikeFaction && !fac.temporary).TryRandomElement(out var result))
			{
				break;
			}
			DevelopmentalStage developmentalStages = (Find.Storyteller.difficulty.ChildrenAllowed ? (DevelopmentalStage.Child | DevelopmentalStage.Adult) : DevelopmentalStage.Adult);
			PawnGenerationRequest request = new PawnGenerationRequest((slaveKindDef != null) ? slaveKindDef : PawnKindDefOf.Slave, result, PawnGenerationContext.NonPlayer, forTile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, !trader.orbital, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, developmentalStages, null, null, null);
			yield return PawnGenerator.GeneratePawn(request);
		}
	}

	public override bool HandlesThingDef(ThingDef thingDef)
	{
		if (thingDef.category == ThingCategory.Pawn && thingDef.race.Humanlike)
		{
			return thingDef.tradeability != Tradeability.None;
		}
		return false;
	}
}
