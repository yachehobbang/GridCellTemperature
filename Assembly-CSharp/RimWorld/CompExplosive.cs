using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class CompExplosive : ThingComp
{
	public bool wickStarted;

	public int wickTicksLeft;

	private Thing instigator;

	private int countdownTicksLeft = -1;

	public bool destroyedThroughDetonation;

	private List<Thing> thingsIgnoredByExplosion;

	public float? customExplosiveRadius;

	protected Sustainer wickSoundSustainer;

	private OverlayHandle? overlayBurningWick;

	public CompProperties_Explosive Props => (CompProperties_Explosive)props;

	protected int StartWickThreshold => Mathf.RoundToInt(Props.startWickHitPointsPercent * (float)parent.MaxHitPoints);

	protected virtual bool CanEverExplodeFromDamage
	{
		get
		{
			if (Props.chanceNeverExplodeFromDamage < 1E-05f)
			{
				return true;
			}
			Rand.PushState();
			Rand.Seed = parent.thingIDNumber.GetHashCode();
			bool result = Rand.Value > Props.chanceNeverExplodeFromDamage;
			Rand.PopState();
			return result;
		}
	}

	public void AddThingsIgnoredByExplosion(List<Thing> things)
	{
		if (thingsIgnoredByExplosion == null)
		{
			thingsIgnoredByExplosion = new List<Thing>();
		}
		thingsIgnoredByExplosion.AddRange(things);
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_References.Look(ref instigator, "instigator");
		Scribe_Collections.Look(ref thingsIgnoredByExplosion, "thingsIgnoredByExplosion", LookMode.Reference);
		Scribe_Values.Look(ref wickStarted, "wickStarted", defaultValue: false);
		Scribe_Values.Look(ref wickTicksLeft, "wickTicksLeft", 0);
		Scribe_Values.Look(ref destroyedThroughDetonation, "destroyedThroughDetonation", defaultValue: false);
		Scribe_Values.Look(ref countdownTicksLeft, "countdownTicksLeft", 0);
		Scribe_Values.Look(ref customExplosiveRadius, "explosiveRadius", null);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (Props.countdownTicks.HasValue)
		{
			countdownTicksLeft = Props.countdownTicks.Value.RandomInRange;
		}
		UpdateOverlays();
	}

	public override void CompTick()
	{
		if (countdownTicksLeft > 0)
		{
			countdownTicksLeft--;
			if (countdownTicksLeft == 0)
			{
				StartWick();
				countdownTicksLeft = -1;
			}
		}
		if (!wickStarted)
		{
			return;
		}
		if (wickSoundSustainer == null)
		{
			StartWickSustainer();
		}
		else
		{
			wickSoundSustainer.Maintain();
		}
		if (Props.wickMessages != null)
		{
			foreach (WickMessage wickMessage in Props.wickMessages)
			{
				if (wickMessage.ticksLeft == wickTicksLeft && wickMessage.wickMessagekey != null)
				{
					Messages.Message(wickMessage.wickMessagekey.Translate(parent.GetCustomLabelNoCount(includeHp: false), wickTicksLeft.ToStringSecondsFromTicks()), parent, wickMessage.messageType ?? MessageTypeDefOf.NeutralEvent, historical: false);
				}
			}
		}
		wickTicksLeft--;
		if (wickTicksLeft <= 0)
		{
			Detonate(parent.MapHeld);
		}
	}

	private void StartWickSustainer()
	{
		SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(parent.PositionHeld, parent.MapHeld));
		SoundInfo info = SoundInfo.InMap(parent, MaintenanceType.PerTick);
		wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
	}

	private void EndWickSustainer()
	{
		if (wickSoundSustainer != null)
		{
			wickSoundSustainer.End();
			wickSoundSustainer = null;
		}
	}

	private void UpdateOverlays()
	{
		if (parent.Spawned && Props.drawWick)
		{
			parent.Map.overlayDrawer.Disable(parent, ref overlayBurningWick);
			if (wickStarted)
			{
				overlayBurningWick = parent.Map.overlayDrawer.Enable(parent, OverlayTypes.BurningWick);
			}
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		if ((mode == DestroyMode.KillFinalize && Props.explodeOnKilled) || Props.explodeOnDestroyed)
		{
			Detonate(previousMap, ignoreUnspawned: true);
		}
	}

	public override void PostDeSpawn(Map map)
	{
		base.PostDeSpawn(map);
		StopWick();
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		absorbed = false;
		if (!CanEverExplodeFromDamage)
		{
			return;
		}
		if (dinfo.Def.ExternalViolenceFor(parent) && dinfo.Amount >= (float)parent.HitPoints && CanExplodeFromDamageType(dinfo.Def))
		{
			if (parent.MapHeld != null)
			{
				instigator = dinfo.Instigator;
				Detonate(parent.MapHeld);
				if (parent.Destroyed)
				{
					absorbed = true;
				}
			}
		}
		else if (!wickStarted && Props.startWickOnDamageTaken != null && Props.startWickOnDamageTaken.Contains(dinfo.Def))
		{
			StartWick(dinfo.Instigator);
		}
	}

	public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		if (CanEverExplodeFromDamage && CanExplodeFromDamageType(dinfo.Def) && !parent.Destroyed)
		{
			if (wickStarted && dinfo.Def == DamageDefOf.Stun)
			{
				StopWick();
			}
			else if (!wickStarted && parent.HitPoints <= StartWickThreshold && (dinfo.Def.ExternalViolenceFor(parent) || (!Props.startWickOnInternalDamageTaken.NullOrEmpty() && Props.startWickOnInternalDamageTaken.Contains(dinfo.Def))))
			{
				StartWick(dinfo.Instigator);
			}
		}
	}

	public void StartWick(Thing instigator = null)
	{
		if (!wickStarted && !(ExplosiveRadius() <= 0f))
		{
			this.instigator = instigator;
			wickStarted = true;
			wickTicksLeft = Props.wickTicks.RandomInRange;
			StartWickSustainer();
			GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(parent, Props.explosiveDamageType, null, instigator);
			UpdateOverlays();
		}
	}

	public void StopWick()
	{
		wickStarted = false;
		instigator = null;
		UpdateOverlays();
		EndWickSustainer();
	}

	public float ExplosiveRadius()
	{
		CompProperties_Explosive compProperties_Explosive = Props;
		float num = customExplosiveRadius ?? Props.explosiveRadius;
		if (parent.stackCount > 1 && compProperties_Explosive.explosiveExpandPerStackcount > 0f)
		{
			num += Mathf.Sqrt((float)(parent.stackCount - 1) * compProperties_Explosive.explosiveExpandPerStackcount);
		}
		if (compProperties_Explosive.explosiveExpandPerFuel > 0f && parent.GetComp<CompRefuelable>() != null)
		{
			num += Mathf.Sqrt(parent.GetComp<CompRefuelable>().Fuel * compProperties_Explosive.explosiveExpandPerFuel);
		}
		return num;
	}

	protected void Detonate(Map map, bool ignoreUnspawned = false)
	{
		if (!ignoreUnspawned && !parent.SpawnedOrAnyParentSpawned)
		{
			return;
		}
		CompProperties_Explosive compProperties_Explosive = Props;
		float num = ExplosiveRadius();
		if (num <= 0f)
		{
			return;
		}
		Thing thing = ((instigator == null || (instigator.HostileTo(parent.Faction) && parent.Faction != Faction.OfPlayer)) ? parent : instigator);
		if (compProperties_Explosive.explosiveExpandPerFuel > 0f && parent.GetComp<CompRefuelable>() != null)
		{
			parent.GetComp<CompRefuelable>().ConsumeFuel(parent.GetComp<CompRefuelable>().Fuel);
		}
		if (compProperties_Explosive.destroyThingOnExplosionSize <= num && !parent.Destroyed)
		{
			destroyedThroughDetonation = true;
			parent.Kill(null);
		}
		EndWickSustainer();
		wickStarted = false;
		if (map == null)
		{
			Log.Warning("Tried to detonate CompExplosive in a null map.");
			return;
		}
		if (compProperties_Explosive.explosionEffect != null)
		{
			Effecter effecter = compProperties_Explosive.explosionEffect.Spawn();
			effecter.Trigger(new TargetInfo(parent.PositionHeld, map), new TargetInfo(parent.PositionHeld, map));
			effecter.Cleanup();
		}
		GenExplosion.DoExplosion(parent.PositionHeld, map, num, compProperties_Explosive.explosiveDamageType, thing, compProperties_Explosive.damageAmountBase, compProperties_Explosive.armorPenetrationBase, compProperties_Explosive.explosionSound, null, null, null, compProperties_Explosive.postExplosionSpawnThingDef, compProperties_Explosive.postExplosionSpawnChance, compProperties_Explosive.postExplosionSpawnThingCount, Props.postExplosionGasType, compProperties_Explosive.applyDamageToExplosionCellsNeighbors, compProperties_Explosive.preExplosionSpawnThingDef, compProperties_Explosive.preExplosionSpawnChance, compProperties_Explosive.preExplosionSpawnThingCount, compProperties_Explosive.chanceToStartFire, compProperties_Explosive.damageFalloff, null, thingsIgnoredByExplosion, null, compProperties_Explosive.doVisualEffects, doSoundEffects: compProperties_Explosive.doSoundEffects, propagationSpeed: compProperties_Explosive.propagationSpeed);
	}

	private bool CanExplodeFromDamageType(DamageDef damage)
	{
		if (Props.requiredDamageTypeToExplode != null)
		{
			return Props.requiredDamageTypeToExplode == damage;
		}
		return true;
	}

	public override string CompInspectStringExtra()
	{
		string text = "";
		if (countdownTicksLeft != -1)
		{
			text += "DetonationCountdown".Translate(countdownTicksLeft.TicksToDays().ToString("0.0"));
		}
		if (Props.extraInspectStringKey != null)
		{
			text += ((text != "") ? "\n" : "") + Props.extraInspectStringKey.Translate();
		}
		return text;
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (countdownTicksLeft > 0)
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "DEV: Trigger countdown";
			command_Action.action = delegate
			{
				countdownTicksLeft = 1;
			};
			yield return command_Action;
		}
	}
}
