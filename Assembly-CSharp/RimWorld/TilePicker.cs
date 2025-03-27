using System;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

[StaticConstructorOnStartup]
public class TilePicker
{
	private static readonly Vector2 ButtonSize = new Vector2(150f, 38f);

	private const int Padding = 8;

	private const int BottomPanelYOffset = -50;

	private Func<int, bool> validator;

	private bool allowEscape;

	private bool active;

	private Action<int> tileChosen;

	private Action noTileChosen;

	private string title;

	public bool Active => active;

	public bool AllowEscape => allowEscape;

	public void StartTargeting(Func<int, bool> validator, Action<int> tileChosen, bool allowEscape = true, Action noTileChosen = null, string title = null)
	{
		this.validator = validator;
		this.allowEscape = allowEscape;
		this.noTileChosen = noTileChosen;
		this.tileChosen = tileChosen;
		this.title = title;
		Find.WorldSelector.ClearSelection();
		active = true;
	}

	public void StopTargeting()
	{
		if (active && noTileChosen != null)
		{
			noTileChosen();
		}
		StopTargetingInt();
	}

	private void StopTargetingInt()
	{
		active = false;
	}

	public void TileSelectorOnGUI()
	{
		if (!title.NullOrEmpty())
		{
			Text.Font = GameFont.Medium;
			Vector2 vector = Text.CalcSize(title);
			Widgets.Label(new Rect((float)UI.screenWidth / 2f - vector.x / 2f, 4f, vector.x + 4f, vector.y), title);
			Text.Font = GameFont.Small;
		}
		Vector2 buttonSize = ButtonSize;
		int num = 24;
		Rect rect = new Rect((float)UI.screenWidth / 2f - 2f * buttonSize.x / 2f - (float)num / 2f, (float)UI.screenHeight - (buttonSize.y + 8f) + -50f, 2f * buttonSize.x + (float)num, buttonSize.y + 16f);
		Widgets.DrawWindowBackground(rect);
		if (Widgets.ButtonText(new Rect(rect.x + 8f, rect.y + 8f, buttonSize.x, buttonSize.y), "SelectRandomSite".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			Find.WorldInterface.SelectedTile = TileFinder.RandomStartingTile();
			Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
		}
		if (Widgets.ButtonText(new Rect(rect.x + 16f + buttonSize.x, rect.y + 8f, buttonSize.x, buttonSize.y), "Next".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			int selectedTile = Find.WorldInterface.SelectedTile;
			if (selectedTile < 0)
			{
				Messages.Message("MustSelectStartingSite".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			else if (validator(selectedTile))
			{
				StopTargetingInt();
				tileChosen(selectedTile);
				Event.current.Use();
			}
		}
		if (KeyBindingDefOf.Cancel.KeyDownEvent && Active && !allowEscape)
		{
			Event.current.Use();
		}
	}
}
