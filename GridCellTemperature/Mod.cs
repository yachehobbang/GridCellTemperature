using HarmonyLib;
using UnityEngine;
using Verse;

namespace GridCellTemperature
{
	public sealed class Mod : Verse.Mod
	{
		public const bool CheckTickTime = true;

		public const string Id = "GridCellTemperature";
		public const string Name = "GridCell Temperature";
		public const string Version = "1.0";
		public static Mod Instance;

		public Mod(ModContentPack content)
		  : base(content)
		{
			Mod.Instance = this;
			GetSettings<Settings>();
			var harmony = new Harmony("GridCellTemperature");
			harmony.PatchAll();

			Mod.Log("Initialized");
		}

		public static void Log(string message) => Verse.Log.Message(Mod.PrefixMessage(message));

		public static void Warning(string message) => Verse.Log.Warning(Mod.PrefixMessage(message));

		public static void Error(string message) => Verse.Log.Error(Mod.PrefixMessage(message));

		private static string PrefixMessage(string message) => $"[GridCellTemperature v{Version}] " + message;

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Settings listingSettings = new Listing_Settings();
			listingSettings.Begin(inRect);

			if (listingSettings.ButtonText("Reset", null, 1f))
			{
				Settings.Reset();
			}

			listingSettings.CheckboxLabeled("View SimTick Time", ref Settings.viewSimTickTime.Value);

			listingSettings.SliderLabeled("Base Heat Transfer Coefficient", ref Settings.baseHeatTransferCoefficient.Value, 1, 4, 1);
			listingSettings.SliderLabeled("Air-to-Surface Heat Transfer Coefficient", ref Settings.airDiffusivity.Value, 0f, 1f, 0.001f);
			listingSettings.SliderLabeled("Air-to-Wall Heat Transfer Coefficient", ref Settings.wallDiffusivity.Value, 0f, 1f, 0.001f);
			listingSettings.SliderLabeled("Wall-to-Wall Heat Transfer Coefficient", ref Settings.wallMassDiffusivity.Value, 0f, 1f, 0.001f);
			listingSettings.SliderLabeled("Outdoor Heat Exchange", ref Settings.skyDiffusivity.Value, 0f, 1f, 0.001f);
			listingSettings.SliderLabeled("Roof Heat Exchange", ref Settings.roofDiffusivity.Value, 0f, 1f, 0.001f);
			listingSettings.SliderLabeled("ThickRoof Heat Exchange", ref Settings.thickRoofDiffusivity.Value, 0f, 1f, 0.001f);
			listingSettings.SliderLabeled("Indoor Thermal Diffusivity", ref Settings.roomDiffusivity.Value, 0f, 1f, 0.001f);

			listingSettings.End();

			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory() => "Temperature Grid";
	}
}
