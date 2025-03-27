using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class MechanitorControlGroupGizmo : Gizmo
{
	public const int InRectPadding = 6;

	private const float Width = 130f;

	private const int IconButtonSize = 26;

	private const float BaseSelectedTexJump = 20f;

	private const float BaseSelectedTextScale = 0.8f;

	private static readonly CachedTexture PowerIcon = new CachedTexture("UI/Icons/MechRechargeSettings");

	private static readonly Color UncontrolledMechBackgroundColor = new Color32(byte.MaxValue, 25, 25, 55);

	private MechanitorControlGroup controlGroup;

	private List<MechanitorControlGroup> mergedControlGroups;

	public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => GetWorkModeOptions(controlGroup);

	public override bool Visible
	{
		get
		{
			if (controlGroup.MechsForReading.Count <= 0)
			{
				return Find.Selector.SelectedPawns.Count == 1;
			}
			return true;
		}
	}

	public override float Order
	{
		get
		{
			if (controlGroup.MechsForReading.Count > 0)
			{
				return -89f;
			}
			return -88f;
		}
	}

	public MechanitorControlGroupGizmo(MechanitorControlGroup controlGroup)
	{
		this.controlGroup = controlGroup;
		Order = -89f;
	}

	public static IEnumerable<FloatMenuOption> GetWorkModeOptions(MechanitorControlGroup controlGroup)
	{
		foreach (MechWorkModeDef wm in DefDatabase<MechWorkModeDef>.AllDefsListForReading.OrderBy((MechWorkModeDef d) => d.uiOrder))
		{
			FloatMenuOption floatMenuOption = new FloatMenuOption(wm.LabelCap, delegate
			{
				controlGroup.SetWorkMode(wm);
			}, wm.uiIcon, Color.white);
			floatMenuOption.tooltip = new TipSignal(wm.description, wm.index ^ 0xDFE8661);
			yield return floatMenuOption;
		}
	}

	public override void GizmoUpdateOnMouseover()
	{
		base.GizmoUpdateOnMouseover();
		controlGroup.WorkMode.Worker.DrawControlGroupMouseOverExtra(controlGroup);
	}

	public override bool GroupsWith(Gizmo other)
	{
		if (!(other is MechanitorControlGroupGizmo mechanitorControlGroupGizmo))
		{
			return false;
		}
		if (mechanitorControlGroupGizmo.controlGroup == controlGroup)
		{
			return true;
		}
		if (controlGroup.Tracker == mechanitorControlGroupGizmo.controlGroup.Tracker && controlGroup.MechsForReading.Count == 0 && mechanitorControlGroupGizmo.controlGroup.MechsForReading.Count == 0)
		{
			return true;
		}
		if (mergedControlGroups.NotNullAndContains(mechanitorControlGroupGizmo.controlGroup))
		{
			mergedControlGroups.Remove(mechanitorControlGroupGizmo.controlGroup);
		}
		return false;
	}

	public override void MergeWith(Gizmo gizmo)
	{
		if (!(gizmo is MechanitorControlGroupGizmo mechanitorControlGroupGizmo))
		{
			Log.ErrorOnce("Tried to merge MechanitorControlGroupGizmo with unexpected type", 345234235);
		}
		else if (mechanitorControlGroupGizmo.controlGroup != controlGroup)
		{
			if (mergedControlGroups == null)
			{
				mergedControlGroups = new List<MechanitorControlGroup>();
			}
			if (!mergedControlGroups.Contains(mechanitorControlGroupGizmo.controlGroup))
			{
				mergedControlGroups.Add(mechanitorControlGroupGizmo.controlGroup);
			}
		}
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		if (!ModLister.CheckBiotech("Mechanitor control group gizmo"))
		{
			return new GizmoResult(GizmoState.Clear);
		}
		AcceptanceReport canControlMechs = controlGroup.Tracker.CanControlMechs;
		disabled = !canControlMechs;
		disabledReason = canControlMechs.Reason;
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect rect2 = rect.ContractedBy(6f);
		bool flag = Mouse.IsOver(rect2);
		List<Pawn> mechsForReading = controlGroup.MechsForReading;
		Color white = Color.white;
		Material material = ((disabled || parms.lowLight || mechsForReading.Count <= 0) ? TexUI.GrayscaleGUI : null);
		GUI.color = (parms.lowLight ? Command.LowLightBgColor : white);
		GenUI.DrawTextureWithMaterial(rect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex, material);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect3 = rect2;
		TaggedString str = ((!mergedControlGroups.NullOrEmpty()) ? "Groups".Translate() : "Group".Translate());
		str += " " + controlGroup.Index;
		if (!mergedControlGroups.NullOrEmpty())
		{
			mergedControlGroups.SortBy((MechanitorControlGroup c) => c.Index);
			for (int i = 0; i < mergedControlGroups.Count; i++)
			{
				str += ", " + mergedControlGroups[i].Index;
			}
		}
		str = str.Truncate(rect2.width);
		Vector2 vector = Text.CalcSize(str);
		rect3.width = vector.x;
		rect3.height = vector.y;
		Widgets.Label(rect3, str);
		if (mechsForReading.Count <= 0)
		{
			GUI.color = ColoredText.SubtleGrayColor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect2, "(" + "NoMechs".Translate() + ")");
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			return new GizmoResult(GizmoState.Clear);
		}
		if (Mouse.IsOver(rect3))
		{
			Widgets.DrawHighlight(rect3);
			if (Widgets.ButtonInvisible(rect3))
			{
				Find.Selector.ClearSelection();
				for (int j = 0; j < mechsForReading.Count; j++)
				{
					Find.Selector.Select(mechsForReading[j]);
				}
			}
		}
		bool flag2 = false;
		Rect rect4 = new Rect(rect.x + rect.width - 26f - 6f, rect.y + 6f, 26f, 26f);
		Widgets.DrawTextureFitted(rect4, PowerIcon.Texture, 1f);
		if (!disabled && Mouse.IsOver(rect4))
		{
			flag2 = true;
			Widgets.DrawHighlight(rect4);
			if (Widgets.ButtonInvisible(rect4))
			{
				Find.WindowStack.Add(new Dialog_RechargeSettings(controlGroup));
			}
		}
		bool flag3 = false;
		Rect rect5 = new Rect(rect.x + rect.width - 52f - 6f, rect.y + 6f, 26f, 26f);
		Widgets.DrawTextureFitted(rect5, controlGroup.WorkMode.uiIcon, 1f);
		if (!disabled && Mouse.IsOver(rect5))
		{
			flag3 = true;
			Widgets.DrawHighlight(rect5);
		}
		Rect rect6 = new Rect(rect2.x, rect2.y + 26f + 4f, rect2.width, rect2.height - 26f - 4f);
		float num = rect6.height;
		int num2 = 0;
		int num3 = 0;
		for (float num4 = num; num4 >= 0f; num4 -= 1f)
		{
			num2 = Mathf.FloorToInt(rect6.width / num4);
			num3 = Mathf.FloorToInt(rect6.height / num4);
			if (num2 * num3 >= mechsForReading.Count)
			{
				num = num4;
				break;
			}
		}
		float num5 = (rect6.width - (float)num2 * num) / 2f;
		float num6 = (rect6.height - (float)num3 * num) / 2f;
		int num7 = 0;
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num2; l++)
			{
				if (num7 >= mechsForReading.Count)
				{
					break;
				}
				Rect rect7 = new Rect(rect6.x + (float)l * num + num5, rect6.y + (float)k * num + num6, num, num);
				RenderTexture image = PortraitsCache.Get(mechsForReading[num7], rect7.size, Rot4.East, default(Vector3), mechsForReading[num7].kindDef.controlGroupPortraitZoom, supersample: true, compensateForUIScale: true, renderHeadgear: true, renderClothes: true, null, null, stylingStation: false, null);
				if (!controlGroup.Tracker.ControlledPawns.Contains(mechsForReading[num7]))
				{
					Widgets.DrawRectFast(rect7, UncontrolledMechBackgroundColor);
				}
				GUI.DrawTexture(rect7, image);
				if (Mouse.IsOver(rect7))
				{
					Widgets.DrawHighlight(rect7);
					MouseoverSounds.DoRegion(rect7, SoundDefOf.Mouseover_Command);
					if (Event.current.type == EventType.MouseDown)
					{
						if (Event.current.shift)
						{
							Find.Selector.Select(mechsForReading[num7]);
						}
						else
						{
							CameraJumper.TryJumpAndSelect(mechsForReading[num7]);
						}
					}
					TargetHighlighter.Highlight(mechsForReading[num7], arrow: true, colonistBar: false);
				}
				if (Find.Selector.IsSelected(mechsForReading[num7]))
				{
					SelectionDrawerUtility.DrawSelectionOverlayOnGUI(mechsForReading[num7], rect7, 0.8f / (float)num2, 20f);
				}
				num7++;
			}
			if (num7 >= mechsForReading.Count)
			{
				break;
			}
		}
		if (Find.WindowStack.FloatMenu == null && !flag2)
		{
			TooltipHandler.TipRegion(rect, delegate
			{
				string text = ((string)("ControlGroup".Translate() + " #") + controlGroup.Index).Colorize(ColoredText.TipSectionTitleColor) + "\n\n";
				text = text + ("CurrentMechWorkMode".Translate() + ": " + controlGroup.WorkMode.LabelCap).Colorize(ColoredText.TipSectionTitleColor) + "\n" + controlGroup.WorkMode.description + "\n\n";
				IEnumerable<string> entries = from x in controlGroup.MechsForReading
					where x.needs?.energy != null
					select x into m
					select (m.LabelCap + " (" + m.needs.energy.CurLevelPercentage.ToStringPercent() + " " + "EnergyLower".Translate() + ")").Resolve();
				text = text + "AssignedMechs".Translate().Colorize(ColoredText.TipSectionTitleColor) + "\n" + entries.ToLineList(" - ");
				if (disabled && !disabledReason.NullOrEmpty())
				{
					text += ("\n\n" + "DisabledCommand".Translate() + ": " + disabledReason).Colorize(ColorLibrary.RedReadable);
				}
				return text;
			}, 2545872);
		}
		if (flag3 && Event.current.type == EventType.MouseDown)
		{
			return new GizmoResult(GizmoState.OpenedFloatMenu, Event.current);
		}
		return new GizmoResult(flag ? GizmoState.Mouseover : GizmoState.Clear);
	}

	public override float GetWidth(float maxWidth)
	{
		return 130f;
	}
}
