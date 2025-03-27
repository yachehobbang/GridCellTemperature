using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace Verse;

public abstract class Designator : Command
{
	protected bool useMouseIcon;

	public bool isOrder;

	public SoundDef soundDragSustain;

	public SoundDef soundDragChanged;

	public SoundDef soundSucceeded;

	protected SoundDef soundFailed = SoundDefOf.Designate_Failed;

	protected bool hasDesignateAllFloatMenuOption;

	protected string designateAllLabel;

	protected bool showReverseDesignatorDisabledReason;

	private string cachedTutorTagSelect;

	private string cachedTutorTagDesignate;

	protected string cachedHighlightTag;

	public Map Map => Find.CurrentMap;

	public virtual int DraggableDimensions => 0;

	public virtual bool DragDrawMeasurements => false;

	public virtual bool DragDrawOutline => DraggableDimensions == 2;

	protected override bool DoTooltip => false;

	protected virtual DesignationDef Designation => null;

	public virtual float PanelReadoutTitleExtraRightMargin => 0f;

	public override string TutorTagSelect
	{
		get
		{
			if (tutorTag == null)
			{
				return null;
			}
			if (cachedTutorTagSelect == null)
			{
				cachedTutorTagSelect = "SelectDesignator-" + tutorTag;
			}
			return cachedTutorTagSelect;
		}
	}

	public string TutorTagDesignate
	{
		get
		{
			if (tutorTag == null)
			{
				return null;
			}
			if (cachedTutorTagDesignate == null)
			{
				cachedTutorTagDesignate = "Designate-" + tutorTag;
			}
			return cachedTutorTagDesignate;
		}
	}

	public override string HighlightTag
	{
		get
		{
			if (cachedHighlightTag == null && tutorTag != null)
			{
				cachedHighlightTag = "Designator-" + tutorTag;
			}
			return cachedHighlightTag;
		}
	}

	public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
	{
		get
		{
			foreach (FloatMenuOption rightClickFloatMenuOption in base.RightClickFloatMenuOptions)
			{
				yield return rightClickFloatMenuOption;
			}
			if (hasDesignateAllFloatMenuOption)
			{
				int num = 0;
				List<Thing> things = Map.listerThings.AllThings;
				for (int i = 0; i < things.Count; i++)
				{
					Thing t = things[i];
					if (!t.Fogged() && CanDesignateThing(t).Accepted)
					{
						num++;
					}
				}
				if (num > 0)
				{
					yield return new FloatMenuOption(designateAllLabel + " (" + "CountToDesignate".Translate(num) + ")", delegate
					{
						for (int j = 0; j < things.Count; j++)
						{
							Thing t2 = things[j];
							if (!t2.Fogged() && CanDesignateThing(t2).Accepted)
							{
								DesignateThing(things[j]);
							}
						}
					});
				}
				else
				{
					yield return new FloatMenuOption(designateAllLabel + " (" + "NoneLower".Translate() + ")", null);
				}
			}
			DesignationDef designationDef = Designation;
			if (Designation == null)
			{
				yield break;
			}
			int num2 = 0;
			foreach (Designation item in Map.designationManager.designationsByDef[designationDef])
			{
				if (RemoveAllDesignationsAffects(item.target))
				{
					num2++;
				}
			}
			if (num2 > 0)
			{
				yield return new FloatMenuOption((string)("RemoveAllDesignations".Translate() + " (") + num2 + ")", delegate
				{
					List<Designation> list = Map.designationManager.designationsByDef[designationDef];
					for (int num3 = list.Count - 1; num3 >= 0; num3--)
					{
						if (RemoveAllDesignationsAffects(list[num3].target))
						{
							Map.designationManager.RemoveDesignation(list[num3]);
						}
					}
				});
			}
			else
			{
				yield return new FloatMenuOption("RemoveAllDesignations".Translate() + " (" + "NoneLower".Translate() + ")", null);
			}
		}
	}

	public Designator()
	{
		activateSound = SoundDefOf.Tick_Tiny;
		designateAllLabel = "DesignateAll".Translate();
	}

	protected bool CheckCanInteract()
	{
		if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(TutorTagSelect))
		{
			return false;
		}
		return true;
	}

	public override void ProcessInput(Event ev)
	{
		if (CheckCanInteract())
		{
			base.ProcessInput(ev);
			Find.DesignatorManager.Select(this);
		}
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
		if (DebugViewSettings.showArchitectMenuOrder)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(topLeft.x, topLeft.y + 5f, GetWidth(maxWidth), 15f), Order.ToString());
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
		}
		return result;
	}

	public Command_Action CreateReverseDesignationGizmo(Thing t)
	{
		AcceptanceReport acceptanceReport = CanDesignateThing(t);
		float angle;
		Vector2 offset;
		if (acceptanceReport.Accepted || (showReverseDesignatorDisabledReason && !acceptanceReport.Reason.NullOrEmpty()))
		{
			return new Command_Action
			{
				defaultLabel = LabelCapReverseDesignating(t),
				icon = IconReverseDesignating(t, out angle, out offset),
				iconAngle = angle,
				iconOffset = offset,
				defaultDesc = (acceptanceReport.Reason.NullOrEmpty() ? DescReverseDesignating(t) : acceptanceReport.Reason),
				Order = ((this is Designator_Uninstall) ? (-11f) : (-20f)),
				Disabled = !acceptanceReport.Accepted,
				action = delegate
				{
					if (TutorSystem.AllowAction(TutorTagDesignate))
					{
						DesignateThing(t);
						Finalize(somethingSucceeded: true);
					}
				},
				hotKey = hotKey,
				groupKeyIgnoreContent = groupKeyIgnoreContent,
				groupKey = groupKey
			};
		}
		return null;
	}

	public virtual AcceptanceReport CanDesignateThing(Thing t)
	{
		return AcceptanceReport.WasRejected;
	}

	public virtual void DesignateThing(Thing t)
	{
		throw new NotImplementedException();
	}

	public abstract AcceptanceReport CanDesignateCell(IntVec3 loc);

	public virtual void DesignateMultiCell(IEnumerable<IntVec3> cells)
	{
		if (TutorSystem.TutorialMode && !TutorSystem.AllowAction(new EventPack(TutorTagDesignate, cells)))
		{
			return;
		}
		bool somethingSucceeded = false;
		bool flag = false;
		foreach (IntVec3 cell in cells)
		{
			if (CanDesignateCell(cell).Accepted)
			{
				DesignateSingleCell(cell);
				somethingSucceeded = true;
				if (!flag)
				{
					flag = ShowWarningForCell(cell);
				}
			}
		}
		Finalize(somethingSucceeded);
		if (TutorSystem.TutorialMode)
		{
			TutorSystem.Notify_Event(new EventPack(TutorTagDesignate, cells));
		}
	}

	public virtual void DesignateSingleCell(IntVec3 c)
	{
		throw new NotImplementedException();
	}

	public virtual bool ShowWarningForCell(IntVec3 c)
	{
		return false;
	}

	public void Finalize(bool somethingSucceeded)
	{
		if (somethingSucceeded)
		{
			FinalizeDesignationSucceeded();
		}
		else
		{
			FinalizeDesignationFailed();
		}
	}

	protected virtual void FinalizeDesignationSucceeded()
	{
		if (soundSucceeded != null)
		{
			soundSucceeded.PlayOneShotOnCamera();
		}
	}

	protected virtual void FinalizeDesignationFailed()
	{
		if (soundFailed != null)
		{
			soundFailed.PlayOneShotOnCamera();
		}
		if (Find.DesignatorManager.Dragger.FailureReason != null)
		{
			Messages.Message(Find.DesignatorManager.Dragger.FailureReason, MessageTypeDefOf.RejectInput, historical: false);
		}
	}

	public virtual string LabelCapReverseDesignating(Thing t)
	{
		return LabelCap;
	}

	public virtual string DescReverseDesignating(Thing t)
	{
		return Desc;
	}

	public virtual Texture2D IconReverseDesignating(Thing t, out float angle, out Vector2 offset)
	{
		angle = iconAngle;
		offset = iconOffset;
		return (Texture2D)icon;
	}

	protected virtual bool RemoveAllDesignationsAffects(LocalTargetInfo target)
	{
		return true;
	}

	public virtual void DrawMouseAttachments()
	{
		if (useMouseIcon)
		{
			GenUI.DrawMouseAttachment(icon, "", iconAngle, iconOffset, null, drawTextBackground: false, default(Color), null);
		}
	}

	public virtual void DrawPanelReadout(ref float curY, float width)
	{
	}

	public virtual void DoExtraGuiControls(float leftX, float bottomY)
	{
	}

	public virtual void SelectedUpdate()
	{
	}

	public virtual void SelectedProcessInput(Event ev)
	{
	}

	public virtual void Rotate(RotationDirection rotDir)
	{
	}

	public virtual bool CanRemainSelected()
	{
		return true;
	}

	public virtual void Selected()
	{
	}

	public virtual void Deselected()
	{
	}

	public virtual void RenderHighlight(List<IntVec3> dragCells)
	{
		DesignatorUtility.RenderHighlightOverSelectableThings(this, dragCells);
	}
}
