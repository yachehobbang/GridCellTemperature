using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public class MainTabWindow_Animals : MainTabWindow_PawnTable
{
	protected override PawnTableDef PawnTableDef => PawnTableDefOf.Animals;

	protected override IEnumerable<Pawn> Pawns => from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
		where p.IsNonMutantAnimal
		select p;

	public override void DoWindowContents(Rect rect)
	{
		base.DoWindowContents(rect);
		if (Widgets.ButtonText(new Rect(rect.x, rect.y, Mathf.Min(rect.width, 260f), 32f), "ManageAutoSlaughter".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Find.WindowStack.Add(new Dialog_AutoSlaughter(Find.CurrentMap));
		}
	}
}
