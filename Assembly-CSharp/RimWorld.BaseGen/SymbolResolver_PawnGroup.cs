using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld.BaseGen;

public class SymbolResolver_PawnGroup : SymbolResolver
{
	private const float DefaultPoints = 250f;

	public static PawnGroupMakerParms GetGroupMakerParms(Map map, ResolveParams rp)
	{
		PawnGroupMakerParms pawnGroupMakerParms = rp.pawnGroupMakerParams;
		if (pawnGroupMakerParms == null)
		{
			pawnGroupMakerParms = new PawnGroupMakerParms();
			pawnGroupMakerParms.tile = map.Tile;
			pawnGroupMakerParms.faction = Find.FactionManager.RandomRaidableEnemyFaction();
			pawnGroupMakerParms.points = 250f;
		}
		pawnGroupMakerParms.groupKind = rp.pawnGroupKindDef ?? PawnGroupKindDefOf.Combat;
		return pawnGroupMakerParms;
	}

	public override bool CanResolve(ResolveParams rp)
	{
		if (!base.CanResolve(rp))
		{
			return false;
		}
		if (!rp.rect.Cells.Where((IntVec3 x) => x.Standable(BaseGen.globalSettings.map)).Any())
		{
			return false;
		}
		return true;
	}

	public override void Resolve(ResolveParams rp)
	{
		PawnGroupMakerParms groupMakerParms = GetGroupMakerParms(BaseGen.globalSettings.map, rp);
		List<PawnKindDef> list = new List<PawnKindDef>();
		foreach (Pawn item in PawnGroupMakerUtility.GeneratePawns(groupMakerParms))
		{
			list.Add(item.kindDef);
			ResolveParams resolveParams = rp;
			resolveParams.singlePawnToSpawn = item;
			BaseGen.symbolStack.Push("pawn", resolveParams);
		}
	}
}
