using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GridCellTemperature
{
	[StaticConstructorOnStartup]
	public static class OnStartUp
	{
		static OnStartUp()
		{
			var temperatureColorMapFieldInfo = AccessTools.Field(typeof(MapTemperature), "TemperatureColorMap");
			temperatureColorMapFieldInfo.SetValue(null, new List<(float, Color)>
			{
				(-100f, ColorLibrary.Black),
				(-25f, ColorLibrary.DarkBlue),
				(0f, ColorLibrary.Blue),
				(25f, ColorLibrary.Green),
				(50f, ColorLibrary.Yellow),
				(150f, ColorLibrary.Red),
				(300f, ColorLibrary.Magenta),
				(600f, new Color(1f, 1f, 1f)),
				// MapTemperature 에 TemperatureColorMap 구현이 잘못되어서 마지막에 한 번 더 넣어줌
				(600f, new Color(1f, 1f, 1f)),
			});
		}
	}
}
