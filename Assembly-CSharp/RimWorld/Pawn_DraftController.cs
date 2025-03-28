using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public class Pawn_DraftController : IExposable
{
	public Pawn pawn;

	private bool draftedInt;

	private bool fireAtWillInt = true;

	private AutoUndrafter autoUndrafter;

	public bool Drafted
	{
		get
		{
			return draftedInt;
		}
		set
		{
			if (value == draftedInt)
			{
				return;
			}
			pawn.mindState.priorityWork.ClearPrioritizedWorkAndJobQueue();
			fireAtWillInt = true;
			draftedInt = value;
			if (!value && pawn.Spawned)
			{
				pawn.Map.pawnDestinationReservationManager.ReleaseAllClaimedBy(pawn);
			}
			pawn.jobs.ClearQueuedJobs();
			if (pawn.jobs.curJob != null && pawn.jobs.IsCurrentJobPlayerInterruptible())
			{
				if (pawn.jobs.curDriver is JobDriver_Ingest { EatingFromInventory: not false })
				{
					pawn.inventory.innerContainer.TryAddRangeOrTransfer(pawn.carryTracker.innerContainer);
				}
				ChildcareUtility.BreastfeedFailReason? reason;
				if (!value)
				{
					if (pawn.carryTracker.CarriedThing is Pawn baby && ChildcareUtility.CanSuckle(baby, out reason))
					{
						Job newJob = ChildcareUtility.MakeBringBabyToSafetyJob(pawn, baby);
						pawn.jobs.StartJob(newJob, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, true);
					}
					else if (pawn.IsCarrying())
					{
						pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
						pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
					}
					else
					{
						pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
					}
				}
				else
				{
					if (pawn.carryTracker.CarriedThing is Pawn baby2 && ChildcareUtility.CanSuckle(baby2, out reason))
					{
						Messages.Message("MessageDraftedPawnCarryingBaby".Translate(pawn), pawn, MessageTypeDefOf.NeutralEvent);
					}
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
				}
			}
			if (draftedInt)
			{
				Lord lord = pawn.GetLord();
				if (lord != null && lord.LordJob is LordJob_VoluntarilyJoinable)
				{
					lord.Notify_PawnLost(pawn, PawnLostCondition.Drafted, null);
				}
				autoUndrafter.Notify_Drafted();
				pawn.jobs?.SetFormingCaravanTick(clear: true);
				pawn.inventory.DropAllPackingCaravanThings();
				pawn.TryGetComp<CompCanBeDormant>()?.WakeUp();
			}
			else
			{
				if (pawn.playerSettings != null)
				{
					pawn.playerSettings.animalsReleased = false;
				}
				if (pawn.IsFormingCaravan())
				{
					pawn.jobs?.SetFormingCaravanTick();
				}
			}
			foreach (Pawn item in PawnUtility.SpawnedMasteredPawns(pawn))
			{
				item.jobs.Notify_MasterDraftedOrUndrafted();
			}
		}
	}

	public bool FireAtWill
	{
		get
		{
			return fireAtWillInt;
		}
		set
		{
			fireAtWillInt = value;
			if (!fireAtWillInt && pawn.stances.curStance is Stance_Warmup)
			{
				pawn.stances.CancelBusyStanceSoft();
			}
		}
	}

	public bool ShowDraftGizmo
	{
		get
		{
			if (ModsConfig.BiotechActive && pawn.IsColonyMech && pawn.GetMechControlGroup() == null)
			{
				return false;
			}
			if (ModsConfig.AnomalyActive && pawn.IsMutant && !pawn.IsColonyMutantPlayerControlled)
			{
				return false;
			}
			return true;
		}
	}

	public Pawn_DraftController(Pawn pawn)
	{
		this.pawn = pawn;
		autoUndrafter = new AutoUndrafter(pawn);
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref draftedInt, "drafted", defaultValue: false);
		Scribe_Values.Look(ref fireAtWillInt, "fireAtWill", defaultValue: true);
		Scribe_Deep.Look(ref autoUndrafter, "autoUndrafter", pawn);
	}

	public void DraftControllerTick()
	{
		autoUndrafter.AutoUndraftTick();
	}

	internal IEnumerable<Gizmo> GetGizmos()
	{
		if (ShowDraftGizmo)
		{
			Command_Toggle command_Toggle = new Command_Toggle();
			command_Toggle.hotKey = KeyBindingDefOf.Command_ColonistDraft;
			command_Toggle.isActive = () => Drafted;
			command_Toggle.toggleAction = delegate
			{
				Drafted = !Drafted;
				PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Drafting, KnowledgeAmount.SpecificInteraction);
				if (Drafted)
				{
					LessonAutoActivator.TeachOpportunity(ConceptDefOf.QueueOrders, OpportunityType.GoodToKnow);
				}
			};
			command_Toggle.defaultDesc = "CommandToggleDraftDesc".Translate();
			command_Toggle.icon = TexCommand.Draft;
			command_Toggle.turnOnSound = SoundDefOf.DraftOn;
			command_Toggle.turnOffSound = SoundDefOf.DraftOff;
			command_Toggle.groupKeyIgnoreContent = 81729172;
			command_Toggle.defaultLabel = (Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate();
			if (pawn.Downed)
			{
				command_Toggle.Disable("IsIncapped".Translate(pawn.LabelShort, pawn));
			}
			if (pawn.Deathresting)
			{
				command_Toggle.Disable("IsDeathresting".Translate(pawn.Named("PAWN")));
			}
			if (ModsConfig.BiotechActive && pawn.IsColonyMech)
			{
				AcceptanceReport acceptanceReport = MechanitorUtility.CanDraftMech(pawn);
				if (!acceptanceReport)
				{
					command_Toggle.Disable(acceptanceReport.Reason);
				}
			}
			if (!Drafted)
			{
				command_Toggle.tutorTag = "Draft";
			}
			else
			{
				command_Toggle.tutorTag = "Undraft";
			}
			yield return command_Toggle;
		}
		if (Drafted && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsRangedWeapon)
		{
			Command_Toggle command_Toggle2 = new Command_Toggle();
			command_Toggle2.hotKey = KeyBindingDefOf.Misc6;
			command_Toggle2.isActive = () => FireAtWill;
			command_Toggle2.toggleAction = delegate
			{
				FireAtWill = !FireAtWill;
			};
			command_Toggle2.icon = TexCommand.FireAtWill;
			command_Toggle2.defaultLabel = "CommandFireAtWillLabel".Translate();
			command_Toggle2.defaultDesc = "CommandFireAtWillDesc".Translate();
			command_Toggle2.tutorTag = "FireAtWillToggle";
			yield return command_Toggle2;
		}
	}

	internal void Notify_PrimaryWeaponChanged()
	{
		fireAtWillInt = true;
	}
}
