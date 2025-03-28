using Verse;
using Verse.Sound;
using Verse.Steam;

namespace RimWorld;

public sealed class PlaySettings : IExposable
{
	public bool showLearningHelper = true;

	public bool showZones = true;

	public bool showBeauty;

	public bool showRoomStats;

	public bool showColonistBar = true;

	public bool showRoofOverlay;

	public bool showFertilityOverlay;

	public bool showTerrainAffordanceOverlay;

	public bool showPollutionOverlay;

	public bool autoHomeArea = true;

	public bool autoRebuild;

	public bool showTemperatureOverlay;

	public bool lockNorthUp = true;

	public bool usePlanetDayNightSystem = true;

	public bool showExpandingIcons = true;

	public bool showWorldFeatures = true;

	public bool useWorkPriorities;

	public MedicalCareCategory defaultCareForColonist = MedicalCareCategory.Best;

	public MedicalCareCategory defaultCareForPrisoner = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForSlave = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForTamedAnimal = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForFriendlyFaction = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForNeutralFaction = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForHostileFaction = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForNoFaction = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForWildlife = MedicalCareCategory.HerbalOrWorse;

	public MedicalCareCategory defaultCareForEntities = MedicalCareCategory.NoMeds;

	public MedicalCareCategory defaultCareForGhouls = MedicalCareCategory.NoMeds;

	public void ExposeData()
	{
		Scribe_Values.Look(ref showLearningHelper, "showLearningHelper", defaultValue: false);
		Scribe_Values.Look(ref showZones, "showZones", defaultValue: false);
		Scribe_Values.Look(ref showBeauty, "showBeauty", defaultValue: false);
		Scribe_Values.Look(ref showRoomStats, "showRoomStats", defaultValue: false);
		Scribe_Values.Look(ref showColonistBar, "showColonistBar", defaultValue: false);
		Scribe_Values.Look(ref showRoofOverlay, "showRoofOverlay", defaultValue: false);
		Scribe_Values.Look(ref showFertilityOverlay, "showFertilityOverlay", defaultValue: false);
		Scribe_Values.Look(ref showTerrainAffordanceOverlay, "showTerrainAffordanceOverlay", defaultValue: false);
		Scribe_Values.Look(ref showPollutionOverlay, "showPollutionOverlay", defaultValue: false);
		Scribe_Values.Look(ref autoHomeArea, "autoHomeArea", defaultValue: false);
		Scribe_Values.Look(ref autoRebuild, "autoRebuild", defaultValue: false);
		Scribe_Values.Look(ref lockNorthUp, "lockNorthUp", defaultValue: false);
		Scribe_Values.Look(ref usePlanetDayNightSystem, "usePlanetDayNightSystem", defaultValue: false);
		Scribe_Values.Look(ref showExpandingIcons, "showExpandingIcons", defaultValue: false);
		Scribe_Values.Look(ref showWorldFeatures, "showWorldFeatures", defaultValue: false);
		Scribe_Values.Look(ref showTemperatureOverlay, "showTemperatureOverlay", defaultValue: false);
		Scribe_Values.Look(ref useWorkPriorities, "useWorkPriorities", defaultValue: false);
		Scribe_Values.Look(ref defaultCareForColonist, "defaultCareForColonist", MedicalCareCategory.Best);
		Scribe_Values.Look(ref defaultCareForTamedAnimal, "defaultCareForTamedAnimal", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForPrisoner, "defaultCareForPrisoner", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForSlave, "defaultCareForSlave", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForNeutralFaction, "defaultCareForNeutralFaction", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForWildlife, "defaultCareForWildlife", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForHostileFaction, "defaultCareForHumanlikeEnemies", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForFriendlyFaction, "defaultCareForFriendlyFaction", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForNoFaction, "defaultCareForNoFaction", MedicalCareCategory.HerbalOrWorse);
		Scribe_Values.Look(ref defaultCareForEntities, "defaultCareForEntities", MedicalCareCategory.NoMeds);
		Scribe_Values.Look(ref defaultCareForGhouls, "defaultCareForGhouls", MedicalCareCategory.NoMeds);
		BackCompatibility.PostExposeData(this);
	}

	public void DoPlaySettingsGlobalControls(WidgetRow row, bool worldView)
	{
		bool num = showColonistBar;
		if (worldView)
		{
			if (Current.ProgramState == ProgramState.Playing)
			{
				row.ToggleableIcon(ref showColonistBar, TexButton.ShowColonistBar, SteamDeck.IsSteamDeckInNonKeyboardMode ? "ShowColonistBarToggleButtonController".Translate() : "ShowColonistBarToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			}
			bool num2 = lockNorthUp;
			row.ToggleableIcon(ref lockNorthUp, TexButton.LockNorthUp, "LockNorthUpToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			if (num2 != lockNorthUp && lockNorthUp)
			{
				Find.WorldCameraDriver.RotateSoNorthIsUp();
			}
			row.ToggleableIcon(ref usePlanetDayNightSystem, TexButton.UsePlanetDayNightSystem, "UsePlanetDayNightSystemToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref showExpandingIcons, TexButton.ShowExpandingIcons, "ShowExpandingIconsToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref showWorldFeatures, TexButton.ShowWorldFeatures, "ShowWorldFeaturesToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
		}
		else
		{
			row.ToggleableIcon(ref showLearningHelper, TexButton.ShowLearningHelper, "ShowLearningHelperWhenEmptyToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref showZones, TexButton.ShowZones, "ZoneVisibilityToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(tooltip: string.Format(arg1: KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.ToggleBeautyDisplay, KeyPrefs.BindingSlot.A).ToStringReadable(), format: "{0}: {1}\n\n{2}", arg0: "HotKeyTip".Translate(), arg2: "ShowBeautyToggleButton".Translate()), toggleable: ref showBeauty, tex: TexButton.ShowBeauty, mouseoverSound: SoundDefOf.Mouseover_ButtonToggle);
			CheckKeyBindingToggle(KeyBindingDefOf.ToggleBeautyDisplay, ref showBeauty);
			row.ToggleableIcon(tooltip: string.Format(arg1: KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.ToggleRoomStatsDisplay, KeyPrefs.BindingSlot.A).ToStringReadable(), format: "{0}: {1}\n\n{2}", arg0: "HotKeyTip".Translate(), arg2: "ShowRoomStatsToggleButton".Translate()), toggleable: ref showRoomStats, tex: TexButton.ShowRoomStats, mouseoverSound: SoundDefOf.Mouseover_ButtonToggle);
			CheckKeyBindingToggle(KeyBindingDefOf.ToggleRoomStatsDisplay, ref showRoomStats);
			row.ToggleableIcon(ref showColonistBar, TexButton.ShowColonistBar, SteamDeck.IsSteamDeckInNonKeyboardMode ? "ShowColonistBarToggleButtonController".Translate() : "ShowColonistBarToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref showRoofOverlay, TexButton.ShowRoofOverlay, "ShowRoofOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref showFertilityOverlay, TexButton.ShowFertilityOverlay, "ShowFertilityOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref showTerrainAffordanceOverlay, TexButton.ShowTerrainAffordanceOverlay, "ShowTerrainAffordanceOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref autoHomeArea, TexButton.AutoHomeArea, "AutoHomeAreaToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref autoRebuild, TexButton.AutoRebuild, "AutoRebuildButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			row.ToggleableIcon(ref showTemperatureOverlay, TexButton.ShowTemperatureOverlay, "ShowTemperatureOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			bool toggleable = Prefs.ResourceReadoutCategorized;
			bool flag = toggleable;
			row.ToggleableIcon(ref toggleable, TexButton.CategorizedResourceReadout, "CategorizedResourceReadoutToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			if (toggleable != flag)
			{
				Prefs.ResourceReadoutCategorized = toggleable;
			}
			if (ModsConfig.BiotechActive)
			{
				row.ToggleableIcon(ref showPollutionOverlay, TexButton.ShowPollutionOverlay, "ShowPollutionOverlayToggleButton".Translate(), SoundDefOf.Mouseover_ButtonToggle);
			}
			string tooltip3 = string.Format(arg1: KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.OpenMapSearch, KeyPrefs.BindingSlot.A).ToStringReadable(), format: "{0}: {1}\n\n{2}", arg0: "HotKeyTip".Translate(), arg2: "SearchTheMapDesc".Translate());
			if (Find.CurrentMap != null && (row.ButtonIcon(TexButton.SearchButton, tooltip3, null, null, null) || KeyBindingDefOf.OpenMapSearch.JustPressed))
			{
				Find.WindowStack.Add(new Dialog_MapSearch());
			}
			if (ModsConfig.AnomalyActive && Find.Anomaly.AnomalyStudyEnabled)
			{
				UIHighlighter.HighlightOpportunity(row.ButtonIconRect(), "EntityCodex");
				if (row.ButtonIcon(TexButton.CodexButton, "EntityCodexGizmoTip".Translate(), null, null, null))
				{
					Find.WindowStack.Add(new Dialog_EntityCodex());
				}
			}
		}
		if (num != showColonistBar)
		{
			Find.ColonistBar.MarkColonistsDirty();
		}
	}

	private void CheckKeyBindingToggle(KeyBindingDef keyBinding, ref bool value)
	{
		if (keyBinding.KeyDownEvent)
		{
			value = !value;
			if (value)
			{
				SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
			}
			else
			{
				SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
			}
		}
	}
}
