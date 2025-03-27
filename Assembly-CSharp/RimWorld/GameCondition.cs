using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace RimWorld;

public class GameCondition : IExposable, ILoadReferenceable
{
	public GameConditionManager gameConditionManager;

	public Thing conditionCauser;

	public GameConditionDef def;

	public int uniqueID = -1;

	public int startTick;

	public bool suppressEndMessage;

	private int duration = -1;

	private bool permanent;

	public bool forceDisplayAsDuration;

	private List<Map> cachedAffectedMaps = new List<Map>();

	private List<Map> cachedAffectedMapsForMaps = new List<Map>();

	public Quest quest;

	public PsychicRitualDef psychicRitualDef;

	private static List<GameConditionManager> tmpGameConditionManagers = new List<GameConditionManager>();

	protected Map SingleMap => gameConditionManager.ownerMap;

	public virtual string Label => def.label;

	public virtual string LabelCap => Label.CapitalizeFirst(def);

	public virtual string LetterText => def.letterText;

	public virtual bool Expired
	{
		get
		{
			if (!Permanent)
			{
				return Find.TickManager.TicksGame > startTick + Duration;
			}
			return false;
		}
	}

	public virtual bool ElectricityDisabled => false;

	public int TicksPassed => Find.TickManager.TicksGame - startTick;

	public virtual string Description => def.description;

	public virtual int TransitionTicks => 300;

	public int TicksLeft
	{
		get
		{
			if (Permanent)
			{
				Log.ErrorOnce("Trying to get ticks left of a permanent condition.", 384767654);
				return 360000000;
			}
			return Duration - TicksPassed;
		}
		set
		{
			Duration = TicksPassed + value;
		}
	}

	public bool Permanent
	{
		get
		{
			return permanent;
		}
		set
		{
			if (value)
			{
				duration = -1;
			}
			permanent = value;
		}
	}

	public int Duration
	{
		get
		{
			if (Permanent)
			{
				Log.ErrorOnce("Trying to get duration of a permanent condition.", 100394867);
				return 360000000;
			}
			return duration;
		}
		set
		{
			permanent = false;
			duration = value;
		}
	}

	public virtual string TooltipString
	{
		get
		{
			string text = def.LabelCap.ToString();
			if (Permanent && !forceDisplayAsDuration)
			{
				text += "\n" + "Permanent".Translate().CapitalizeFirst();
			}
			else
			{
				Vector2 location = ((SingleMap != null) ? Find.WorldGrid.LongLatOf(SingleMap.Tile) : ((Find.CurrentMap != null) ? Find.WorldGrid.LongLatOf(Find.CurrentMap.Tile) : ((Find.AnyPlayerHomeMap == null) ? Vector2.zero : Find.WorldGrid.LongLatOf(Find.AnyPlayerHomeMap.Tile))));
				text = string.Concat(text, "\n", "Started".Translate(), ": ", GenDate.DateFullStringAt(GenDate.TickGameToAbs(startTick), location).Colorize(ColoredText.DateTimeColor));
				text = string.Concat(text, "\n", "Lasted".Translate(), ": ", TicksPassed.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor));
			}
			text += "\n";
			text = text + "\n" + Description.ResolveTags();
			text += "\n";
			text += "\n";
			if (conditionCauser != null && CameraJumper.CanJump(conditionCauser))
			{
				text += def.jumpToSourceKey.Translate().Resolve();
			}
			else if (quest != null && !quest.hidden)
			{
				text += "CausedByQuest".Translate(quest.name).Resolve();
			}
			else if (psychicRitualDef != null)
			{
				text += "CausedByPsychicRitual".Translate() + ": " + psychicRitualDef.label.CapitalizeFirst();
			}
			else if (!def.natural)
			{
				text += "SourceUnknown".Translate();
			}
			return text;
		}
	}

	public List<Map> AffectedMaps
	{
		get
		{
			if (!GenCollection.ListsEqual(cachedAffectedMapsForMaps, Find.Maps))
			{
				cachedAffectedMapsForMaps.Clear();
				cachedAffectedMapsForMaps.AddRange(Find.Maps);
				cachedAffectedMaps.Clear();
				if (CanApplyOnMap(gameConditionManager.ownerMap))
				{
					cachedAffectedMaps.Add(gameConditionManager.ownerMap);
				}
				tmpGameConditionManagers.Clear();
				gameConditionManager.GetChildren(tmpGameConditionManagers);
				for (int i = 0; i < tmpGameConditionManagers.Count; i++)
				{
					if (CanApplyOnMap(tmpGameConditionManagers[i].ownerMap))
					{
						cachedAffectedMaps.Add(tmpGameConditionManagers[i].ownerMap);
					}
				}
				tmpGameConditionManagers.Clear();
			}
			return cachedAffectedMaps;
		}
	}

	public virtual void ExposeData()
	{
		Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
		Scribe_Values.Look(ref suppressEndMessage, "suppressEndMessage", defaultValue: false);
		Scribe_Defs.Look(ref def, "def");
		Scribe_Values.Look(ref startTick, "startTick", 0);
		Scribe_Values.Look(ref duration, "duration", 0);
		Scribe_Values.Look(ref permanent, "permanent", defaultValue: false);
		Scribe_References.Look(ref quest, "quest");
		Scribe_Values.Look(ref forceDisplayAsDuration, "forceDisplayAsDuration", defaultValue: false);
		Scribe_Defs.Look(ref psychicRitualDef, "psychicRitualDef");
		BackCompatibility.PostExposeData(this);
	}

	public virtual void GameConditionTick()
	{
	}

	public virtual void GameConditionDraw(Map map)
	{
	}

	public virtual void Init()
	{
		if (!def.startMessage.NullOrEmpty())
		{
			Messages.Message(def.startMessage, MessageTypeDefOf.NeutralEvent);
		}
	}

	public virtual void End()
	{
		if (!suppressEndMessage && def.endMessage != null && !cachedAffectedMaps.Any((Map map) => HiddenByOtherCondition(map)))
		{
			Messages.Message(def.endMessage, MessageTypeDefOf.NeutralEvent);
		}
		gameConditionManager.ActiveConditions.Remove(this);
	}

	public bool CanApplyOnMap(Map map)
	{
		if (map == null)
		{
			return false;
		}
		if (map.generatorDef.isUnderground && !def.allowUnderground)
		{
			return false;
		}
		return true;
	}

	public bool HiddenByOtherCondition(Map map)
	{
		if (def.silencedByConditions.NullOrEmpty())
		{
			return false;
		}
		for (int i = 0; i < def.silencedByConditions.Count; i++)
		{
			if (map.gameConditionManager.ConditionIsActive(def.silencedByConditions[i]))
			{
				return true;
			}
		}
		return false;
	}

	public virtual float SkyGazeChanceFactor(Map map)
	{
		return 1f;
	}

	public virtual float SkyGazeJoyGainFactor(Map map)
	{
		return 1f;
	}

	public virtual float TemperatureOffset()
	{
		return 0f;
	}

	public virtual float SkyTargetLerpFactor(Map map)
	{
		return 0f;
	}

	public virtual SkyTarget? SkyTarget(Map map)
	{
		return null;
	}

	public virtual float AnimalDensityFactor(Map map)
	{
		return 1f;
	}

	public virtual float PlantDensityFactor(Map map)
	{
		return 1f;
	}

	public virtual bool AllowEnjoyableOutsideNow(Map map)
	{
		return true;
	}

	public virtual List<SkyOverlay> SkyOverlays(Map map)
	{
		return null;
	}

	public virtual void DoCellSteadyEffects(IntVec3 c, Map map)
	{
	}

	public virtual WeatherDef ForcedWeather()
	{
		return null;
	}

	public virtual void PostMake()
	{
		uniqueID = Find.UniqueIDsManager.GetNextGameConditionID();
	}

	public virtual void RandomizeSettings(float points, Map map, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
	{
	}

	public string GetUniqueLoadID()
	{
		return $"{GetType().Name}_{uniqueID.ToString()}";
	}
}
