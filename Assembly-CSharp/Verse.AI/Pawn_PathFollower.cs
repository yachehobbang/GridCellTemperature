using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace Verse.AI;

public class Pawn_PathFollower : IExposable
{
	protected Pawn pawn;

	private bool moving;

	public IntVec3 nextCell;

	private IntVec3 lastCell;

	public float lastMoveDirection;

	public float nextCellCostLeft;

	public float nextCellCostTotal = 1f;

	private int cellsUntilClamor;

	private int lastMovedTick = -999999;

	public bool debugDisabled;

	private LocalTargetInfo destination;

	private PathEndMode peMode;

	public PawnPath curPath;

	public IntVec3 lastPathedTargetPosition;

	private int foundPathWhichCollidesWithPawns = -999999;

	private int foundPathWithDanger = -999999;

	private int failedToFindCloseUnoccupiedCellTicks = -999999;

	private float cachedMovePercentage;

	private bool cachedWillCollideNextCell;

	private const int MaxMoveTicks = 450;

	private const int MaxCheckAheadNodes = 20;

	private const float SnowReductionFromWalking = 0.001f;

	private const int ClamorCellsInterval = 12;

	private const int MinCostWalk = 50;

	private const int MinCostAmble = 60;

	private const int CheckForMovingCollidingPawnsIfCloserToTargetThanX = 15;

	private const int AttackBlockingHostilePawnAfterTicks = 180;

	private const int WaitForRopeeTicks = 60;

	private const float RopeLength = 8f;

	public LocalTargetInfo Destination => destination;

	public bool Moving => moving;

	public bool MovingNow
	{
		get
		{
			if (Moving)
			{
				return !WillCollideNextCell;
			}
			return false;
		}
	}

	public float MovePercentage => cachedMovePercentage;

	public int LastMovedTick => lastMovedTick;

	public bool WillCollideNextCell => cachedWillCollideNextCell;

	public IntVec3 LastPassableCellInPath
	{
		get
		{
			if (!Moving || curPath == null)
			{
				return IntVec3.Invalid;
			}
			if (!Destination.Cell.Impassable(pawn.Map))
			{
				return Destination.Cell;
			}
			List<IntVec3> nodesReversed = curPath.NodesReversed;
			for (int i = 0; i < nodesReversed.Count; i++)
			{
				if (!nodesReversed[i].Impassable(pawn.Map))
				{
					return nodesReversed[i];
				}
			}
			if (!pawn.Position.Impassable(pawn.Map))
			{
				return pawn.Position;
			}
			return IntVec3.Invalid;
		}
	}

	public Pawn_PathFollower(Pawn newPawn)
	{
		pawn = newPawn;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref moving, "moving", defaultValue: true);
		Scribe_Values.Look(ref nextCell, "nextCell");
		Scribe_Values.Look(ref nextCellCostLeft, "nextCellCostLeft", 0f);
		Scribe_Values.Look(ref nextCellCostTotal, "nextCellCostInitial", 0f);
		Scribe_Values.Look(ref peMode, "peMode", PathEndMode.None);
		Scribe_Values.Look(ref cellsUntilClamor, "cellsUntilClamor", 0);
		Scribe_Values.Look(ref lastMovedTick, "lastMovedTick", -999999);
		Scribe_Values.Look(ref debugDisabled, "debugDisabled", defaultValue: false);
		if (moving)
		{
			Scribe_TargetInfo.Look(ref destination, "destination");
		}
	}

	public void StartPath(LocalTargetInfo dest, PathEndMode peMode)
	{
		dest = (LocalTargetInfo)GenPath.ResolvePathMode(pawn, dest.ToTargetInfo(pawn.Map), ref peMode);
		if (dest.HasThing && dest.ThingDestroyed)
		{
			Log.Error(string.Concat(pawn, " pathing to destroyed thing ", dest.Thing, " curJob=", pawn.CurJob.ToStringSafe()));
			PatherFailed();
		}
		else
		{
			if ((!PawnCanOccupy(pawn.Position) && !TryRecoverFromUnwalkablePosition()) || (moving && curPath != null && destination == dest && this.peMode == peMode))
			{
				return;
			}
			if (!pawn.Map.reachability.CanReach(pawn.Position, dest, peMode, TraverseParms.For(TraverseMode.PassDoors)))
			{
				PatherFailed();
				return;
			}
			this.peMode = peMode;
			destination = dest;
			if (!IsNextCellWalkable() || NextCellDoorToWaitForOrManuallyOpen() != null || nextCellCostLeft == nextCellCostTotal)
			{
				ResetToCurrentPosition();
			}
			PawnDestinationReservationManager.PawnDestinationReservation pawnDestinationReservation = pawn.Map.pawnDestinationReservationManager.MostRecentReservationFor(pawn);
			if (pawnDestinationReservation != null && ((destination.HasThing && pawnDestinationReservation.target != destination.Cell) || (pawnDestinationReservation.job != pawn.CurJob && pawnDestinationReservation.target != destination.Cell)))
			{
				pawn.Map.pawnDestinationReservationManager.ObsoleteAllClaimedBy(pawn);
			}
			if (AtDestinationPosition())
			{
				PatherArrived();
				return;
			}
			if (pawn.Downed && !pawn.health.CanCrawl)
			{
				Log.Error(pawn.LabelCap + " tried to path while downed. This should never happen. curJob=" + pawn.CurJob.ToStringSafe());
				PatherFailed();
				return;
			}
			curPath?.ReleaseToPool();
			curPath = null;
			moving = true;
			pawn.jobs.posture = PawnPosture.Standing;
			cachedMovePercentage = 0f;
			cachedWillCollideNextCell = false;
		}
	}

	public void StopDead()
	{
		curPath?.ReleaseToPool();
		curPath = null;
		moving = false;
		nextCell = pawn.Position;
		cachedMovePercentage = 0f;
		cachedWillCollideNextCell = false;
	}

	public void PatherTick()
	{
		cachedMovePercentage = 0f;
		if (this.pawn.RaceProps.doesntMove || debugDisabled)
		{
			return;
		}
		if (WillCollideWithPawnAt(this.pawn.Position, onlyStanding: true))
		{
			if (FailedToFindCloseUnoccupiedCellRecently())
			{
				return;
			}
			if (CellFinder.TryFindBestPawnStandCell(this.pawn, out var cell, cellByCell: true) && cell != this.pawn.Position)
			{
				if (DebugViewSettings.drawPatherState)
				{
					MoteMaker.ThrowText(this.pawn.DrawPos, this.pawn.Map, "Unstuck");
				}
				this.pawn.Position = cell;
				ResetToCurrentPosition();
				if (moving && TrySetNewPath())
				{
					TryEnterNextPathCell();
				}
			}
			else
			{
				failedToFindCloseUnoccupiedCellTicks = Find.TickManager.TicksGame;
			}
		}
		else
		{
			if (this.pawn.stances.FullBodyBusy)
			{
				return;
			}
			cachedWillCollideNextCell = WillCollideWithPawnAt(nextCell, this.pawn.IsShambler && !this.pawn.mindState.anyCloseHostilesRecently);
			if (moving && WillCollideNextCell)
			{
				cachedMovePercentage = 1f - nextCellCostLeft / nextCellCostTotal;
				if (((curPath != null && curPath.NodesLeftCount < 15) || PawnUtility.AnyPawnBlockingPathAt(nextCell, this.pawn, actAsIfHadCollideWithPawnsJob: false, collideOnlyWithStandingPawns: true)) && !BestPathHadPawnsInTheWayRecently() && !BestPathHadPawnsInTheWayRecently() && TrySetNewPath())
				{
					if (DebugViewSettings.drawPatherState)
					{
						MoteMaker.ThrowText(this.pawn.DrawPos, this.pawn.Map, "Repathed");
					}
					ResetToCurrentPosition();
					TryEnterNextPathCell();
				}
				else
				{
					if (Find.TickManager.TicksGame - lastMovedTick < 180)
					{
						return;
					}
					Pawn pawn = PawnUtility.PawnBlockingPathAt(nextCell, this.pawn);
					if (pawn != null && this.pawn.HostileTo(pawn))
					{
						if (this.pawn.CanAttackWhenPathingBlocked && this.pawn.TryGetAttackVerb(pawn) != null)
						{
							Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, pawn);
							job.maxNumMeleeAttacks = 1;
							job.expiryInterval = 300;
							this.pawn.jobs.StartJob(job, JobCondition.Incompletable, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, null);
						}
						else
						{
							this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
						}
					}
				}
			}
			else if (nextCellCostLeft > 0f || moving)
			{
				if (nextCellCostLeft > 0f)
				{
					nextCellCostLeft -= CostToPayThisTick();
				}
				else
				{
					TryEnterNextPathCell();
				}
				lastMovedTick = Find.TickManager.TicksGame;
				if (this.pawn.pather != null && this.pawn.pather.BuildingBlockingNextPathCell() == null && this.pawn.pather.NextCellDoorToWaitForOrManuallyOpen() == null)
				{
					cachedMovePercentage = 1f - nextCellCostLeft / nextCellCostTotal;
				}
			}
		}
	}

	public void DrawDebugGUI()
	{
		Rect adjustedScreenspaceRect = SilhouetteUtility.GetAdjustedScreenspaceRect(pawn, 0.025f);
		Color? color = null;
		if (WillCollideWithPawnAt(pawn.Position, onlyStanding: true))
		{
			color = ((!FailedToFindCloseUnoccupiedCellRecently()) ? new Color?(new Color(0.14f, 0.93f, 1f, 0.5f)) : new Color?(new Color(1f, 0f, 0f, 0.5f)));
		}
		else if (pawn.stances.FullBodyBusy)
		{
			color = new Color(0.37f, 1f, 0.19f, 0.5f);
		}
		else if (Moving && WillCollideNextCell)
		{
			color = new Color(1f, 0.71f, 0.22f, 0.5f);
		}
		if (color.HasValue)
		{
			GUI.DrawTexture(adjustedScreenspaceRect, TexUI.DotHighlight, ScaleMode.ScaleToFit, alphaBlend: true, 0f, color.Value, 0f, 0f);
		}
	}

	public void TryResumePathingAfterLoading()
	{
		if (moving)
		{
			if (destination.HasThing && destination.ThingDestroyed)
			{
				PatherFailed();
			}
			else
			{
				StartPath(destination, peMode);
			}
		}
	}

	public void Notify_Teleported_Int()
	{
		StopDead();
		ResetToCurrentPosition();
	}

	public void ResetToCurrentPosition()
	{
		nextCell = pawn.Position;
		nextCellCostLeft = 0f;
		nextCellCostTotal = 1f;
		cachedMovePercentage = 0f;
	}

	private bool PawnCanOccupy(IntVec3 c)
	{
		if (!c.WalkableBy(pawn.Map, pawn))
		{
			return false;
		}
		Building_Door door = c.GetDoor(pawn.Map);
		if (door != null && !door.PawnCanOpen(pawn) && !door.Open)
		{
			return false;
		}
		return true;
	}

	public Building BuildingBlockingNextPathCell()
	{
		Building edifice = nextCell.GetEdifice(pawn.Map);
		if (edifice != null && edifice.BlocksPawn(pawn))
		{
			return edifice;
		}
		return null;
	}

	public void NotifyThingTransformed(Thing from, Thing to)
	{
		if (destination.HasThing && destination.Thing == from)
		{
			destination = new LocalTargetInfo(to);
		}
	}

	private bool IsNextCellWalkable()
	{
		if (!nextCell.WalkableBy(pawn.Map, pawn))
		{
			return false;
		}
		if (WillCollideWithPawnAt(nextCell))
		{
			return false;
		}
		return true;
	}

	private bool WillCollideWithPawnAt(IntVec3 c, bool onlyStanding = false)
	{
		if (!PawnUtility.ShouldCollideWithPawns(pawn))
		{
			return false;
		}
		return PawnUtility.AnyPawnBlockingPathAt(c, pawn, actAsIfHadCollideWithPawnsJob: false, onlyStanding);
	}

	public Building_Door NextCellDoorToWaitForOrManuallyOpen()
	{
		Building_Door door = nextCell.GetDoor(pawn.Map);
		if (door != null && door.SlowsPawns && (!door.Open || door.TicksTillFullyOpened > 0) && door.PawnCanOpen(pawn))
		{
			return door;
		}
		return null;
	}

	private Pawn RopeeWithStretchedRopeAtNextPathCell()
	{
		List<Pawn> ropees = this.pawn.roping.Ropees;
		for (int i = 0; i < ropees.Count; i++)
		{
			Pawn pawn = ropees[i];
			if (!pawn.Position.InHorDistOf(nextCell, 8f))
			{
				return pawn;
			}
		}
		return null;
	}

	public void PatherDraw()
	{
		if (!Find.ScreenshotModeHandler.Active && DebugViewSettings.drawPaths && curPath != null && Find.Selector.IsSelected(pawn))
		{
			curPath.DrawPath(pawn);
		}
	}

	public bool MovedRecently(int ticks)
	{
		return Find.TickManager.TicksGame - lastMovedTick <= ticks;
	}

	public bool TryRecoverFromUnwalkablePosition(bool error = true)
	{
		bool flag = false;
		for (int i = 0; i < GenRadial.RadialPattern.Length; i++)
		{
			IntVec3 intVec = pawn.Position + GenRadial.RadialPattern[i];
			if (!PawnCanOccupy(intVec))
			{
				continue;
			}
			if (intVec == pawn.Position)
			{
				return true;
			}
			if (error)
			{
				Log.Warning(string.Concat(pawn, " on unwalkable cell ", pawn.Position, ". Teleporting to ", intVec));
			}
			pawn.Position = intVec;
			pawn.Notify_Teleported(endCurrentJob: true, resetTweenedPos: false);
			flag = true;
			break;
		}
		if (!flag)
		{
			pawn.Destroy();
			Log.Error(string.Concat(pawn.ToStringSafe(), " on unwalkable cell ", pawn.Position, ". Could not find walkable position nearby. Destroyed."));
		}
		return flag;
	}

	private void PatherArrived()
	{
		StopDead();
		if (pawn.jobs.curJob != null)
		{
			pawn.jobs.curDriver.Notify_PatherArrived();
		}
	}

	private void PatherFailed()
	{
		StopDead();
		pawn.jobs.curDriver.Notify_PatherFailed();
	}

	private void TryEnterNextPathCell()
	{
		Building building = BuildingBlockingNextPathCell();
		if (building != null)
		{
			if (!(building is Building_Door { FreePassage: not false }))
			{
				if ((this.pawn.CurJob != null && this.pawn.CurJob.canBashDoors) || this.pawn.HostileTo(building))
				{
					MakeBashBlockerJob(building);
				}
				else
				{
					PatherFailed();
				}
				return;
			}
			if (building.def.IsFence && this.pawn.def.race.FenceBlocked)
			{
				if (this.pawn.CurJob != null && this.pawn.CurJob.canBashFences)
				{
					MakeBashBlockerJob(building);
				}
				else
				{
					PatherFailed();
				}
				return;
			}
		}
		Building_Door building_Door2 = NextCellDoorToWaitForOrManuallyOpen();
		if (building_Door2 != null)
		{
			if (!building_Door2.Open)
			{
				building_Door2.StartManualOpenBy(this.pawn);
			}
			Stance_Cooldown stance_Cooldown = new Stance_Cooldown(building_Door2.TicksTillFullyOpened, building_Door2, null);
			stance_Cooldown.neverAimWeapon = true;
			this.pawn.stances.SetStance(stance_Cooldown);
			building_Door2.CheckFriendlyTouched(this.pawn);
			return;
		}
		lastCell = this.pawn.Position;
		this.pawn.Position = nextCell;
		lastMoveDirection = (nextCell - lastCell).AngleFlat;
		if (this.pawn.RaceProps.Humanlike)
		{
			cellsUntilClamor--;
			if (cellsUntilClamor <= 0)
			{
				GenClamor.DoClamor(this.pawn, 7f, ClamorDefOf.Movement);
				cellsUntilClamor = 12;
			}
		}
		this.pawn.filth.Notify_EnteredNewCell();
		if (this.pawn.BodySize > 0.9f)
		{
			this.pawn.Map.snowGrid.AddDepth(this.pawn.Position, -0.001f);
		}
		Building_Door door = lastCell.GetDoor(this.pawn.Map);
		if (door != null && !this.pawn.HostileTo(door))
		{
			door.CheckFriendlyTouched(this.pawn);
			if (!door.BlockedOpenMomentary && !door.HoldOpen && door.SlowsPawns && door.PawnCanOpen(this.pawn))
			{
				door.StartManualCloseBy(this.pawn);
				return;
			}
		}
		Pawn pawn = RopeeWithStretchedRopeAtNextPathCell();
		if (pawn != null)
		{
			Stance_Cooldown stance_Cooldown2 = new Stance_Cooldown(60, pawn, null);
			stance_Cooldown2.neverAimWeapon = true;
			this.pawn.stances.SetStance(stance_Cooldown2);
		}
		else if (!NeedNewPath() || TrySetNewPath())
		{
			if (AtDestinationPosition())
			{
				PatherArrived();
			}
			else
			{
				SetupMoveIntoNextCell();
			}
		}
	}

	private void MakeBashBlockerJob(Building blocker)
	{
		Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, blocker);
		job.expiryInterval = 300;
		pawn.jobs.StartJob(job, JobCondition.Incompletable, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, null);
	}

	private void SetupMoveIntoNextCell()
	{
		if (curPath.NodesLeftCount <= 1)
		{
			Log.Error(string.Concat(pawn, " at ", pawn.Position, " ran out of path nodes while pathing to ", destination, "."));
			PatherFailed();
			return;
		}
		nextCell = curPath.ConsumeNextNode();
		if (!nextCell.WalkableBy(pawn.Map, pawn))
		{
			Log.Error(string.Concat(pawn, " entering ", nextCell, " which is unwalkable."));
		}
		float moveCost = (nextCellCostLeft = (nextCellCostTotal = CostToMoveIntoCell(nextCell)));
		cachedMovePercentage = 0f;
		cachedWillCollideNextCell = WillCollideWithPawnAt(nextCell);
		nextCell.GetDoor(pawn.Map)?.Notify_PawnApproaching(pawn, moveCost);
	}

	private float CostToMoveIntoCell(IntVec3 c)
	{
		return CostToMoveIntoCell(pawn, c);
	}

	private static float CostToMoveIntoCell(Pawn pawn, IntVec3 c)
	{
		float num = ((c.x != pawn.Position.x && c.z != pawn.Position.z) ? pawn.TicksPerMoveDiagonal : pawn.TicksPerMoveCardinal);
		num += (float)pawn.Map.pathing.For(pawn).pathGrid.CalculatedCostAt(c, perceivedStatic: false, pawn.Position);
		Building edifice = c.GetEdifice(pawn.Map);
		if (edifice != null)
		{
			num += (float)(int)edifice.PathWalkCostFor(pawn);
		}
		if (num > 450f)
		{
			num = 450f;
		}
		if (pawn.CurJob != null)
		{
			Pawn locomotionUrgencySameAs = pawn.jobs.curDriver.locomotionUrgencySameAs;
			if (locomotionUrgencySameAs != null && locomotionUrgencySameAs != pawn && locomotionUrgencySameAs.Spawned)
			{
				float num2 = CostToMoveIntoCell(locomotionUrgencySameAs, c);
				if (num < num2)
				{
					num = num2;
				}
			}
			else
			{
				switch (pawn.jobs.curJob.locomotionUrgency)
				{
				case LocomotionUrgency.Amble:
					num *= 3f;
					if (num < 60f)
					{
						num = 60f;
					}
					break;
				case LocomotionUrgency.Walk:
					num *= 2f;
					if (num < 50f)
					{
						num = 50f;
					}
					break;
				case LocomotionUrgency.Jog:
					num *= 1f;
					break;
				case LocomotionUrgency.Sprint:
					num = Mathf.RoundToInt(num * 0.75f);
					break;
				}
			}
		}
		return Mathf.Max(num, 1f);
	}

	private float CostToPayThisTick()
	{
		float num = 1f;
		if (pawn.stances.stagger.Staggered)
		{
			num *= pawn.stances.stagger.StaggerMoveSpeedFactor;
		}
		if (num < nextCellCostTotal / 450f)
		{
			num = nextCellCostTotal / 450f;
		}
		return num;
	}

	private bool TrySetNewPath()
	{
		PawnPath pawnPath = GenerateNewPath();
		if (!pawnPath.Found)
		{
			PatherFailed();
			return false;
		}
		if (curPath != null)
		{
			curPath.ReleaseToPool();
		}
		curPath = pawnPath;
		for (int i = 0; i < 20 && i < curPath.NodesLeftCount; i++)
		{
			IntVec3 c = curPath.Peek(i);
			if (PawnUtility.ShouldCollideWithPawns(pawn) && PawnUtility.AnyPawnBlockingPathAt(c, pawn))
			{
				foundPathWhichCollidesWithPawns = Find.TickManager.TicksGame;
			}
			if (PawnUtility.KnownDangerAt(c, pawn.Map, pawn))
			{
				foundPathWithDanger = Find.TickManager.TicksGame;
			}
			if (foundPathWhichCollidesWithPawns == Find.TickManager.TicksGame && foundPathWithDanger == Find.TickManager.TicksGame)
			{
				break;
			}
		}
		return true;
	}

	private PawnPath GenerateNewPath()
	{
		lastPathedTargetPosition = destination.Cell;
		return pawn.Map.pathFinder.FindPath(pawn.Position, destination, pawn, peMode);
	}

	private bool AtDestinationPosition()
	{
		return pawn.CanReachImmediate(destination, peMode);
	}

	private bool NeedNewPath()
	{
		if (!destination.IsValid || curPath == null || !curPath.Found || curPath.NodesLeftCount == 0)
		{
			return true;
		}
		if (destination.HasThing && destination.Thing.Map != pawn.Map)
		{
			return true;
		}
		if ((pawn.Position.InHorDistOf(curPath.LastNode, 15f) || pawn.Position.InHorDistOf(destination.Cell, 15f)) && !ReachabilityImmediate.CanReachImmediate(curPath.LastNode, destination, pawn.Map, peMode, pawn))
		{
			return true;
		}
		if (curPath.UsedRegionHeuristics && curPath.NodesConsumedCount >= 75)
		{
			return true;
		}
		if (lastPathedTargetPosition != destination.Cell)
		{
			float num = (pawn.Position - destination.Cell).LengthHorizontalSquared;
			float num2 = ((num > 900f) ? 10f : ((num > 289f) ? 5f : ((num > 100f) ? 3f : ((!(num > 49f)) ? 0.5f : 2f))));
			if ((float)(lastPathedTargetPosition - destination.Cell).LengthHorizontalSquared > num2 * num2)
			{
				return true;
			}
		}
		bool flag = PawnUtility.ShouldCollideWithPawns(pawn);
		bool flag2 = curPath.NodesLeftCount < 15;
		PathingContext pc = pawn.Map.pathing.For(pawn);
		bool canBashFences = pawn.CurJob != null && pawn.CurJob.canBashFences;
		IntVec3 other = IntVec3.Invalid;
		for (int i = 0; i < 20 && i < curPath.NodesLeftCount; i++)
		{
			IntVec3 intVec = curPath.Peek(i);
			if (!intVec.WalkableBy(pawn.Map, pawn))
			{
				return true;
			}
			if (flag && !BestPathHadPawnsInTheWayRecently() && (PawnUtility.AnyPawnBlockingPathAt(intVec, pawn, actAsIfHadCollideWithPawnsJob: false, collideOnlyWithStandingPawns: true) || (flag2 && PawnUtility.AnyPawnBlockingPathAt(intVec, pawn))))
			{
				return true;
			}
			if (!BestPathHadDangerRecently() && PawnUtility.KnownDangerAt(intVec, pawn.Map, pawn))
			{
				return true;
			}
			if (intVec.GetEdifice(pawn.Map) is Building_Door building_Door)
			{
				if (!building_Door.CanPhysicallyPass(pawn) && !pawn.HostileTo(building_Door))
				{
					return true;
				}
				if (building_Door.IsForbiddenToPass(pawn))
				{
					return true;
				}
			}
			if (i != 0 && intVec.AdjacentToDiagonal(other) && (PathFinder.BlocksDiagonalMovement(intVec.x, other.z, pc, canBashFences) || PathFinder.BlocksDiagonalMovement(other.x, intVec.z, pc, canBashFences)))
			{
				return true;
			}
			other = intVec;
		}
		return false;
	}

	private bool BestPathHadPawnsInTheWayRecently()
	{
		return foundPathWhichCollidesWithPawns + 240 > Find.TickManager.TicksGame;
	}

	private bool BestPathHadDangerRecently()
	{
		return foundPathWithDanger + 240 > Find.TickManager.TicksGame;
	}

	private bool FailedToFindCloseUnoccupiedCellRecently()
	{
		return failedToFindCloseUnoccupiedCellTicks + 100 > Find.TickManager.TicksGame;
	}
}
