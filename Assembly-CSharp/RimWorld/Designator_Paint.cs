using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public abstract class Designator_Paint : DesignatorWithEyedropper
{
	protected ColorDef colorDef;

	private string cachedAttachmentString;

	private static IEnumerable<ColorDef> Colors = from x in DefDatabase<ColorDef>.AllDefs
		where x.colorType == ColorType.Structure
		orderby x.displayOrder
		select x;

	protected abstract Texture2D IconTopTex { get; }

	public override int DraggableDimensions
	{
		get
		{
			if (!eyedropMode)
			{
				return 2;
			}
			return eyedropper.DraggableDimensions;
		}
	}

	public override Color IconDrawColor => colorDef.color;

	public override bool DragDrawMeasurements => true;

	private string AttachmentString
	{
		get
		{
			if (cachedAttachmentString == null)
			{
				cachedAttachmentString = "Paint".Translate() + ": " + colorDef.LabelCap + "\n" + KeyBindingDefOf.ShowEyedropper.MainKeyLabel + ": " + "GrabExistingColor".Translate();
			}
			return cachedAttachmentString;
		}
	}

	public Designator_Paint()
	{
		colorDef = Colors.FirstOrDefault();
		soundDragSustain = SoundDefOf.Designate_DragStandard;
		soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
		useMouseIcon = true;
		soundSucceeded = SoundDefOf.Designate_Paint;
		hotKey = KeyBindingDefOf.Misc6;
		eyedropper = new Designator_Eyedropper(delegate(ColorDef newCol)
		{
			colorDef = newCol;
			cachedAttachmentString = null;
			if (!eyedropMode)
			{
				Find.DesignatorManager.Select(this);
			}
		}, "SelectAPaintedBuilding".Translate(), "DesignatorEyeDropperDesc_Paint".Translate());
	}

	public override void ProcessInput(Event ev)
	{
		if (!CheckCanInteract())
		{
			return;
		}
		List<FloatMenuGridOption> list = new List<FloatMenuGridOption>();
		list.Add(new FloatMenuGridOption(Designator_Eyedropper.EyeDropperTex, delegate
		{
			base.ProcessInput(ev);
			Find.DesignatorManager.Select(eyedropper);
		}, null, "DesignatorEyeDropperDesc_Paint".Translate()));
		foreach (ColorDef color in Colors)
		{
			ColorDef newCol = color;
			list.Add(new FloatMenuGridOption(BaseContent.WhiteTex, delegate
			{
				base.ProcessInput(ev);
				Find.DesignatorManager.Select(this);
				colorDef = newCol;
				cachedAttachmentString = null;
			}, newCol.color, newCol.LabelCap));
		}
		Find.WindowStack.Add(new FloatMenuGrid(list));
		Find.DesignatorManager.Select(this);
	}

	public override void DrawMouseAttachments()
	{
		eyedropMode = KeyBindingDefOf.ShowEyedropper.IsDown;
		if (eyedropMode)
		{
			eyedropper.DrawMouseAttachments();
			return;
		}
		if (useMouseIcon)
		{
			GenUI.DrawMouseAttachment(icon, AttachmentString, iconAngle, iconOffset, null, drawTextBackground: false, default(Color), colorDef.color, delegate(Rect r)
			{
				GUI.DrawTexture(r, IconTopTex);
			});
		}
		if (Find.DesignatorManager.Dragger.Dragging)
		{
			Vector2 vector = Event.current.mousePosition + Designator_Place.PlaceMouseAttachmentDrawOffset;
			if (useMouseIcon)
			{
				vector.y += 32f + Text.LineHeight * 2f;
			}
			Widgets.ThingIcon(new Rect(vector.x, vector.y, 27f, 27f), ThingDefOf.Dye, null, null, 1f, null, null);
			int num = NumHighlightedCells();
			string text = num.ToStringCached();
			if (base.Map.resourceCounter.GetCount(ThingDefOf.Dye) < num)
			{
				GUI.color = Color.red;
				text += " (" + "NotEnoughStoredLower".Translate() + ")";
			}
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(vector.x + 29f, vector.y, 999f, 29f), text);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}

	public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
	{
		base.DrawIcon(rect, buttonMat, parms);
		Widgets.DrawTextureFitted(rect, IconTopTex, iconDrawScale * 0.85f, iconProportions, iconTexCoords, iconAngle, buttonMat);
	}

	protected abstract int NumHighlightedCells();
}
