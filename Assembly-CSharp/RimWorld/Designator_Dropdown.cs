using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

[StaticConstructorOnStartup]
public class Designator_Dropdown : DesignatorWithEyedropper
{
	private List<Designator> elements = new List<Designator>();

	private Designator activeDesignator;

	private bool activeDesignatorSet;

	public static readonly Texture2D PlusTex = ContentFinder<Texture2D>.Get("UI/Widgets/PlusOptions");

	private const float ButSize = 16f;

	private const float ButPadding = 1f;

	public override string Label => activeDesignator.Label + (activeDesignatorSet ? "" : "...");

	public override string Desc => activeDesignator.Desc;

	public override Color IconDrawColor => activeDesignator.IconDrawColor;

	public override bool Visible
	{
		get
		{
			for (int i = 0; i < elements.Count; i++)
			{
				if (elements[i].Visible)
				{
					return true;
				}
			}
			return false;
		}
	}

	public List<Designator> Elements => elements;

	public override float PanelReadoutTitleExtraRightMargin => activeDesignator.PanelReadoutTitleExtraRightMargin;

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
		DrawExtraOptionsIcon(topLeft, GetWidth(maxWidth));
		return result;
	}

	public static void DrawExtraOptionsIcon(Vector2 topLeft, float width)
	{
		GUI.DrawTexture(new Rect(topLeft.x + width - 16f - 1f, topLeft.y + 1f, 16f, 16f), PlusTex);
	}

	public void Add(Designator des)
	{
		elements.Add(des);
		if (activeDesignator == null)
		{
			SetActiveDesignator(des, explicitySet: false);
		}
	}

	public void SetActiveDesignator(Designator des, bool explicitySet = true)
	{
		activeDesignator = des;
		icon = des.icon;
		iconDrawScale = des.iconDrawScale;
		iconProportions = des.iconProportions;
		iconTexCoords = des.iconTexCoords;
		iconAngle = des.iconAngle;
		iconOffset = des.iconOffset;
		if (explicitySet)
		{
			activeDesignatorSet = true;
		}
	}

	public override void DrawMouseAttachments()
	{
		activeDesignator.DrawMouseAttachments();
	}

	public override void ProcessInput(Event ev)
	{
		BuildableDef placingDef;
		Window window = (elements.Any((Designator x) => x is Designator_Place designator_Place && (placingDef = designator_Place.PlacingDef) != null && placingDef.designatorDropdown.useGridMenu) ? SetupGridMenu(ev) : SetupFloatMenu(ev));
		Find.WindowStack.Add(window);
		Find.DesignatorManager.Select(activeDesignator);
	}

	private Window SetupGridMenu(Event ev)
	{
		List<FloatMenuGridOption> list = new List<FloatMenuGridOption>();
		if (elements.Any((Designator x) => x is Designator_Place { PlacingDef: TerrainDef placingDef } && placingDef.designatorDropdown.includeEyeDropperTool))
		{
			if (eyedropper == null)
			{
				eyedropper = new Designator_Eyedropper(delegate(ColorDef newCol)
				{
					for (int i = 0; i < elements.Count; i++)
					{
						Designator designator = elements[i];
						if (designator is Designator_Place { PlacingDef: TerrainDef placingDef2 } && placingDef2.colorDef == newCol)
						{
							GetDesignatorSelectAction(ev, designator)();
							break;
						}
					}
				}, "SelectColoredFloor".Translate(), "DesignatorEyeDropperDesc_Carpet".Translate());
			}
			list.Add(new FloatMenuGridOption(Designator_Eyedropper.EyeDropperTex, delegate
			{
				Find.DesignatorManager.Select(eyedropper);
			}, null, "DesignatorEyeDropperDesc_Carpet".Translate()));
		}
		for (int j = 0; j < elements.Count; j++)
		{
			Designator designator2 = elements[j];
			if (!designator2.Visible)
			{
				continue;
			}
			BuildableDef placingDef3;
			if (designator2 is Designator_Place designator_Place3 && (placingDef3 = designator_Place3.PlacingDef) != null)
			{
				ThingDef designatorCost;
				if (placingDef3.designatorDropdown.iconSource == DesignatorDropdownGroupDef.IconSource.Cost && (designatorCost = GetDesignatorCost(designator2)) != null)
				{
					list.Add(new FloatMenuGridOption(Widgets.GetIconFor(designatorCost, null, null, null), GetDesignatorSelectAction(ev, designator2), null, designator2.LabelCap));
				}
				else if (placingDef3.designatorDropdown.iconSource == DesignatorDropdownGroupDef.IconSource.Placed)
				{
					FloatMenuGridOption floatMenuGridOption = new FloatMenuGridOption((Texture2D)designator_Place3.icon, GetDesignatorSelectAction(ev, designator2), designator_Place3.IconDrawColor, designator2.LabelCap);
					if (placingDef3 is TerrainDef)
					{
						floatMenuGridOption.iconTexCoords = Widgets.CroppedTerrainTextureRect((Texture2D)designator_Place3.icon);
					}
					list.Add(floatMenuGridOption);
				}
			}
			else
			{
				Log.Error("Trying to setup grid float menu with designator without icon.");
			}
		}
		return new FloatMenuGrid(list)
		{
			onCloseCallback = delegate
			{
				activeDesignatorSet = true;
			}
		};
	}

	private Window SetupFloatMenu(Event ev)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		for (int i = 0; i < elements.Count; i++)
		{
			Designator designator = elements[i];
			if (!designator.Visible)
			{
				continue;
			}
			BuildableDef placingDef;
			if (designator is Designator_Place designator_Place && (placingDef = designator_Place.PlacingDef) != null)
			{
				ThingDef designatorCost;
				if (placingDef.designatorDropdown.iconSource == DesignatorDropdownGroupDef.IconSource.Cost && (designatorCost = GetDesignatorCost(designator)) != null)
				{
					list.Add(new FloatMenuOption(designator.LabelCap, GetDesignatorSelectAction(ev, designator), designatorCost, null, forceBasicStyle: false, MenuOptionPriority.Default, null, null, 0f, null, null, playSelectionSound: true, 0, null));
					continue;
				}
				if (placingDef.designatorDropdown.iconSource == DesignatorDropdownGroupDef.IconSource.Placed)
				{
					FloatMenuOption floatMenuOption = new FloatMenuOption(designator.LabelCap, GetDesignatorSelectAction(ev, designator), (Texture2D)designator_Place.icon, designator_Place.IconDrawColor);
					if (placingDef is TerrainDef)
					{
						floatMenuOption.iconTexCoords = Widgets.CroppedTerrainTextureRect((Texture2D)designator_Place.icon);
					}
					list.Add(floatMenuOption);
					continue;
				}
			}
			list.Add(new FloatMenuOption(designator.LabelCap, GetDesignatorSelectAction(ev, designator)));
		}
		return new FloatMenu(list)
		{
			onCloseCallback = delegate
			{
				activeDesignatorSet = true;
			}
		};
	}

	private Action GetDesignatorSelectAction(Event ev, Designator des)
	{
		return delegate
		{
			base.ProcessInput(ev);
			Find.DesignatorManager.Select(des);
			SetActiveDesignator(des);
		};
	}

	public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
	{
		activeDesignator?.DrawIcon(rect, buttonMat, parms);
	}

	public override AcceptanceReport CanDesignateCell(IntVec3 loc)
	{
		return activeDesignator.CanDesignateCell(loc);
	}

	public override void SelectedUpdate()
	{
		activeDesignator.SelectedUpdate();
	}

	public override void DrawPanelReadout(ref float curY, float width)
	{
		activeDesignator.DrawPanelReadout(ref curY, width);
	}

	private ThingDef GetDesignatorCost(Designator des)
	{
		if (des is Designator_Place { PlacingDef: var placingDef } && placingDef.CostList != null && placingDef.CostList.Count > 0)
		{
			return placingDef.CostList.MaxBy((ThingDefCountClass c) => c.thingDef.BaseMarketValue * (float)c.count).thingDef;
		}
		return null;
	}
}
