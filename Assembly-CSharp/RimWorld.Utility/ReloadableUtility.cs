using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld.Utility;

public static class ReloadableUtility
{
	public static IReloadableComp FindSomeReloadableComponent(Pawn pawn, bool allowForcedReload)
	{
		if (pawn.equipment?.PrimaryEq != null && pawn.equipment.PrimaryEq is IReloadableComp reloadableComp && reloadableComp.NeedsReload(allowForcedReload))
		{
			return reloadableComp;
		}
		if (pawn.apparel != null)
		{
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				CompApparelReloadable compApparelReloadable = item.TryGetComp<CompApparelReloadable>();
				if (compApparelReloadable != null && compApparelReloadable.NeedsReload(allowForcedReload))
				{
					return compApparelReloadable;
				}
			}
		}
		return null;
	}

	public static List<Thing> FindEnoughAmmo(Pawn pawn, IntVec3 rootCell, IReloadableComp reloadable, bool forceReload)
	{
		if (reloadable == null)
		{
			return null;
		}
		IntRange desiredQuantity = new IntRange(reloadable.MinAmmoNeeded(forceReload), reloadable.MaxAmmoNeeded(forceReload));
		return RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, rootCell, desiredQuantity, (Thing t) => t.def == reloadable.AmmoDef);
	}

	public static IEnumerable<Pair<IReloadableComp, Thing>> FindPotentiallyReloadableGear(Pawn pawn, List<Thing> potentialAmmo)
	{
		if (pawn.equipment?.PrimaryEq != null)
		{
			IReloadableComp reloadableComp = pawn.equipment.PrimaryEq as IReloadableComp;
			if (TryGetAmmo(reloadableComp, out var ammo2))
			{
				yield return new Pair<IReloadableComp, Thing>(reloadableComp, ammo2);
			}
		}
		if (pawn.apparel == null)
		{
			yield break;
		}
		foreach (Apparel item in pawn.apparel.WornApparel)
		{
			IReloadableComp reloadableComp2 = item.TryGetComp<CompApparelReloadable>();
			if (TryGetAmmo(reloadableComp2, out var ammo3))
			{
				yield return new Pair<IReloadableComp, Thing>(reloadableComp2, ammo3);
			}
		}
		bool TryGetAmmo(IReloadableComp reloadable, out Thing ammo)
		{
			ammo = null;
			if (reloadable?.AmmoDef == null)
			{
				return false;
			}
			foreach (Thing item2 in potentialAmmo)
			{
				if (item2.def == reloadable.AmmoDef)
				{
					ammo = item2;
					return true;
				}
			}
			return false;
		}
	}

	public static Pawn OwnerOf(IReloadableComp reloadable)
	{
		IThingHolder parentHolder = reloadable.ReloadableThing.ParentHolder;
		if (parentHolder != null)
		{
			if (parentHolder is Pawn_ApparelTracker pawn_ApparelTracker)
			{
				return pawn_ApparelTracker.pawn;
			}
			if (parentHolder is Pawn_EquipmentTracker pawn_EquipmentTracker)
			{
				return pawn_EquipmentTracker.pawn;
			}
		}
		return null;
	}

	public static int TotalChargesFromQueuedJobs(Pawn pawn, ThingWithComps gear)
	{
		ICompWithCharges compWithCharges = gear.TryGetComp<CompApparelVerbOwner_Charged>();
		ICompWithCharges compWithCharges2 = compWithCharges ?? gear.TryGetComp<CompEquippableAbilityReloadable>();
		int num = 0;
		if (compWithCharges2 != null && pawn != null)
		{
			foreach (Job item in pawn.jobs.AllJobs())
			{
				Verb verbToUse = item.verbToUse;
				if (verbToUse != null && (compWithCharges2 == verbToUse.ReloadableCompSource || compWithCharges2 == verbToUse.DirectOwner))
				{
					num++;
				}
			}
		}
		return num;
	}

	public static bool CanUseConsideringQueuedJobs(Pawn pawn, ThingWithComps gear, bool showMessage = true)
	{
		ICompWithCharges compWithCharges = gear.TryGetComp<CompApparelVerbOwner_Charged>();
		ICompWithCharges compWithCharges2 = compWithCharges ?? gear.TryGetComp<CompEquippableAbilityReloadable>();
		if (compWithCharges2 == null)
		{
			return true;
		}
		string text = null;
		if (!Event.current.shift)
		{
			if (!compWithCharges2.CanBeUsed(out var reason))
			{
				text = reason;
			}
		}
		else if (TotalChargesFromQueuedJobs(pawn, gear) + 1 > compWithCharges2.RemainingCharges)
		{
			text = ((!(compWithCharges2 is IReloadableComp reloadableComp)) ? ((string)"CommandNoUsesLeft".Translate()) : reloadableComp.DisabledReason(reloadableComp.MaxAmmoAmount(), reloadableComp.MaxAmmoAmount()));
		}
		if (text != null)
		{
			if (showMessage)
			{
				Messages.Message(text, pawn, MessageTypeDefOf.RejectInput, historical: false);
			}
			return false;
		}
		return true;
	}
}
