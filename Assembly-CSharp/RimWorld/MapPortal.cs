using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

[StaticConstructorOnStartup]
public abstract class MapPortal : Building, IThingHolder
{
	private static readonly Texture2D CancelEnterTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

	private static readonly Texture2D DefaultEnterTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter");

	public List<TransferableOneWay> leftToLoad;

	public PortalContainerProxy containerProxy;

	public bool notifiedCantLoadMore;

	public virtual bool AutoDraftOnEnter => false;

	protected virtual Texture2D EnterTex => DefaultEnterTex;

	public virtual string EnterCommandString => "EnterPortal".Translate(Label);

	public virtual string EnteringString => "EnteringPortal".Translate(Label);

	public bool LoadInProgress
	{
		get
		{
			if (leftToLoad != null)
			{
				return leftToLoad.Any();
			}
			return false;
		}
	}

	public bool AnyPawnCanLoadAnythingNow
	{
		get
		{
			if (!LoadInProgress)
			{
				return false;
			}
			if (!base.Spawned)
			{
				return false;
			}
			IReadOnlyList<Pawn> allPawnsSpawned = base.Map.mapPawns.AllPawnsSpawned;
			for (int i = 0; i < allPawnsSpawned.Count; i++)
			{
				if (allPawnsSpawned[i].CurJobDef == JobDefOf.HaulToPortal && ((JobDriver_HaulToPortal)allPawnsSpawned[i].jobs.curDriver).MapPortal == this)
				{
					return true;
				}
				if (allPawnsSpawned[i].CurJobDef == JobDefOf.EnterPortal && ((JobDriver_EnterPortal)allPawnsSpawned[i].jobs.curDriver).MapPortal == this)
				{
					return true;
				}
			}
			for (int j = 0; j < allPawnsSpawned.Count; j++)
			{
				Thing thing = allPawnsSpawned[j].mindState?.duty?.focus.Thing;
				if (thing != null && thing == this && allPawnsSpawned[j].CanReach(thing, PathEndMode.Touch, Danger.Deadly))
				{
					return true;
				}
			}
			for (int k = 0; k < allPawnsSpawned.Count; k++)
			{
				if (allPawnsSpawned[k].IsColonist && EnterPortalUtility.HasJobOnPortal(allPawnsSpawned[k], this))
				{
					return true;
				}
			}
			return false;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		containerProxy = new PortalContainerProxy
		{
			portal = this
		};
	}

	public override void Tick()
	{
		base.Tick();
		if (this.IsHashIntervalTick(60) && base.Spawned && LoadInProgress && !notifiedCantLoadMore && !AnyPawnCanLoadAnythingNow && leftToLoad[0]?.AnyThing != null)
		{
			notifiedCantLoadMore = true;
			Messages.Message("MessageCantLoadMoreIntoPortal".Translate(Label, Faction.OfPlayer.def.pawnsPlural, leftToLoad[0].AnyThing), this, MessageTypeDefOf.CautionInput);
		}
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return containerProxy;
	}

	public void Notify_ThingAdded(Thing t)
	{
		SubtractFromToLoadList(t, t.stackCount);
	}

	public void AddToTheToLoadList(TransferableOneWay t, int count)
	{
		if (!t.HasAnyThing || count <= 0)
		{
			return;
		}
		if (leftToLoad == null)
		{
			leftToLoad = new List<TransferableOneWay>();
		}
		TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t.AnyThing, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
		if (transferableOneWay != null)
		{
			for (int i = 0; i < t.things.Count; i++)
			{
				if (!transferableOneWay.things.Contains(t.things[i]))
				{
					transferableOneWay.things.Add(t.things[i]);
				}
			}
			if (transferableOneWay.CanAdjustBy(count).Accepted)
			{
				transferableOneWay.AdjustBy(count);
			}
		}
		else
		{
			TransferableOneWay transferableOneWay2 = new TransferableOneWay();
			leftToLoad.Add(transferableOneWay2);
			transferableOneWay2.things.AddRange(t.things);
			transferableOneWay2.AdjustTo(count);
		}
	}

	public int SubtractFromToLoadList(Thing t, int count)
	{
		if (leftToLoad == null)
		{
			return 0;
		}
		TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(t, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
		if (transferableOneWay == null)
		{
			return 0;
		}
		if (transferableOneWay.CountToTransfer <= 0)
		{
			return 0;
		}
		int num = Mathf.Min(count, transferableOneWay.CountToTransfer);
		transferableOneWay.AdjustBy(-num);
		if (transferableOneWay.CountToTransfer <= 0)
		{
			leftToLoad.Remove(transferableOneWay);
		}
		return num;
	}

	public void CancelLoad()
	{
		Lord lord = base.Map.lordManager.lords.FirstOrDefault((Lord l) => l.LordJob is LordJob_LoadAndEnterPortal lordJob_LoadAndEnterPortal && lordJob_LoadAndEnterPortal.portal == this);
		if (lord != null)
		{
			base.Map.lordManager.RemoveLord(lord);
		}
		leftToLoad.Clear();
	}

	public virtual bool IsEnterable(out string reason)
	{
		reason = "";
		return true;
	}

	public abstract Map GetOtherMap();

	public abstract IntVec3 GetDestinationLocation();

	public virtual void OnEntered(Pawn pawn)
	{
		Notify_ThingAdded(pawn);
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		Command_Action command_Action = new Command_Action();
		command_Action.action = delegate
		{
			Dialog_EnterPortal window = new Dialog_EnterPortal(this);
			Find.WindowStack.Add(window);
		};
		command_Action.icon = EnterTex;
		command_Action.defaultLabel = EnterCommandString + "...";
		command_Action.defaultDesc = "CommandEnterPortalDesc".Translate(Label);
		command_Action.Disabled = !IsEnterable(out var reason);
		command_Action.disabledReason = reason;
		yield return command_Action;
		if (LoadInProgress)
		{
			Command_Action command_Action2 = new Command_Action();
			command_Action2.action = CancelLoad;
			command_Action2.icon = CancelEnterTex;
			command_Action2.defaultLabel = "CommandCancelEnterPortal".Translate();
			command_Action2.defaultDesc = "CommandCancelEnterPortalDesc".Translate();
			yield return command_Action2;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref leftToLoad, "leftToLoad", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			leftToLoad?.RemoveAll((TransferableOneWay x) => x.AnyThing == null);
		}
	}
}
