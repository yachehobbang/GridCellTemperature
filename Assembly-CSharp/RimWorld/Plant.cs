using System.Collections.Generic;
using System.Text;
using LudeonTK;
using UnityEngine;
using Verse;

namespace RimWorld;

[StaticConstructorOnStartup]
public class Plant : ThingWithComps
{
	public enum LeaflessCause
	{
		Cold,
		Poison,
		Pollution,
		NoPollution
	}

	protected float growthInt = 0.15f;

	protected int ageInt;

	protected int unlitTicks;

	protected int madeLeaflessTick = -99999;

	public bool sown;

	private string cachedLabelMouseover;

	private static Color32[] workingColors = new Color32[4];

	public const float BaseGrowthPercent = 0.15f;

	public const float BaseSownGrowthPercent = 0.0001f;

	public const float MinGrowthForAnimalIngestion = 0.1f;

	private const float BaseDyingDamagePerTick = 0.005f;

	private static readonly FloatRange DyingDamagePerTickBecauseExposedToLight = new FloatRange(0.0001f, 0.001f);

	private const float GridPosRandomnessFactor = 0.3f;

	private const int TicksWithoutLightBeforeStartDying = 450000;

	private const int LeaflessMinRecoveryTicks = 60000;

	public const float MinGrowthTemperature = 0f;

	public const float MinOptimalGrowthTemperature = 6f;

	public const float MaxOptimalGrowthTemperature = 42f;

	public const float MaxGrowthTemperature = 58f;

	private const float MinLeaflessTemperature = -18f;

	public const float MaxLeaflessTemperature = -10f;

	public const float TopVerticesAltitudeBias = 0.1f;

	private static Graphic GraphicSowing = GraphicDatabase.Get<Graphic_Single>("Things/Plant/Plant_Sowing", ShaderDatabase.Cutout, Vector2.one, Color.white);

	private static readonly FloatRange PollutionDamagePerTickRange = new FloatRange(1.6666667E-05f, 0.00016666666f);

	private static readonly Texture2D CutAllBlightTex = ContentFinder<Texture2D>.Get("UI/Commands/CutAllBlightedPlants");

	[TweakValue("Graphics", -1f, 1f)]
	private static float LeafSpawnRadius = 0.4f;

	[TweakValue("Graphics", 0f, 2f)]
	private static float LeafSpawnYMin = 0.3f;

	[TweakValue("Graphics", 0f, 2f)]
	private static float LeafSpawnYMax = 1f;

	public virtual float Growth
	{
		get
		{
			return growthInt;
		}
		set
		{
			growthInt = Mathf.Clamp01(value);
			cachedLabelMouseover = null;
		}
	}

	public virtual int Age
	{
		get
		{
			return ageInt;
		}
		set
		{
			ageInt = value;
			cachedLabelMouseover = null;
		}
	}

	public virtual bool HarvestableNow
	{
		get
		{
			if (def.plant.Harvestable)
			{
				return growthInt > def.plant.harvestMinGrowth;
			}
			return false;
		}
	}

	public bool HarvestableSoon
	{
		get
		{
			if (HarvestableNow)
			{
				return true;
			}
			if (!def.plant.Harvestable)
			{
				return false;
			}
			float num = Mathf.Max(1f - Growth, 0f) * def.plant.growDays;
			float num2 = Mathf.Max(1f - def.plant.harvestMinGrowth, 0f) * def.plant.growDays;
			if ((num <= 10f || num2 <= 1f) && GrowthRateFactor_Fertility > 0f)
			{
				return GrowthRateFactor_Temperature > 0f;
			}
			return false;
		}
	}

	public virtual bool BlightableNow
	{
		get
		{
			if (!Blighted && def.plant.Blightable && sown && LifeStage != 0)
			{
				return !base.Map.Biome.AllWildPlants.Contains(def);
			}
			return false;
		}
	}

	public Blight Blight
	{
		get
		{
			if (!base.Spawned || !def.plant.Blightable)
			{
				return null;
			}
			return base.Position.GetFirstBlight(base.Map);
		}
	}

	public bool Blighted => Blight != null;

	public override bool IngestibleNow
	{
		get
		{
			if (!base.IngestibleNow)
			{
				return false;
			}
			if (def.plant.IsTree)
			{
				return true;
			}
			if (growthInt < def.plant.harvestMinGrowth)
			{
				return false;
			}
			if (growthInt < 0.1f)
			{
				return false;
			}
			if (LeaflessNow)
			{
				return false;
			}
			if (base.Spawned && base.Position.GetSnowDepth(base.Map) > def.hideAtSnowDepth)
			{
				return false;
			}
			return true;
		}
	}

	public virtual float CurrentDyingDamagePerTick
	{
		get
		{
			if (!base.Spawned)
			{
				return 0f;
			}
			float num = 0f;
			if (def.plant.LimitedLifespan && ageInt > def.plant.LifespanTicks)
			{
				num = Mathf.Max(num, 0.005f);
			}
			if (!def.plant.cavePlant && def.plant.dieIfNoSunlight && unlitTicks > 450000)
			{
				num = Mathf.Max(num, 0.005f);
			}
			if (DyingBecauseExposedToLight)
			{
				float lerpPct = base.Map.glowGrid.GroundGlowAt(base.Position, ignoreCavePlants: true);
				num = Mathf.Max(num, DyingDamagePerTickBecauseExposedToLight.LerpThroughRange(lerpPct));
			}
			if (DyingFromPollution || DyingFromNoPollution)
			{
				num = Mathf.Max(num, PollutionDamagePerTickRange.RandomInRangeSeeded(base.Position.GetHashCode()));
			}
			return num;
		}
	}

	public virtual bool DyingBecauseExposedToLight
	{
		get
		{
			if (def.plant.cavePlant && base.Spawned)
			{
				return base.Map.glowGrid.GroundGlowAt(base.Position, ignoreCavePlants: true) > 0f;
			}
			return false;
		}
	}

	public bool Dying => CurrentDyingDamagePerTick > 0f;

	protected virtual bool Resting
	{
		get
		{
			if (!(GenLocalDate.DayPercent(this) < 0.25f))
			{
				return GenLocalDate.DayPercent(this) > 0.8f;
			}
			return true;
		}
	}

	public virtual float GrowthRate
	{
		get
		{
			if (Blighted)
			{
				return 0f;
			}
			if (base.Spawned && !PlantUtility.GrowthSeasonNow(base.Position, base.Map))
			{
				return 0f;
			}
			return GrowthRateFactor_Fertility * GrowthRateFactor_Temperature * GrowthRateFactor_Light * GrowthRateFactor_NoxiousHaze;
		}
	}

	public virtual string GrowthRateCalcDesc
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (GrowthRateFactor_Fertility != 1f)
			{
				stringBuilder.AppendInNewLine("StatsReport_MultiplierFor".Translate("FertilityLower".Translate()) + ": " + GrowthRateFactor_Fertility.ToStringPercent());
			}
			if (GrowthRateFactor_Temperature != 1f)
			{
				stringBuilder.AppendInNewLine("StatsReport_MultiplierFor".Translate("TemperatureLower".Translate()) + ": " + GrowthRateFactor_Temperature.ToStringPercent());
			}
			if (GrowthRateFactor_Light != 1f)
			{
				stringBuilder.AppendInNewLine("StatsReport_MultiplierFor".Translate("LightLower".Translate()) + ": " + GrowthRateFactor_Light.ToStringPercent());
			}
			if (ModsConfig.BiotechActive && base.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.NoxiousHaze) && GrowthRateFactor_NoxiousHaze != 1f)
			{
				stringBuilder.AppendInNewLine("StatsReport_MultiplierFor".Translate(GameConditionDefOf.NoxiousHaze.label) + ": " + GrowthRateFactor_NoxiousHaze.ToStringPercent());
			}
			return stringBuilder.ToString();
		}
	}

	protected float GrowthPerTick
	{
		get
		{
			if (LifeStage != PlantLifeStage.Growing || Resting)
			{
				return 0f;
			}
			return 1f / (60000f * def.plant.growDays) * GrowthRate;
		}
	}

	public float GrowthRateFactor_Fertility => PlantUtility.GrowthRateFactorFor_Fertility(def, base.Map.fertilityGrid.FertilityAt(base.Position));

	public float GrowthRateFactor_Light
	{
		get
		{
			float glow = base.Map.glowGrid.GroundGlowAt(base.Position);
			return PlantUtility.GrowthRateFactorFor_Light(def, glow);
		}
	}

	public float GrowthRateFactor_Temperature
	{
		get
		{
			if (!GenTemperature.TryGetTemperatureForCell(base.Position, base.Map, out var tempResult))
			{
				return 1f;
			}
			return PlantUtility.GrowthRateFactorFor_Temperature(tempResult);
		}
	}

	public float GrowthRateFactor_NoxiousHaze
	{
		get
		{
			if (NoxiousHazeUtility.IsExposedToNoxiousHaze(this))
			{
				return 0.5f;
			}
			return 1f;
		}
	}

	protected int TicksUntilFullyGrown
	{
		get
		{
			if (growthInt > 0.9999f)
			{
				return 0;
			}
			float growthPerTick = GrowthPerTick;
			if (growthPerTick == 0f)
			{
				return int.MaxValue;
			}
			return (int)((1f - growthInt) / growthPerTick);
		}
	}

	protected string GrowthPercentString => (growthInt + 0.0001f).ToStringPercent();

	public override string LabelMouseover
	{
		get
		{
			if (cachedLabelMouseover == null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(def.LabelCap);
				stringBuilder.Append(" (" + "PercentGrowth".Translate(GrowthPercentString));
				if (Dying)
				{
					stringBuilder.Append(", " + "DyingLower".Translate());
				}
				stringBuilder.Append(")");
				cachedLabelMouseover = stringBuilder.ToString();
			}
			return cachedLabelMouseover;
		}
	}

	protected virtual bool HasEnoughLightToGrow => GrowthRateFactor_Light > 0.001f;

	public virtual PlantLifeStage LifeStage
	{
		get
		{
			if (growthInt < 0.0001f)
			{
				return PlantLifeStage.Sowing;
			}
			if (growthInt > 0.999f)
			{
				return PlantLifeStage.Mature;
			}
			return PlantLifeStage.Growing;
		}
	}

	public override Graphic Graphic
	{
		get
		{
			if (LifeStage == PlantLifeStage.Sowing)
			{
				return GraphicSowing;
			}
			if (def.plant.pollutedGraphic != null && base.PositionHeld.IsPolluted(base.MapHeld))
			{
				return def.plant.pollutedGraphic;
			}
			if (def.plant.leaflessGraphic != null && LeaflessNow && (!sown || !HarvestableNow))
			{
				return def.plant.leaflessGraphic;
			}
			if (def.plant.immatureGraphic != null && !HarvestableNow)
			{
				return def.plant.immatureGraphic;
			}
			return base.Graphic;
		}
	}

	public bool LeaflessNow
	{
		get
		{
			if (Find.TickManager.TicksGame - madeLeaflessTick < 60000)
			{
				return true;
			}
			return false;
		}
	}

	protected virtual float LeaflessTemperatureThresh => Rand.RangeSeeded(-18f, -10f, thingIDNumber ^ 0x31F3A5C1);

	public bool IsCrop
	{
		get
		{
			if (!def.plant.Sowable)
			{
				return false;
			}
			if (!base.Spawned)
			{
				Log.Warning("Can't determine if crop when unspawned.");
				return false;
			}
			return def == WorkGiver_Grower.CalculateWantedPlantDef(base.Position, base.Map);
		}
	}

	public bool DyingFromPollution
	{
		get
		{
			if (def.plant.RequiresNoPollution)
			{
				return base.Position.IsPolluted(base.Map);
			}
			return false;
		}
	}

	public bool DyingFromNoPollution
	{
		get
		{
			if (def.plant.RequiresPollution)
			{
				return !base.Position.IsPolluted(base.Map);
			}
			return false;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (Current.ProgramState == ProgramState.Playing && !respawningAfterLoad)
		{
			CheckMakeLeafless();
		}
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		Blight firstBlight = base.Position.GetFirstBlight(base.Map);
		base.DeSpawn(mode);
		firstBlight?.Notify_PlantDeSpawned();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref growthInt, "growth", 0f);
		Scribe_Values.Look(ref ageInt, "age", 0);
		Scribe_Values.Look(ref unlitTicks, "unlitTicks", 0);
		Scribe_Values.Look(ref madeLeaflessTick, "madeLeaflessTick", -99999);
		Scribe_Values.Look(ref sown, "sown", defaultValue: false);
	}

	public override void PostMapInit()
	{
		CheckMakeLeafless();
	}

	protected override void IngestedCalculateAmounts(Pawn ingester, float nutritionWanted, out int numTaken, out float nutritionIngested)
	{
		float statValue = this.GetStatValue(StatDefOf.Nutrition);
		float num = growthInt * statValue;
		nutritionIngested = Mathf.Min(nutritionWanted, num);
		if (nutritionIngested >= num)
		{
			numTaken = 1;
			return;
		}
		numTaken = 0;
		growthInt -= nutritionIngested / statValue;
		if (base.Spawned)
		{
			base.Map.mapDrawer.MapMeshDirty(base.Position, MapMeshFlagDefOf.Things);
		}
	}

	public virtual void PlantCollected(Pawn by, PlantDestructionMode plantDestructionMode)
	{
		if (def.plant.HarvestDestroys)
		{
			if (def.plant.IsTree && def.plant.treeLoversCareIfChopped)
			{
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.CutTree, by.Named(HistoryEventArgsNames.Doer)));
				base.Map.treeDestructionTracker.Notify_TreeCut(by);
			}
			if (Blighted && plantDestructionMode == PlantDestructionMode.Cut)
			{
				base.Map.floodFiller.FloodFill(base.Position, delegate(IntVec3 cell)
				{
					if (base.Position == cell)
					{
						return true;
					}
					Plant plant = cell.GetPlant(base.Map);
					return plant != null && !base.Map.designationManager.HasMapDesignationOn(plant) && plant.Blighted;
				}, delegate(IntVec3 cell)
				{
					if (!(base.Position == cell))
					{
						Plant plant2 = cell.GetPlant(base.Map);
						base.Map.designationManager.AddDesignation(new Designation(plant2, DesignationDefOf.CutPlant));
						by.jobs?.curJob?.targetQueueA?.Add(plant2);
					}
				}, 100);
			}
			Thing thing = TrySpawnStump(plantDestructionMode);
			Map map = base.Map;
			Destroy(DestroyMode.KillFinalizeLeavingsOnly);
			if (thing != null && plantDestructionMode == PlantDestructionMode.Cut && by.Faction == Faction.OfPlayer)
			{
				map.designationManager.AddDesignation(new Designation(thing, DesignationDefOf.CutPlant));
				by.jobs?.curJob?.targetQueueA?.Add(thing);
			}
		}
		else
		{
			growthInt = def.plant.harvestAfterGrowth;
			base.Map.mapDrawer.MapMeshDirty(base.Position, MapMeshFlagDefOf.Things);
		}
	}

	public Thing TrySpawnStump(PlantDestructionMode treeDestructionMode)
	{
		if (!base.Spawned || LifeStage == PlantLifeStage.Sowing)
		{
			return null;
		}
		if (!HarvestableNow)
		{
			return null;
		}
		ThingDef thingDef = null;
		switch (treeDestructionMode)
		{
		case PlantDestructionMode.Smash:
			thingDef = def.plant.smashedThingDef;
			break;
		case PlantDestructionMode.Flame:
			thingDef = def.plant.burnedThingDef;
			break;
		case PlantDestructionMode.Chop:
		case PlantDestructionMode.Cut:
			thingDef = def.plant.choppedThingDef;
			break;
		}
		if (thingDef != null)
		{
			Thing thing = GenSpawn.Spawn(thingDef, base.Position, base.Map);
			if (thing is DeadPlant deadPlant)
			{
				deadPlant.Growth = Growth;
			}
			if (Find.Selector.IsSelected(this))
			{
				Find.Selector.Select(thing, playSound: false, forceDesignatorDeselect: false);
			}
			return thing;
		}
		return null;
	}

	public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
	{
		if (base.Spawned && dinfo.HasValue)
		{
			if (dinfo.Value.Def == DamageDefOf.Flame)
			{
				TrySpawnStump(PlantDestructionMode.Flame);
			}
			else
			{
				TrySpawnStump(PlantDestructionMode.Smash);
			}
			if (def.plant.IsTree && def.plant.treeLoversCareIfChopped)
			{
				base.Map.treeDestructionTracker.Notify_TreeDestroyed(dinfo.Value);
			}
		}
		base.Kill((DamageInfo?)null, (Hediff)null);
	}

	protected virtual void CheckMakeLeafless()
	{
		if (DyingFromPollution)
		{
			MakeLeafless(LeaflessCause.Pollution);
		}
		else if (DyingFromNoPollution)
		{
			MakeLeafless(LeaflessCause.NoPollution);
		}
		else if (base.AmbientTemperature < LeaflessTemperatureThresh)
		{
			MakeLeafless(LeaflessCause.Cold);
		}
	}

	public virtual void MakeLeafless(LeaflessCause cause, bool sendMessage = true)
	{
		bool num = !LeaflessNow;
		Map map = base.Map;
		if (cause == LeaflessCause.Poison && def.plant.leaflessGraphic == null)
		{
			if (IsCrop && sendMessage && MessagesRepeatAvoider.MessageShowAllowed("MessagePlantDiedOfPoison-" + def.defName, 240f))
			{
				Messages.Message("MessagePlantDiedOfPoison".Translate(GetCustomLabelNoCount(includeHp: false)), new TargetInfo(base.Position, map), MessageTypeDefOf.NegativeEvent);
			}
			TakeDamage(new DamageInfo(DamageDefOf.Rotting, 99999f));
		}
		else if (def.plant.dieIfLeafless)
		{
			if (IsCrop)
			{
				switch (cause)
				{
				case LeaflessCause.Cold:
					if (sendMessage && MessagesRepeatAvoider.MessageShowAllowed("MessagePlantDiedOfCold-" + def.defName, 240f))
					{
						Messages.Message("MessagePlantDiedOfCold".Translate(GetCustomLabelNoCount(includeHp: false)), new TargetInfo(base.Position, map), MessageTypeDefOf.NegativeEvent);
					}
					break;
				case LeaflessCause.Poison:
					if (sendMessage && MessagesRepeatAvoider.MessageShowAllowed("MessagePlantDiedOfPoison-" + def.defName, 240f))
					{
						Messages.Message("MessagePlantDiedOfPoison".Translate(GetCustomLabelNoCount(includeHp: false)), new TargetInfo(base.Position, map), MessageTypeDefOf.NegativeEvent);
					}
					break;
				case LeaflessCause.Pollution:
					if (sendMessage && MessagesRepeatAvoider.MessageShowAllowed("MessagePlantDiedOfPollution-" + def.defName, 240f))
					{
						Messages.Message("MessagePlantDiedOfPollution".Translate(GetCustomLabelNoCount(includeHp: false)), new TargetInfo(base.Position, map), MessageTypeDefOf.NegativeEvent);
					}
					break;
				case LeaflessCause.NoPollution:
					if (sendMessage && MessagesRepeatAvoider.MessageShowAllowed("MessagePlantDiedOfNoPollution-" + def.defName, 240f))
					{
						Messages.Message("MessagePlantDiedOfNoPollution".Translate(GetCustomLabelNoCount(includeHp: false)), new TargetInfo(base.Position, map), MessageTypeDefOf.NegativeEvent);
					}
					break;
				}
			}
			TakeDamage(new DamageInfo(DamageDefOf.Rotting, 99999f));
		}
		else
		{
			madeLeaflessTick = Find.TickManager.TicksGame;
		}
		if (num)
		{
			map.mapDrawer.MapMeshDirty(base.Position, MapMeshFlagDefOf.Things);
		}
	}

	public override void Tick()
	{
		base.Tick();
		if (this.IsHashIntervalTick(2000))
		{
			TickLong();
		}
	}

	public override void TickLong()
	{
		CheckMakeLeafless();
		if (base.Destroyed)
		{
			return;
		}
		base.TickLong();
		if (PlantUtility.GrowthSeasonNow(base.Position, base.Map))
		{
			float num = growthInt;
			bool num2 = LifeStage == PlantLifeStage.Mature;
			growthInt += GrowthPerTick * 2000f;
			if (growthInt > 1f)
			{
				growthInt = 1f;
			}
			if (((!num2 && LifeStage == PlantLifeStage.Mature) || (int)(num * 10f) != (int)(growthInt * 10f)) && CurrentlyCultivated())
			{
				base.Map.mapDrawer.MapMeshDirty(base.Position, MapMeshFlagDefOf.Things);
			}
		}
		if (!HasEnoughLightToGrow)
		{
			unlitTicks += 2000;
		}
		else
		{
			unlitTicks = 0;
		}
		ageInt += 2000;
		if (Dying)
		{
			Map map = base.Map;
			bool isCrop = IsCrop;
			bool harvestableNow = HarvestableNow;
			bool dyingBecauseExposedToLight = DyingBecauseExposedToLight;
			int num3 = Mathf.CeilToInt(CurrentDyingDamagePerTick * 2000f);
			TakeDamage(new DamageInfo(DamageDefOf.Rotting, num3));
			if (base.Destroyed && !def.plant.skipDeteriorationMessage)
			{
				if (isCrop && def.plant.Harvestable && MessagesRepeatAvoider.MessageShowAllowed("MessagePlantDiedOfRot-" + def.defName, 240f))
				{
					string key = (harvestableNow ? ((!ModsConfig.BiotechActive || !base.Position.IsPolluted(map)) ? "MessagePlantDiedOfRot_LeftUnharvested" : "MessagePlantDiedOfRot_PollutedTerrain") : ((!dyingBecauseExposedToLight) ? "MessagePlantDiedOfRot" : "MessagePlantDiedOfRot_ExposedToLight"));
					Messages.Message(key.Translate(GetCustomLabelNoCount(includeHp: false)), new TargetInfo(base.Position, map), MessageTypeDefOf.NegativeEvent);
				}
				return;
			}
		}
		cachedLabelMouseover = null;
		if (def.plant.dropLeaves && MoteMaker.MakeStaticMote(Vector3.zero, base.Map, ThingDefOf.Mote_Leaf) is MoteLeaf moteLeaf)
		{
			float num4 = def.plant.visualSizeRange.LerpThroughRange(growthInt);
			float treeHeight = def.graphicData.drawSize.x * num4;
			Vector3 vector = Rand.InsideUnitCircleVec3 * LeafSpawnRadius;
			moteLeaf.Initialize(base.Position.ToVector3Shifted() + Vector3.up * Rand.Range(LeafSpawnYMin, LeafSpawnYMax) + vector + Vector3.forward * def.graphicData.shadowData.offset.z, Rand.Value * 2000.TicksToSeconds(), vector.z > 0f, treeHeight);
		}
	}

	protected virtual bool CurrentlyCultivated()
	{
		if (!def.plant.Sowable)
		{
			return false;
		}
		if (!base.Spawned)
		{
			return false;
		}
		Zone zone = base.Map.zoneManager.ZoneAt(base.Position);
		if (zone != null && zone is Zone_Growing)
		{
			return true;
		}
		Building edifice = base.Position.GetEdifice(base.Map);
		if (edifice != null && edifice.def.building.SupportsPlants)
		{
			return true;
		}
		return false;
	}

	public bool DeliberatelyCultivated()
	{
		if (!def.plant.Sowable)
		{
			return false;
		}
		if (!base.Spawned)
		{
			return false;
		}
		if (base.Map.zoneManager.ZoneAt(base.Position) is Zone_Growing zone_Growing && zone_Growing.GetPlantDefToGrow() == def)
		{
			return true;
		}
		Building edifice = base.Position.GetEdifice(base.Map);
		if (edifice != null && edifice.def.building.SupportsPlants)
		{
			return true;
		}
		return false;
	}

	public virtual bool CanYieldNow()
	{
		if (!HarvestableNow)
		{
			return false;
		}
		if (def.plant.harvestYield <= 0f)
		{
			return false;
		}
		if (Blighted)
		{
			return false;
		}
		return true;
	}

	public virtual int YieldNow()
	{
		if (!CanYieldNow())
		{
			return 0;
		}
		float harvestYield = def.plant.harvestYield;
		float num = Mathf.InverseLerp(def.plant.harvestMinGrowth, 1f, growthInt);
		num = 0.5f + num * 0.5f;
		harvestYield *= num;
		harvestYield *= Mathf.Lerp(0.5f, 1f, (float)HitPoints / (float)base.MaxHitPoints);
		if (def.plant.harvestYieldAffectedByDifficulty)
		{
			harvestYield *= Find.Storyteller.difficulty.cropYieldFactor;
		}
		return GenMath.RoundRandom(harvestYield);
	}

	public override void Print(SectionLayer layer)
	{
		Vector3 vector = this.TrueCenter();
		Rand.PushState();
		Rand.Seed = base.Position.GetHashCode();
		int num = Mathf.CeilToInt(growthInt * (float)def.plant.maxMeshCount);
		if (num < 1)
		{
			num = 1;
		}
		float num2 = def.plant.visualSizeRange.LerpThroughRange(growthInt);
		float num3 = def.graphicData.drawSize.x * num2;
		Vector3 zero = Vector3.zero;
		int num4 = 0;
		int[] positionIndices = PlantPosIndices.GetPositionIndices(this);
		bool flag = false;
		foreach (int num5 in positionIndices)
		{
			if (def.plant.maxMeshCount == 1)
			{
				zero = vector + Gen.RandomHorizontalVector(0.05f);
				float num6 = base.Position.z;
				if (zero.z - num2 / 2f < num6)
				{
					zero.z = num6 + num2 / 2f;
					flag = true;
				}
			}
			else
			{
				int num7 = 1;
				switch (def.plant.maxMeshCount)
				{
				case 1:
					num7 = 1;
					break;
				case 4:
					num7 = 2;
					break;
				case 9:
					num7 = 3;
					break;
				case 16:
					num7 = 4;
					break;
				case 25:
					num7 = 5;
					break;
				default:
					Log.Error(string.Concat(def, " must have plant.MaxMeshCount that is a perfect square."));
					break;
				}
				float num8 = 1f / (float)num7;
				zero = base.Position.ToVector3();
				zero.y = def.Altitude;
				zero.x += 0.5f * num8;
				zero.z += 0.5f * num8;
				int num9 = num5 / num7;
				int num10 = num5 % num7;
				zero.x += (float)num9 * num8;
				zero.z += (float)num10 * num8;
				float max = num8 * 0.3f;
				zero += Gen.RandomHorizontalVector(max);
			}
			bool @bool = Rand.Bool;
			Material material = Graphic.MatSingleFor(this);
			if (Graphic is Graphic_Random)
			{
				material = Graphic.MatSingle;
			}
			Graphic.TryGetTextureAtlasReplacementInfo(material, def.category.ToAtlasGroup(), @bool, vertexColors: false, out material, out var uvs, out var _);
			PlantUtility.SetWindExposureColors(workingColors, this);
			Printer_Plane.PrintPlane(size: new Vector2(num3, num3), layer: layer, center: zero, mat: material, rot: 0f, flipUv: @bool, uvs: uvs, colors: workingColors, topVerticesAltitudeBias: 0.1f, uvzPayload: this.HashOffset() % 1024);
			num4++;
			if (num4 >= num)
			{
				break;
			}
		}
		if (def.graphicData.shadowData != null)
		{
			Vector3 center = vector + def.graphicData.shadowData.offset * num2;
			if (flag)
			{
				center.z = base.Position.ToVector3Shifted().z + def.graphicData.shadowData.offset.z;
			}
			center.y -= 1f / 26f;
			Vector3 volume = def.graphicData.shadowData.volume * num2;
			Printer_Shadow.PrintShadow(layer, center, volume, Rot4.North);
		}
		Rand.PopState();
	}

	public override string GetInspectString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (def.plant.showGrowthInInspectPane)
		{
			if (LifeStage == PlantLifeStage.Growing)
			{
				stringBuilder.AppendLine("PercentGrowth".Translate(GrowthPercentString));
				stringBuilder.AppendLine("GrowthRate".Translate() + ": " + GrowthRate.ToStringPercent());
				if (!Blighted)
				{
					if (Resting)
					{
						stringBuilder.AppendLine("PlantResting".Translate());
					}
					if (!HasEnoughLightToGrow)
					{
						stringBuilder.AppendLine("PlantNeedsLightLevel".Translate() + ": " + def.plant.growMinGlow.ToStringPercent());
					}
					float growthRateFactor_Temperature = GrowthRateFactor_Temperature;
					if (growthRateFactor_Temperature < 0.99f)
					{
						if (Mathf.Approximately(growthRateFactor_Temperature, 0f) || !PlantUtility.GrowthSeasonNow(base.Position, base.Map))
						{
							stringBuilder.AppendLine("OutOfIdealTemperatureRangeNotGrowing".Translate());
						}
						else
						{
							stringBuilder.AppendLine("OutOfIdealTemperatureRange".Translate(Mathf.Max(1, Mathf.RoundToInt(growthRateFactor_Temperature * 100f)).ToString()));
						}
					}
				}
			}
			else if (LifeStage == PlantLifeStage.Mature)
			{
				if (HarvestableNow)
				{
					stringBuilder.AppendLine("ReadyToHarvest".Translate());
				}
				else
				{
					stringBuilder.AppendLine("Mature".Translate());
				}
			}
			if (DyingBecauseExposedToLight)
			{
				stringBuilder.AppendLine("DyingBecauseExposedToLight".Translate());
			}
			if (Blighted)
			{
				stringBuilder.AppendLine("Blighted".Translate() + " (" + Blight.Severity.ToStringPercent() + ")");
			}
		}
		string text = InspectStringPartsFromComps();
		if (!text.NullOrEmpty())
		{
			stringBuilder.Append(text);
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}

	public virtual void CropBlighted()
	{
		if (!Blighted)
		{
			GenSpawn.Spawn(ThingDefOf.Blight, base.Position, base.Map);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (Blighted)
		{
			Designation designation = base.Map.designationManager.DesignationOn(this);
			if (designation == null || designation.def != DesignationDefOf.CutPlant)
			{
				yield return new Command_Action
				{
					defaultLabel = "CutAllBlight".Translate(),
					defaultDesc = "CutAllBlightDesc".Translate(),
					icon = CutAllBlightTex,
					action = delegate
					{
						foreach (Plant item in base.Map.listerThings.ThingsInGroup(ThingRequestGroup.Plant))
						{
							if (item != null && item.Blighted && !base.Map.designationManager.HasMapDesignationOn(item))
							{
								base.Map.designationManager.AddDesignation(new Designation(item, DesignationDefOf.CutPlant));
							}
						}
					}
				};
			}
		}
		if (!DebugSettings.ShowDevGizmos)
		{
			yield break;
		}
		if (Blighted)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Spread blight",
				action = delegate
				{
					Blight.TryReproduceNow();
				}
			};
		}
		else
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Make blighted",
				action = CropBlighted
			};
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		foreach (StatDrawEntry item in base.SpecialDisplayStats())
		{
			yield return item;
		}
		if (def.plant.LimitedLifespan)
		{
			string valueString = Age.ToStringTicksToPeriod();
			yield return new StatDrawEntry(StatCategoryDefOf.Basics, "Stat_Thing_Plant_Age".Translate(), valueString, "Stat_Thing_Plant_AgeDesc".Translate(), 4170);
		}
		if (LifeStage == PlantLifeStage.Growing && base.Spawned)
		{
			TaggedString taggedString = "Stat_Thing_Plant_GrowthRate_Desc".Translate();
			string growthRateCalcDesc = GrowthRateCalcDesc;
			if (!growthRateCalcDesc.NullOrEmpty())
			{
				taggedString += "\n\n" + growthRateCalcDesc;
			}
			taggedString += "\n" + "StatsReport_FinalValue".Translate() + ": " + GrowthRate.ToStringPercent();
			yield return new StatDrawEntry(StatCategoryDefOf.Basics, "Stat_Thing_Plant_GrowthRate".Translate(), GrowthRate.ToStringPercent(), taggedString, 4158);
		}
	}
}
