using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld;

public class WeatherDecider : IExposable
{
	private Map map;

	private int curWeatherDuration = 10000;

	private int ticksWhenRainAllowedAgain;

	private const int FirstWeatherDuration = 10000;

	private const float ChanceFactorRainOnFire = 15f;

	private static List<GameCondition> allConditionsTmp = new List<GameCondition>();

	public WeatherDef ForcedWeather
	{
		get
		{
			allConditionsTmp.Clear();
			map.gameConditionManager.GetAllGameConditionsAffectingMap(map, allConditionsTmp);
			WeatherDef result = null;
			foreach (GameCondition item in allConditionsTmp)
			{
				WeatherDef weatherDef = item.ForcedWeather();
				if (weatherDef != null)
				{
					result = weatherDef;
				}
			}
			return result;
		}
	}

	public IEnumerable<WeatherCommonalityRecord> WeatherCommonalities => map.Biome.baseWeatherCommonalities;

	public bool ClearWeatherAllowed
	{
		get
		{
			if (!WeatherCommonalities.EnumerableNullOrEmpty())
			{
				return WeatherCommonalities.Any((WeatherCommonalityRecord w) => w.weather == WeatherDefOf.Clear);
			}
			return false;
		}
	}

	public WeatherDecider(Map map)
	{
		this.map = map;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref curWeatherDuration, "curWeatherDuration", 0, forceSave: true);
		Scribe_Values.Look(ref ticksWhenRainAllowedAgain, "ticksWhenRainAllowedAgain", 0);
	}

	public void WeatherDeciderTick()
	{
		WeatherDef forcedWeather = ForcedWeather;
		int num = curWeatherDuration;
		if (map.fireWatcher.LargeFireDangerPresent || !map.weatherManager.curWeather.temperatureRange.Includes(map.mapTemperature.OutdoorTemp) || (ModsConfig.AnomalyActive && map.gameConditionManager.BrightnessChanging))
		{
			num = (int)((float)num * 0.25f);
		}
		if (forcedWeather != null && map.weatherManager.curWeather != forcedWeather)
		{
			num = ((!ModsConfig.AnomalyActive || forcedWeather.transitionTicksOverride == int.MaxValue) ? 4000 : forcedWeather.transitionTicksOverride);
		}
		if (map.weatherManager.curWeatherAge > num)
		{
			StartNextWeather();
		}
	}

	public void StartNextWeather()
	{
		WeatherDef weatherDef = ChooseNextWeather();
		map.weatherManager.TransitionTo(weatherDef);
		curWeatherDuration = weatherDef.durationRange.RandomInRange;
	}

	public void StartInitialWeather()
	{
		if (Find.GameInitData != null)
		{
			if (ClearWeatherAllowed)
			{
				map.weatherManager.curWeather = WeatherDefOf.Clear;
			}
			else
			{
				map.weatherManager.curWeather = WeatherCommonalities.RandomElement().weather;
			}
			curWeatherDuration = 10000;
			map.weatherManager.lastWeather = map.weatherManager.curWeather;
			map.weatherManager.curWeatherAge = 0;
		}
		else
		{
			map.weatherManager.curWeather = null;
			WeatherDef weatherDef = ChooseNextWeather();
			WeatherDef lastWeather = ChooseNextWeather();
			map.weatherManager.curWeather = weatherDef;
			map.weatherManager.lastWeather = lastWeather;
			curWeatherDuration = weatherDef.durationRange.RandomInRange;
			map.weatherManager.curWeatherAge = Rand.Range(0, curWeatherDuration);
		}
		map.weatherManager.ResetSkyTargetLerpCache();
	}

	private WeatherDef ChooseNextWeather()
	{
		if (TutorSystem.TutorialMode && ClearWeatherAllowed)
		{
			return WeatherDefOf.Clear;
		}
		WeatherDef forcedWeather = ForcedWeather;
		if (forcedWeather != null)
		{
			return forcedWeather;
		}
		if (!DefDatabase<WeatherDef>.AllDefs.TryRandomElementByWeight(CurrentWeatherCommonality, out var result))
		{
			Log.Warning("All weather commonalities were zero. Defaulting to " + WeatherDefOf.Clear.defName + ".");
			if (!WeatherCommonalities.EnumerableNullOrEmpty())
			{
				return WeatherCommonalities.RandomElement().weather;
			}
			return WeatherDefOf.Clear;
		}
		return result;
	}

	public void DisableRainFor(int ticks)
	{
		ticksWhenRainAllowedAgain = Find.TickManager.TicksGame + ticks;
	}

	private float CurrentWeatherCommonality(WeatherDef weather)
	{
		if (map.weatherManager.curWeather != null && !map.weatherManager.curWeather.repeatable && weather == map.weatherManager.curWeather)
		{
			return 0f;
		}
		if (!weather.temperatureRange.Includes(map.mapTemperature.OutdoorTemp))
		{
			return 0f;
		}
		if ((int)weather.favorability < 2 && GenDate.DaysPassedSinceSettle < 8)
		{
			return 0f;
		}
		if (weather.rainRate > 0.1f && Find.TickManager.TicksGame < ticksWhenRainAllowedAgain)
		{
			return 0f;
		}
		if (weather.rainRate > 0.1f && map.gameConditionManager.ActiveConditions.Any((GameCondition x) => x.def.preventRain))
		{
			return 0f;
		}
		if (ModsConfig.AnomalyActive && weather.minMonolithLevel > Find.Anomaly.HighestLevelReached)
		{
			return 0f;
		}
		BiomeDef biome = map.Biome;
		for (int i = 0; i < biome.baseWeatherCommonalities.Count; i++)
		{
			WeatherCommonalityRecord weatherCommonalityRecord = biome.baseWeatherCommonalities[i];
			if (weatherCommonalityRecord.weather == weather)
			{
				float num = weatherCommonalityRecord.commonality;
				if (map.fireWatcher.LargeFireDangerPresent && weather.rainRate > 0.1f)
				{
					num *= 15f;
				}
				if (weatherCommonalityRecord.weather.commonalityRainfallFactor != null)
				{
					num *= weatherCommonalityRecord.weather.commonalityRainfallFactor.Evaluate(map.TileInfo.rainfall);
				}
				return num;
			}
		}
		return 0f;
	}

	public void LogWeatherChances()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (WeatherDef item in DefDatabase<WeatherDef>.AllDefs.OrderByDescending((WeatherDef w) => CurrentWeatherCommonality(w)))
		{
			stringBuilder.AppendLine(item.label + " - " + CurrentWeatherCommonality(item));
		}
		Log.Message(stringBuilder.ToString());
	}
}
