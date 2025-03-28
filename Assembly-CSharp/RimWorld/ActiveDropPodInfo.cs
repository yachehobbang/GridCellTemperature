using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class ActiveDropPodInfo : IThingHolder, IExposable
{
	public IThingHolder parent;

	public ThingOwner innerContainer;

	public int openDelay = 110;

	public bool leaveSlag;

	public bool savePawnsWithReferenceMode;

	public bool despawnPodBeforeSpawningThing;

	public WipeMode? spawnWipeMode;

	public Rot4? setRotation;

	public bool moveItemsAsideBeforeSpawning;

	public List<string> questTags;

	public const int DefaultOpenDelay = 110;

	private List<Thing> tmpThings = new List<Thing>();

	private List<Pawn> tmpSavedPawns = new List<Pawn>();

	public Thing SingleContainedThing
	{
		get
		{
			if (innerContainer.Count == 0)
			{
				return null;
			}
			if (innerContainer.Count > 1)
			{
				Log.Error("ContainedThing used on a DropPodInfo holding > 1 thing.");
			}
			return innerContainer[0];
		}
		set
		{
			innerContainer.Clear();
			innerContainer.TryAdd(value);
		}
	}

	public IThingHolder ParentHolder => parent;

	public ActiveDropPodInfo()
	{
		innerContainer = new ThingOwner<Thing>(this);
	}

	public ActiveDropPodInfo(IThingHolder parent)
	{
		innerContainer = new ThingOwner<Thing>(this);
		this.parent = parent;
	}

	public void ExposeData()
	{
		if (savePawnsWithReferenceMode && Scribe.mode == LoadSaveMode.Saving)
		{
			tmpThings.Clear();
			tmpThings.AddRange(innerContainer);
			tmpSavedPawns.Clear();
			for (int i = 0; i < tmpThings.Count; i++)
			{
				if (tmpThings[i] is Pawn item)
				{
					innerContainer.Remove(item);
					tmpSavedPawns.Add(item);
				}
			}
			tmpThings.Clear();
		}
		Scribe_Values.Look(ref savePawnsWithReferenceMode, "savePawnsWithReferenceMode", defaultValue: false);
		if (savePawnsWithReferenceMode)
		{
			Scribe_Collections.Look(ref tmpSavedPawns, "tmpSavedPawns", LookMode.Reference);
		}
		Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
		Scribe_Values.Look(ref openDelay, "openDelay", 110);
		Scribe_Values.Look(ref leaveSlag, "leaveSlag", defaultValue: false);
		Scribe_Values.Look(ref spawnWipeMode, "spawnWipeMode", null);
		Scribe_Values.Look(ref despawnPodBeforeSpawningThing, "despawnPodBeforeSpawningThing", defaultValue: false);
		Scribe_Values.Look(ref setRotation, "setRotation", null);
		Scribe_Values.Look(ref moveItemsAsideBeforeSpawning, "moveItemsAsideBeforeSpawning", defaultValue: false);
		Scribe_Collections.Look(ref questTags, "questTags", LookMode.Value);
		if (savePawnsWithReferenceMode && (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving))
		{
			for (int j = 0; j < tmpSavedPawns.Count; j++)
			{
				innerContainer.TryAdd(tmpSavedPawns[j]);
			}
			tmpSavedPawns.Clear();
		}
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}
}
