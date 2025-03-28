using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public static class BillDialogUtility
{
	public static IEnumerable<Widgets.DropdownMenuElement<Pawn>> GetPawnRestrictionOptionsForBill(Bill bill, Func<Pawn, bool> pawnValidator = null)
	{
		SkillDef workSkill = bill.recipe.workSkill;
		IEnumerable<Pawn> allMaps_FreeColonists = PawnsFinder.AllMaps_FreeColonists;
		allMaps_FreeColonists = from p in allMaps_FreeColonists
			where pawnValidator == null || pawnValidator(p)
			select p into pawn
			orderby pawn.LabelShortCap
			select pawn;
		if (workSkill != null)
		{
			allMaps_FreeColonists = allMaps_FreeColonists.OrderByDescending((Pawn pawn) => pawn.skills.GetSkill(bill.recipe.workSkill).Level);
		}
		WorkGiverDef workGiver = bill.billStack.billGiver.GetWorkgiver();
		if (workGiver == null)
		{
			Log.ErrorOnce("Generating pawn restrictions for a BillGiver without a Workgiver", 96455148);
			yield break;
		}
		allMaps_FreeColonists = allMaps_FreeColonists.OrderByDescending((Pawn pawn) => pawn.workSettings.WorkIsActive(workGiver.workType));
		allMaps_FreeColonists = allMaps_FreeColonists.OrderBy((Pawn pawn) => pawn.WorkTypeIsDisabled(workGiver.workType));
		foreach (Pawn pawn2 in allMaps_FreeColonists)
		{
			if (pawn2.WorkTypeIsDisabled(workGiver.workType))
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption(string.Format("{0} ({1})", pawn2.LabelShortCap, "WillNever".Translate(workGiver.verb)), null),
					payload = pawn2
				};
			}
			else if (bill.recipe.workSkill != null && !pawn2.workSettings.WorkIsActive(workGiver.workType))
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption(string.Format("{0} ({1} {2}, {3})", pawn2.LabelShortCap, pawn2.skills.GetSkill(bill.recipe.workSkill).Level, bill.recipe.workSkill.label, "NotAssigned".Translate()), delegate
					{
						bill.SetPawnRestriction(pawn2);
					}),
					payload = pawn2
				};
			}
			else if (!pawn2.workSettings.WorkIsActive(workGiver.workType))
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption(string.Format("{0} ({1})", pawn2.LabelShortCap, "NotAssigned".Translate()), delegate
					{
						bill.SetPawnRestriction(pawn2);
					}),
					payload = pawn2
				};
			}
			else if (bill.recipe.workSkill != null)
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption($"{pawn2.LabelShortCap} ({pawn2.skills.GetSkill(bill.recipe.workSkill).Level} {bill.recipe.workSkill.label})", delegate
					{
						bill.SetPawnRestriction(pawn2);
					}),
					payload = pawn2
				};
			}
			else
			{
				yield return new Widgets.DropdownMenuElement<Pawn>
				{
					option = new FloatMenuOption($"{pawn2.LabelShortCap}", delegate
					{
						bill.SetPawnRestriction(pawn2);
					}),
					payload = pawn2
				};
			}
		}
	}
}
