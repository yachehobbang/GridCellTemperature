using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimWorld;

public class Building_Door : Building
{
	public CompPowerTrader powerComp;

	protected bool openInt;

	protected bool holdOpenInt;

	private int lastFriendlyTouchTick = -9999;

	protected int ticksUntilClose;

	private Pawn approachingPawn;

	private int ticksOpen;

	protected int ticksSinceOpen;

	private bool freePassageWhenClearedReachabilityCache;

	private const float OpenTicks = 45f;

	private const int CloseDelayTicks = 110;

	private const int ApproachCloseDelayTicks = 300;

	private const int MaxTicksSinceFriendlyTouchToAutoClose = 120;

	private const float PowerOffDoorOpenSpeedFactor = 0.25f;

	private const float VisualDoorOffsetStart = 0f;

	private const float VisualDoorOffsetEnd = 0.45f;

	private const float NotifyFogGridDoorOpenPct = 0.4f;

	private const int TicksOpenToBreach = 600;

	private int CloseDelayAdjusted => Mathf.FloorToInt(110f * (DoorPowerOn ? def.building.poweredDoorCloseSpeedFactor : def.building.unpoweredDoorCloseSpeedFactor));

	private int WillCloseSoonThreshold => CloseDelayAdjusted + 1;

	public bool Open => openInt;

	public bool HoldOpen => holdOpenInt;

	public bool FreePassage
	{
		get
		{
			if (!openInt)
			{
				return false;
			}
			if (!holdOpenInt)
			{
				return !WillCloseSoon;
			}
			return true;
		}
	}

	public int TicksTillFullyOpened
	{
		get
		{
			int num = TicksToOpenNow - ticksSinceOpen;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
	}

	public bool WillCloseSoon
	{
		get
		{
			if (!base.Spawned)
			{
				return true;
			}
			if (!openInt)
			{
				return true;
			}
			if (holdOpenInt)
			{
				return false;
			}
			if (ticksUntilClose > 0 && ticksUntilClose <= WillCloseSoonThreshold && !BlockedOpenMomentary)
			{
				return true;
			}
			if (CanTryCloseAutomatically && !BlockedOpenMomentary)
			{
				return true;
			}
			foreach (IntVec3 item in this.OccupiedRect())
			{
				for (int i = 0; i < 5; i++)
				{
					IntVec3 c = item + GenAdj.CardinalDirectionsAndInside[i];
					if (!c.InBounds(base.Map))
					{
						continue;
					}
					List<Thing> thingList = c.GetThingList(base.Map);
					for (int j = 0; j < thingList.Count; j++)
					{
						if (thingList[j] is Pawn pawn && !pawn.HostileTo(this) && !pawn.Downed && (pawn.Position == item || (pawn.pather.Moving && pawn.pather.nextCell == item)))
						{
							return true;
						}
					}
				}
			}
			return false;
		}
	}

	public bool ContainmentBreached
	{
		get
		{
			if (openInt)
			{
				return ticksOpen >= 600;
			}
			return false;
		}
	}

	public bool BlockedOpenMomentary
	{
		get
		{
			if (StuckOpen)
			{
				return true;
			}
			foreach (IntVec3 item in this.OccupiedRect())
			{
				List<Thing> thingList = item.GetThingList(base.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					Thing thing = thingList[i];
					if (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Pawn)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	protected bool StuckOpen
	{
		get
		{
			if (!base.Spawned || def.size == IntVec2.One)
			{
				return false;
			}
			foreach (IntVec3 item in DoorUtility.WallRequirementCells(def, base.Position, base.Rotation))
			{
				if (!DoorUtility.EncapsulatingWallAt(item, base.Map))
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool DoorPowerOn
	{
		get
		{
			if (powerComp != null)
			{
				return powerComp.PowerOn;
			}
			return false;
		}
	}

	public bool SlowsPawns
	{
		get
		{
			if (DoorPowerOn)
			{
				return TicksToOpenNow > 20;
			}
			return true;
		}
	}

	public int TicksToOpenNow
	{
		get
		{
			float num = 45f / this.GetStatValue(StatDefOf.DoorOpenSpeed);
			num = ((!DoorPowerOn) ? (num * def.building.unpoweredDoorOpenSpeedFactor) : (num * (0.25f * def.building.poweredDoorOpenSpeedFactor)));
			return Mathf.RoundToInt(num);
		}
	}

	private bool CanTryCloseAutomatically
	{
		get
		{
			if (FriendlyTouchedRecently)
			{
				return !HoldOpen;
			}
			return false;
		}
	}

	private bool FriendlyTouchedRecently => Find.TickManager.TicksGame < lastFriendlyTouchTick + 120;

	public override bool FireBulwark
	{
		get
		{
			if (!Open)
			{
				return base.FireBulwark;
			}
			return false;
		}
	}

	protected float OpenPct => Mathf.Clamp01((float)ticksSinceOpen / (float)TicksToOpenNow);

	public override void PostMake()
	{
		base.PostMake();
		powerComp = GetComp<CompPowerTrader>();
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		powerComp = GetComp<CompPowerTrader>();
		ClearReachabilityCache(map);
		if (BlockedOpenMomentary)
		{
			DoorOpen();
		}
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		Map map = base.Map;
		base.DeSpawn(mode);
		ClearReachabilityCache(map);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref openInt, "open", defaultValue: false);
		Scribe_Values.Look(ref holdOpenInt, "holdOpen", defaultValue: false);
		Scribe_Values.Look(ref lastFriendlyTouchTick, "lastFriendlyTouchTick", 0);
		Scribe_Values.Look(ref ticksUntilClose, "ticksUntilClose", 0);
		Scribe_References.Look(ref approachingPawn, "approachingPawn");
		Scribe_Values.Look(ref ticksOpen, "ticksOpen", 0);
		if (Scribe.mode == LoadSaveMode.LoadingVars && openInt)
		{
			ticksSinceOpen = TicksToOpenNow;
		}
	}

	public override void SetFaction(Faction newFaction, Pawn recruiter = null)
	{
		base.SetFaction(newFaction, recruiter);
		if (base.Spawned)
		{
			ClearReachabilityCache(base.Map);
		}
	}

	public override void Tick()
	{
		base.Tick();
		if (FreePassage != freePassageWhenClearedReachabilityCache)
		{
			ClearReachabilityCache(base.Map);
		}
		if (!openInt)
		{
			if (ticksSinceOpen > 0)
			{
				ticksSinceOpen--;
			}
			ticksOpen = 0;
			if (this.IsHashIntervalTick(375))
			{
				GenTemperature.EqualizeTemperaturesThroughBuilding(this, 1f, twoWay: false);
			}
		}
		else
		{
			if (!openInt)
			{
				return;
			}
			if (ticksSinceOpen < TicksToOpenNow)
			{
				ticksSinceOpen++;
			}
			ticksOpen++;
			foreach (IntVec3 item in this.OccupiedRect())
			{
				List<Thing> thingList = item.GetThingList(base.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i] is Pawn p)
					{
						CheckFriendlyTouched(p);
					}
				}
			}
			if (ticksUntilClose > 0)
			{
				foreach (IntVec3 item2 in this.OccupiedRect())
				{
					if (base.Map.thingGrid.CellContains(item2, ThingCategory.Pawn))
					{
						ticksUntilClose = CloseDelayAdjusted;
						break;
					}
				}
				ticksUntilClose--;
				if (ticksUntilClose <= 0 && !holdOpenInt && !DoorTryClose())
				{
					ticksUntilClose = 1;
				}
			}
			else if (CanTryCloseAutomatically)
			{
				ticksUntilClose = CloseDelayAdjusted;
			}
			if ((Find.TickManager.TicksGame + thingIDNumber.HashOffset()) % 34 == 0)
			{
				GenTemperature.EqualizeTemperaturesThroughBuilding(this, 1f, twoWay: false);
			}
			if (OpenPct >= 0.4f && approachingPawn != null)
			{
				base.Map.fogGrid.Notify_PawnEnteringDoor(this, approachingPawn);
				approachingPawn = null;
			}
		}
	}

	public void CheckFriendlyTouched(Pawn p)
	{
		if (!p.HostileTo(this) && PawnCanOpen(p))
		{
			lastFriendlyTouchTick = Find.TickManager.TicksGame;
		}
	}

	public void Notify_PawnApproaching(Pawn p, float moveCost)
	{
		CheckFriendlyTouched(p);
		bool num = PawnCanOpen(p);
		if (num || Open)
		{
			approachingPawn = p;
		}
		if (num && !SlowsPawns)
		{
			DoorOpen(Mathf.Max(300, Mathf.CeilToInt(moveCost) + 1));
		}
	}

	public bool CanPhysicallyPass(Pawn p)
	{
		if (!FreePassage && !PawnCanOpen(p))
		{
			if (Open)
			{
				return p.HostileTo(this);
			}
			return false;
		}
		return true;
	}

	public virtual bool PawnCanOpen(Pawn p)
	{
		if (ModsConfig.AnomalyActive && p.IsMutant && !p.mutant.Def.canOpenDoors)
		{
			return false;
		}
		if (base.Map?.Parent != null && base.Map.Parent.doorsAlwaysOpenForPlayerPawns && p.Faction == Faction.OfPlayer)
		{
			return true;
		}
		Lord lord = p.GetLord();
		if (lord?.LordJob != null && lord.LordJob.CanOpenAnyDoor(p))
		{
			return true;
		}
		if (WildManUtility.WildManShouldReachOutsideNow(p))
		{
			return true;
		}
		if (p.RaceProps.FenceBlocked && !def.building.roamerCanOpen && (!p.roping.IsRopedByPawn || !PawnCanOpen(p.roping.RopedByPawn)))
		{
			return false;
		}
		if (base.Faction == null)
		{
			return p.RaceProps.canOpenFactionlessDoors;
		}
		if (p.guest != null && p.guest.Released)
		{
			return true;
		}
		if (ModsConfig.AnomalyActive)
		{
			if (p.kindDef == PawnKindDefOf.Revenant)
			{
				return true;
			}
			if (p.IsMutant && p.mutant.Def.canOpenAnyDoor)
			{
				return true;
			}
		}
		return GenAI.MachinesLike(base.Faction, p);
	}

	public override bool BlocksPawn(Pawn p)
	{
		if (openInt)
		{
			return false;
		}
		return !PawnCanOpen(p);
	}

	protected virtual void DoorOpen(int ticksToClose = 110)
	{
		if (openInt)
		{
			ticksUntilClose = ticksToClose;
		}
		else
		{
			ticksUntilClose = TicksToOpenNow + ticksToClose;
		}
		if (!openInt)
		{
			openInt = true;
			CheckClearReachabilityCacheBecauseOpenedOrClosed();
			if (DoorPowerOn)
			{
				def.building.soundDoorOpenPowered.PlayOneShot(this);
			}
			else
			{
				def.building.soundDoorOpenManual.PlayOneShot(this);
			}
		}
	}

	protected bool DoorTryClose()
	{
		if (holdOpenInt || BlockedOpenMomentary)
		{
			return false;
		}
		openInt = false;
		CheckClearReachabilityCacheBecauseOpenedOrClosed();
		if (DoorPowerOn)
		{
			def.building.soundDoorClosePowered.PlayOneShot(this);
		}
		else
		{
			def.building.soundDoorCloseManual.PlayOneShot(this);
		}
		return true;
	}

	public void StartManualOpenBy(Pawn opener)
	{
		DoorOpen();
	}

	public void StartManualCloseBy(Pawn closer)
	{
		ticksUntilClose = CloseDelayAdjusted;
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		DoorPreDraw();
		float offsetDist = 0f + 0.45f * OpenPct;
		DrawMovers(drawLoc, offsetDist, Graphic, AltitudeLayer.DoorMoveable.AltitudeFor(), Vector3.one, Graphic.ShadowGraphic);
	}

	protected void DrawMovers(Vector3 drawPos, float offsetDist, Graphic graphic, float altitude, Vector3 drawScaleFactor, Graphic_Shadow shadowGraphic)
	{
		for (int i = 0; i < 2; i++)
		{
			Mesh mesh;
			Vector3 vector;
			if (i == 0)
			{
				vector = new Vector3(0f, 0f, -def.size.x);
				mesh = MeshPool.plane10;
			}
			else
			{
				vector = new Vector3(0f, 0f, def.size.x);
				mesh = MeshPool.plane10Flip;
			}
			Rot4 rotation = base.Rotation;
			rotation.Rotate(RotationDirection.Clockwise);
			vector = rotation.AsQuat * vector;
			Vector3 vector2 = drawPos;
			vector2.y = altitude;
			vector2 += vector * offsetDist;
			Graphics.DrawMesh(mesh, Matrix4x4.TRS(vector2, base.Rotation.AsQuat, new Vector3((float)def.size.x * drawScaleFactor.x, drawScaleFactor.y, (float)def.size.z * drawScaleFactor.z)), graphic.MatAt(base.Rotation, this), 0);
			shadowGraphic?.DrawWorker(vector2, base.Rotation, def, this, 0f);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (base.Faction == Faction.OfPlayer)
		{
			Command_Toggle command_Toggle = new Command_Toggle();
			command_Toggle.defaultLabel = "CommandToggleDoorHoldOpen".Translate();
			command_Toggle.defaultDesc = "CommandToggleDoorHoldOpenDesc".Translate();
			command_Toggle.hotKey = KeyBindingDefOf.Misc3;
			command_Toggle.icon = TexCommand.HoldOpen;
			command_Toggle.isActive = () => holdOpenInt;
			command_Toggle.toggleAction = delegate
			{
				holdOpenInt = !holdOpenInt;
			};
			yield return command_Toggle;
		}
	}

	private void ClearReachabilityCache(Map map)
	{
		map.reachability.ClearCache();
		freePassageWhenClearedReachabilityCache = FreePassage;
	}

	private void CheckClearReachabilityCacheBecauseOpenedOrClosed()
	{
		if (base.Spawned)
		{
			base.Map.reachability.ClearCacheForHostile(this);
		}
	}

	protected void DoorPreDraw()
	{
		if (def.size == IntVec2.One)
		{
			base.Rotation = DoorUtility.DoorRotationAt(base.Position, base.Map, def.building.preferConnectingToFences);
		}
	}

	public override string GetInspectString()
	{
		string text = base.GetInspectString();
		if (StuckOpen)
		{
			if (!text.NullOrEmpty())
			{
				text += "\n";
			}
			text += "DoorMustBeEnclosedByWalls".Translate().Colorize(ColorLibrary.RedReadable);
		}
		return text;
	}
}
