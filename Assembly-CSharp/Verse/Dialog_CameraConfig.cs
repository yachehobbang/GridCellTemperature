using System;
using UnityEngine;

namespace Verse;

[StaticConstructorOnStartup]
public class Dialog_CameraConfig : Window
{
	private static readonly FloatRange MoveScaleFactorRange = new FloatRange(0f, 2f);

	private static readonly FloatRange ZoomScaleFactorRange = new FloatRange(0.1f, 10f);

	private const float SliderHeight = 30f;

	private static readonly Texture2D ArrowTex = ContentFinder<Texture2D>.Get("UI/Overlays/TutorArrowRight");

	public override Vector2 InitialSize => new Vector2(260f, 300f);

	private CameraMapConfig Config => Find.CameraDriver.config;

	protected override float Margin => 4f;

	public Dialog_CameraConfig()
	{
		closeOnAccept = false;
		closeOnCancel = false;
		draggable = true;
		layer = WindowLayer.Super;
		doCloseX = true;
		onlyOneOfTypeAllowed = true;
		preventCameraMotion = false;
		focusWhenOpened = false;
		drawShadow = false;
		drawInScreenshotMode = false;
		Reset();
	}

	public override void DoWindowContents(Rect rect)
	{
		Text.Font = GameFont.Small;
		Widgets.Label(new Rect(4f, 0f, rect.width, 30f), "Camera config");
		Rect rect2 = new Rect(4f, 36f, rect.width - 8f, 30f);
		Widgets.HorizontalSlider(rect2, ref Config.moveSpeedScale, MoveScaleFactorRange, "Pan speed " + Config.moveSpeedScale, 0.005f);
		rect2.y += 36f;
		Widgets.HorizontalSlider(rect2, ref Config.zoomSpeed, ZoomScaleFactorRange, "Zoom speed " + Config.zoomSpeed, 0.1f);
		rect2.y += 36f;
		Widgets.FloatRange(rect2, GetHashCode(), ref Config.sizeRange, 0f, 100f, "ZoomRange", ToStringStyle.FloatOne, 1f, GameFont.Small, null);
		rect2.y += 36f;
		bool checkOn = Config.zoomPreserveFactor > 0f;
		Widgets.CheckboxLabeled(rect2, "Continuous zoom", ref checkOn);
		Config.zoomPreserveFactor = (checkOn ? 1f : 0f);
		rect2.y += 30f;
		Widgets.CheckboxLabeled(rect2, "Smooth zoom", ref Config.smoothZoom);
		rect2.y += 30f;
		Widgets.CheckboxLabeled(rect2, "Follow selected pawns", ref Config.followSelected);
		rect2.y += 30f;
		Widgets.CheckboxLabeled(rect2, "Auto pan while paused", ref Config.autoPanWhilePaused);
		Rect position = new Rect(4f, rect2.yMax, rect.width - 8f, 9999f);
		float num = 0f;
		GUI.BeginGroup(position);
		Rect rect3 = new Rect((rect.width - 8f) / 2f - 15f, 0f, 30f, 30f);
		Widgets.DrawTextureRotated(rect3.center, ArrowTex, (0f - Config.autoPanTargetAngle) * 57.29578f, 0.4f);
		Rect rect4 = new Rect(0f, rect3.yMax + 3f, rect.width - 8f, 30f);
		float autoPanTargetAngle = Config.autoPanTargetAngle;
		autoPanTargetAngle = Widgets.HorizontalSlider(rect4, autoPanTargetAngle, 0f, (float)Math.PI * 2f, middleAlignment: false, "Auto pan angle " + (autoPanTargetAngle * 57.29578f).ToString("F0") + "°", "0°", "360°", 0.01f);
		if (autoPanTargetAngle != Config.autoPanTargetAngle)
		{
			Config.autoPanTargetAngle = (Config.autoPanAngle = autoPanTargetAngle);
		}
		num = rect4.yMax;
		Rect rect5 = new Rect(0f, num + 6f, rect.width - 8f, 30f);
		float autoPanSpeed = Config.autoPanSpeed;
		autoPanSpeed = Widgets.HorizontalSlider(rect5, autoPanSpeed, 0f, 5f, middleAlignment: false, "Auto pan speed " + Config.autoPanSpeed, "0", "10", 0.05f);
		if (autoPanSpeed != Config.autoPanSpeed)
		{
			Config.autoPanSpeed = autoPanSpeed;
		}
		num = rect5.yMax;
		GUI.EndGroup();
		Rect rect6 = new Rect(0f, rect2.yMax + num + 10f, rect.width, 30f);
		Rect rect7 = rect6;
		rect7.xMax = rect6.width / 3f;
		if (Widgets.ButtonText(rect7, "Reset", drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Reset();
		}
		rect7.x += rect6.width / 3f;
		if (Widgets.ButtonText(rect7, "Save", drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Find.WindowStack.Add(new Dialog_CameraConfigList_Save(Config));
		}
		rect7.x += rect6.width / 3f;
		if (Widgets.ButtonText(rect7, "Load", drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Find.WindowStack.Add(new Dialog_CameraConfigList_Load(delegate(CameraMapConfig c)
			{
				Config.moveSpeedScale = c.moveSpeedScale;
				Config.zoomSpeed = c.zoomSpeed;
				Config.sizeRange = c.sizeRange;
				Config.zoomPreserveFactor = c.zoomPreserveFactor;
				Config.smoothZoom = c.smoothZoom;
				Config.followSelected = c.followSelected;
				Config.autoPanTargetAngle = (Config.autoPanAngle = c.autoPanTargetAngle);
				Config.autoPanSpeed = c.autoPanSpeed;
				Config.fileName = c.fileName;
				Config.autoPanWhilePaused = c.autoPanWhilePaused;
			}));
		}
		if (Event.current.type == EventType.Layout)
		{
			windowRect.height = rect6.yMax + Margin * 2f;
		}
	}

	private void Reset()
	{
		Find.CameraDriver.config = new CameraMapConfig_Normal();
	}

	protected override void SetInitialSizeAndPosition()
	{
		Vector2 initialSize = InitialSize;
		windowRect = new Rect(5f, 5f, initialSize.x, initialSize.y).Rounded();
	}
}
