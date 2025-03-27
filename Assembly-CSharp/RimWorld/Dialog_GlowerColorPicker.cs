using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class Dialog_GlowerColorPicker : Window
{
	private const float GlowValue = 1f;

	private const int ContextHash = 195906069;

	private static readonly List<Color> colors = new List<Color>
	{
		Color.HSVToRGB(0f, 0f, 1f),
		Color.HSVToRGB(0f, 0.5f, 1f),
		Color.HSVToRGB(0f, 0.33f, 1f),
		Color.HSVToRGB(1f / 18f, 1f, 1f),
		Color.HSVToRGB(1f / 18f, 0.5f, 1f),
		Color.HSVToRGB(1f / 18f, 0.33f, 1f),
		Color.HSVToRGB(1f / 9f, 1f, 1f),
		Color.HSVToRGB(1f / 9f, 0.5f, 1f),
		Color.HSVToRGB(1f / 9f, 0.33f, 1f),
		Color.HSVToRGB(1f / 6f, 1f, 1f),
		Color.HSVToRGB(1f / 6f, 0.5f, 1f),
		Color.HSVToRGB(1f / 6f, 0.33f, 1f),
		Color.HSVToRGB(2f / 9f, 1f, 1f),
		Color.HSVToRGB(2f / 9f, 0.5f, 1f),
		Color.HSVToRGB(2f / 9f, 0.33f, 1f),
		Color.HSVToRGB(5f / 18f, 1f, 1f),
		Color.HSVToRGB(5f / 18f, 0.5f, 1f),
		Color.HSVToRGB(5f / 18f, 0.33f, 1f),
		Color.HSVToRGB(1f / 3f, 1f, 1f),
		Color.HSVToRGB(1f / 3f, 0.5f, 1f),
		Color.HSVToRGB(1f / 3f, 0.33f, 1f),
		Color.HSVToRGB(7f / 18f, 1f, 1f),
		Color.HSVToRGB(7f / 18f, 0.5f, 1f),
		Color.HSVToRGB(7f / 18f, 0.33f, 1f),
		Color.HSVToRGB(4f / 9f, 1f, 1f),
		Color.HSVToRGB(4f / 9f, 0.5f, 1f),
		Color.HSVToRGB(4f / 9f, 0.33f, 1f),
		Color.HSVToRGB(0.5f, 1f, 1f),
		Color.HSVToRGB(0.5f, 0.5f, 1f),
		Color.HSVToRGB(0.5f, 0.33f, 1f),
		Color.HSVToRGB(5f / 9f, 1f, 1f),
		Color.HSVToRGB(5f / 9f, 0.5f, 1f),
		Color.HSVToRGB(5f / 9f, 0.33f, 1f),
		Color.HSVToRGB(11f / 18f, 1f, 1f),
		Color.HSVToRGB(11f / 18f, 0.5f, 1f),
		Color.HSVToRGB(11f / 18f, 0.33f, 1f),
		Color.HSVToRGB(2f / 3f, 1f, 1f),
		Color.HSVToRGB(2f / 3f, 0.5f, 1f),
		Color.HSVToRGB(2f / 3f, 0.33f, 1f),
		Color.HSVToRGB(13f / 18f, 1f, 1f),
		Color.HSVToRGB(13f / 18f, 0.5f, 1f),
		Color.HSVToRGB(13f / 18f, 0.33f, 1f),
		Color.HSVToRGB(7f / 9f, 1f, 1f),
		Color.HSVToRGB(7f / 9f, 0.5f, 1f),
		Color.HSVToRGB(7f / 9f, 0.33f, 1f),
		Color.HSVToRGB(5f / 6f, 1f, 1f),
		Color.HSVToRGB(5f / 6f, 0.5f, 1f),
		Color.HSVToRGB(5f / 6f, 0.33f, 1f),
		Color.HSVToRGB(8f / 9f, 1f, 1f),
		Color.HSVToRGB(8f / 9f, 0.5f, 1f),
		Color.HSVToRGB(8f / 9f, 0.33f, 1f),
		Color.HSVToRGB(17f / 18f, 1f, 1f),
		Color.HSVToRGB(17f / 18f, 0.5f, 1f),
		Color.HSVToRGB(17f / 18f, 0.33f, 1f)
	};

	private static readonly List<string> focusableControlNames = new List<string> { "title", "colorTextfields_0", "colorTextfields_1", "colorTextfields_2", "colorTextfields_3", "colorTextfields_4" };

	private const int ColorWheelSize = 128;

	private const int ColorTextfieldsWidth = 125;

	private const int PaletteColumns = 9;

	private const int ColorIconSize = 22;

	private const int ColorIconPadding = 2;

	private const int CurrentColorLabelWidth = 100;

	private const int PaletteWidth = 250;

	private const int ColorTemperatureBarHeight = 34;

	private CompGlower glower;

	private CompGlower[] extraGlowers;

	private Color color;

	private Color oldColor;

	private bool hsvColorWheelDragging;

	private bool colorTemperatureDragging;

	private string[] textfieldBuffers = new string[6];

	private Color textfieldColorBuffer;

	private string previousFocusedControlName;

	private Widgets.ColorComponents visibleTextfields;

	private Widgets.ColorComponents editableTextfields;

	protected static readonly Vector2 ButSize = new Vector2(150f, 38f);

	public override Vector2 InitialSize => new Vector2(600f, 450f);

	public bool ShowDarklight { get; set; } = true;

	public Dialog_GlowerColorPicker(CompGlower glower, IList<CompGlower> extraGlowers, Widgets.ColorComponents visibleTextfields, Widgets.ColorComponents editableTextfields)
	{
		this.glower = glower;
		this.extraGlowers = new CompGlower[extraGlowers.Count];
		extraGlowers.CopyTo(this.extraGlowers, 0);
		Color.RGBToHSV(glower.GlowColor.ToColor, out var H, out var S, out var _);
		color = Color.HSVToRGB(H, S, 1f);
		oldColor = color;
		this.visibleTextfields = visibleTextfields;
		this.editableTextfields = editableTextfields;
		forcePause = true;
		absorbInputAroundWindow = true;
		closeOnClickedOutside = true;
		closeOnAccept = false;
	}

	private static void HeaderRow(ref RectDivider layout)
	{
		using (new TextBlock(GameFont.Medium))
		{
			TaggedString taggedString = "ChooseAColor".Translate().CapitalizeFirst();
			RectDivider rectDivider = layout.NewRow(Text.CalcHeight(taggedString, layout.Rect.width), VerticalJustification.Top, null);
			GUI.SetNextControlName(focusableControlNames[0]);
			Rect rect = rectDivider.Rect;
			rect.y -= 5f;
			Widgets.Label(rect, taggedString);
		}
	}

	private void BottomButtons(ref RectDivider layout)
	{
		RectDivider rectDivider = layout.NewRow(ButSize.y, VerticalJustification.Bottom, null);
		if (Widgets.ButtonText(rectDivider.NewCol(ButSize.x, HorizontalJustification.Left, null), "Cancel".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Close();
		}
		if (Widgets.ButtonText(rectDivider.NewCol(ButSize.x, HorizontalJustification.Right, null), "Accept".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Color.RGBToHSV(color, out var H, out var S, out var _);
			ColorInt glowColor = glower.GlowColor;
			glowColor.SetHueSaturation(H, S);
			glower.GlowColor = glowColor;
			CompGlower[] array = extraGlowers;
			foreach (CompGlower obj in array)
			{
				glowColor = obj.GlowColor;
				glowColor.SetHueSaturation(H, S);
				obj.GlowColor = glowColor;
			}
			Close();
		}
	}

	private static void ColorPalette(ref RectDivider layout, ref Color color, Color defaultColor, bool showDarklight, out float paletteHeight)
	{
		using (new TextBlock(TextAnchor.MiddleLeft))
		{
			RectDivider rectDivider = layout;
			RectDivider rectDivider2 = rectDivider.NewCol(250f, HorizontalJustification.Right, null);
			int num = 26;
			RectDivider rectDivider3 = rectDivider2.NewRow(num, VerticalJustification.Top, null);
			int num2 = 4;
			rectDivider3.Rect.SplitVertically(num2 * (num + 2), out var left, out var right);
			RectDivider rectDivider4 = new RectDivider(left, 195906069, new Vector2(10f, 2f));
			Widgets.ColorBox(rectDivider4.NewCol(num, HorizontalJustification.Left, null), ref color, defaultColor);
			Widgets.Label(rectDivider4, "Default".Translate().CapitalizeFirst());
			RectDivider rectDivider5 = new RectDivider(right, 195906069, new Vector2(10f, 2f));
			Color defaultDarklight = DarklightUtility.DefaultDarklight;
			Rect rect = rectDivider5.NewCol(num, HorizontalJustification.Left, null);
			if (showDarklight)
			{
				Widgets.ColorBox(rect, ref color, defaultDarklight);
				Widgets.Label(rectDivider5, "Darklight".Translate().CapitalizeFirst());
			}
			Widgets.ColorSelector(rectDivider2, ref color, colors, out paletteHeight);
			paletteHeight += num + 2;
		}
	}

	private void ColorTextfields(ref RectDivider layout, out Vector2 size)
	{
		RectAggregator aggregator = new RectAggregator(new Rect(layout.Rect.position, new Vector2(125f, 0f)), 195906069, null);
		bool num = Widgets.ColorTextfields(ref aggregator, ref color, ref textfieldBuffers, ref textfieldColorBuffer, previousFocusedControlName, "colorTextfields", editableTextfields, visibleTextfields);
		size = aggregator.Rect.size;
		if (num)
		{
			Color.RGBToHSV(color, out var H, out var S, out var _);
			color = Color.HSVToRGB(H, S, 1f);
		}
	}

	private static void ColorReadback(Rect rect, Color color, Color oldColor, bool showDarklight)
	{
		rect.SplitVertically((rect.width - 26f) / 2f, out var left, out var right);
		RectDivider rectDivider = new RectDivider(left, 195906069, null);
		TaggedString label = "CurrentColor".Translate().CapitalizeFirst();
		TaggedString label2 = "OldColor".Translate().CapitalizeFirst();
		float width = Mathf.Max(100f, label.GetWidthCached(), label2.GetWidthCached());
		RectDivider rectDivider2 = rectDivider.NewRow(Text.LineHeight, VerticalJustification.Top, null);
		Widgets.Label(rectDivider2.NewCol(width, HorizontalJustification.Left, null), label);
		Widgets.DrawBoxSolid(rectDivider2, color);
		RectDivider rectDivider3 = rectDivider.NewRow(Text.LineHeight, VerticalJustification.Top, null);
		Widgets.Label(rectDivider3.NewCol(width, HorizontalJustification.Left, null), label2);
		Widgets.DrawBoxSolid(rectDivider3, oldColor);
		RectDivider rectDivider4 = new RectDivider(right, 195906069, null);
		rectDivider4.NewCol(26f, HorizontalJustification.Left, null);
		if (showDarklight)
		{
			if (DarklightUtility.IsDarklight(color))
			{
				Widgets.Label(rectDivider4, "Darklight".Translate().CapitalizeFirst());
			}
			else
			{
				Widgets.Label(rectDivider4, "NotDarklight".Translate().CapitalizeFirst());
			}
		}
	}

	private static void TabControl()
	{
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
		{
			bool num = !Event.current.shift;
			Event.current.Use();
			string text = GUI.GetNameOfFocusedControl();
			if (text.NullOrEmpty())
			{
				text = focusableControlNames[0];
			}
			int num2 = focusableControlNames.IndexOf(text);
			if (num2 < 0)
			{
				num2 = focusableControlNames.Count;
			}
			num2 = ((!num) ? (num2 - 1) : (num2 + 1));
			if (num2 >= focusableControlNames.Count)
			{
				num2 = 0;
			}
			else if (num2 < 0)
			{
				num2 = focusableControlNames.Count - 1;
			}
			GUI.FocusControl(focusableControlNames[num2]);
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		using (TextBlock.Default())
		{
			RectDivider layout = new RectDivider(inRect, 195906069, null);
			HeaderRow(ref layout);
			layout.NewRow(0f, VerticalJustification.Top, null);
			BottomButtons(ref layout);
			layout.NewRow(0f, VerticalJustification.Bottom, null);
			Color.RGBToHSV(glower.Props.glowColor.ToColor, out var H, out var S, out var _);
			Color defaultColor = Color.HSVToRGB(H, S, 1f);
			defaultColor.a = 1f;
			ColorPalette(ref layout, ref color, defaultColor, ShowDarklight, out var paletteHeight);
			ColorTextfields(ref layout, out var size);
			float height = Mathf.Max(paletteHeight, 128f, size.y);
			RectDivider rectDivider = layout.NewRow(height, VerticalJustification.Top, null);
			rectDivider.NewCol(size.x, HorizontalJustification.Left, null);
			rectDivider.NewCol(250f, HorizontalJustification.Right, null);
			Widgets.HSVColorWheel(rectDivider.Rect.ContractedBy((rectDivider.Rect.width - 128f) / 2f, (rectDivider.Rect.height - 128f) / 2f), ref color, ref hsvColorWheelDragging, 1f);
			layout.NewRow(10f, VerticalJustification.Top, null);
			Widgets.ColorTemperatureBar(layout.NewRow(34f, VerticalJustification.Top, null), ref color, ref colorTemperatureDragging, 1f);
			layout.NewRow(26f, VerticalJustification.Top, null);
			ColorReadback(layout, color, oldColor, ShowDarklight);
			TabControl();
			if (Event.current.type == EventType.Layout)
			{
				previousFocusedControlName = GUI.GetNameOfFocusedControl();
			}
		}
	}
}
