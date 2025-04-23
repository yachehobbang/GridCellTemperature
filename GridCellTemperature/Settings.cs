using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GridCellTemperature
{
	public class Settings : ModSettings
	{
		private const int SettingsVersion = 1;

		public static readonly Setting<bool> viewSimTickTime = new(nameof(viewSimTickTime), false);
		public static readonly Setting<int> baseHeatTransferCoefficient = new(nameof(baseHeatTransferCoefficient), 3);
		public static readonly Setting<float> airDiffusivity = new(nameof(airDiffusivity), 1f);
		public static readonly Setting<float> wallDiffusivity = new(nameof(wallDiffusivity), 0.1f);
		public static readonly Setting<float> wallMassDiffusivity = new(nameof(wallMassDiffusivity), 0.1f);
		public static readonly Setting<float> skyDiffusivity = new(nameof(skyDiffusivity), 0.5f);
		public static readonly Setting<float> roofDiffusivity = new(nameof(roofDiffusivity), 0.05f);
		public static readonly Setting<float> thickRoofDiffusivity = new(nameof(thickRoofDiffusivity), 0.01f);
		public static readonly Setting<float> roomDiffusivity = new(nameof(roomDiffusivity), 0.5f);

		private static IEnumerable<Setting> AllSettings
		{
			get
			{
				return (typeof(Settings).GetFields()).Select(field => field.GetValue(null) as Setting).Where(setting => setting != null);
			}
		}

		public static void Reset()
		{
			CollectionExtensions.Do(AllSettings, setting => setting.ToDefault());
		}

		public override void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				int version = 0;
				Scribe_Values.Look(ref version, "SettingsVersion");

				if (version != SettingsVersion)
				{
					Reset();
					GridCellTemperature.Mod.Warning("Settings were reset with new update");
					return;
				}
			}
			else
			{
				int version = SettingsVersion;
				Scribe_Values.Look(ref version, "SettingsVersion");
			}

			foreach (Setting setting in Settings.AllSettings)
			{
				setting.Scribe();
			}
		}
	}
}
