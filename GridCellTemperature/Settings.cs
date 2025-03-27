using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace GridCellTemperature
{
	public class Settings : ModSettings
	{
		private const int SettingsVersion = 1;

		public static readonly Setting<bool> viewSimTickTime = new Setting<bool>(nameof(viewSimTickTime), false);
		public static readonly Setting<float> baseHeatTransferCoefficient = new Setting<float>(nameof(baseHeatTransferCoefficient), 3f);
		public static readonly Setting<float> airDiffusivity = new Setting<float>(nameof(airDiffusivity), 1f);
		public static readonly Setting<float> wallDiffusivity = new Setting<float>(nameof(wallDiffusivity), 0.1f);
		public static readonly Setting<float> wallMassDiffusivity = new Setting<float>(nameof(wallMassDiffusivity), 0.1f);
		public static readonly Setting<float> skyDiffusivity = new Setting<float>(nameof(skyDiffusivity), 0.5f);
		public static readonly Setting<float> roofDiffusivity = new Setting<float>(nameof(roofDiffusivity), 0.05f);
		public static readonly Setting<float> thickRoofDiffusivity = new Setting<float>(nameof(thickRoofDiffusivity), 0.01f);
		public static readonly Setting<float> roomDiffusivity = new Setting<float>(nameof(roomDiffusivity), 0.5f);

		private static IEnumerable<Setting> AllSettings
		{
			get
			{
				return ((IEnumerable<FieldInfo>)typeof(Settings).GetFields()).Select<FieldInfo, Setting>((Func<FieldInfo, Setting>)(field => field.GetValue((object)null) as Setting)).Where<Setting>((Func<Setting, bool>)(setting => setting != null));
			}
		}

		public static void Reset()
		{
			CollectionExtensions.Do<Setting>(Settings.AllSettings, (Action<Setting>)(setting => setting.ToDefault()));
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
