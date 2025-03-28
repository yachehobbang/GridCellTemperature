using UnityEngine;

namespace Verse;

[StaticConstructorOnStartup]
public static class TexUI
{
	public static readonly Texture2D TitleBGTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.05f));

	public static readonly Texture2D HighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.1f));

	public static readonly Texture2D HighlightSelectedTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.94f, 0.5f, 0.18f));

	public static readonly Texture2D ArrowTexRight = ContentFinder<Texture2D>.Get("UI/Widgets/ArrowRight");

	public static readonly Texture2D ArrowTexLeft = ContentFinder<Texture2D>.Get("UI/Widgets/ArrowLeft");

	public static readonly Texture2D WinExpandWidget = ContentFinder<Texture2D>.Get("UI/Widgets/WinExpandWidget");

	public static readonly Texture2D ArrowTex = ContentFinder<Texture2D>.Get("UI/Misc/AlertFlashArrow");

	public static readonly Texture2D RotLeftTex = ContentFinder<Texture2D>.Get("UI/Widgets/RotLeft");

	public static readonly Texture2D RotRightTex = ContentFinder<Texture2D>.Get("UI/Widgets/RotRight");

	public static readonly Texture2D GuiltyTex = ContentFinder<Texture2D>.Get("UI/Icons/Guilty");

	public static readonly Texture2D CopyTex = ContentFinder<Texture2D>.Get("UI/Buttons/Copy");

	public static readonly Texture2D DismissTex = ContentFinder<Texture2D>.Get("UI/Buttons/Dismiss");

	public static readonly Texture2D RenameTex = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

	public static readonly Texture2D RectHighlight = ContentFinder<Texture2D>.Get("UI/Overlays/HighlightAtlas");

	public static readonly Texture2D GrayBg = SolidColorMaterials.NewSolidColorTexture(new ColorInt(51, 63, 51, 200).ToColor);

	public static readonly Texture2D DotHighlight = ContentFinder<Texture2D>.Get("UI/Overlays/DotHighlight");

	public static readonly Texture2D SelectionBracketWhole = ContentFinder<Texture2D>.Get("UI/Overlays/SelectionBracketWhole");

	public static readonly Color OldActiveResearchColor = new ColorInt(0, 64, 64, 255).ToColor;

	public static readonly Color OldFinishedResearchColor = new ColorInt(0, 64, 16, 255).ToColor;

	public static readonly Color AvailResearchColor = new ColorInt(32, 32, 32, 255).ToColor;

	public static readonly Color ActiveResearchColor = new ColorInt(81, 66, 7, 255).ToColor;

	public static readonly Color OtherActiveResearchColor = new ColorInt(78, 109, 129, 130).ToColor;

	public static readonly Color FinishedResearchColor = new ColorInt(0, 64, 64, 255).ToColor;

	public static readonly Color LockedResearchColor = new ColorInt(42, 42, 42, 255).ToColor;

	public static readonly Color HiddenResearchColor = new ColorInt(42, 42, 42, 255).ToColor;

	public static readonly Color HighlightBgResearchColor = new ColorInt(30, 30, 30, 255).ToColor;

	public static readonly Color HighlightBorderResearchColor = new ColorInt(160, 160, 160, 255).ToColor;

	public static readonly Color BorderResearchSelectedColor = new ColorInt(240, 240, 240, 255).ToColor;

	public static readonly Color BorderResearchingColor = new ColorInt(253, 225, 114, 255).ToColor;

	public static readonly Color DefaultBorderResearchColor = new ColorInt(80, 80, 80, 255).ToColor;

	public static readonly Color ResearchMainTabColor = new Color(0.2f, 0.8f, 0.85f);

	public static readonly Color FinishedResearchColorTransparent = new ColorInt(78, 109, 129, 140).ToColor;

	public static readonly Color DefaultLineResearchColor = new ColorInt(60, 60, 60, 255).ToColor;

	public static readonly Color HighlightLineResearchColor = new ColorInt(51, 205, 217, 255).ToColor;

	public static readonly Color DependencyOutlineResearchColor = new ColorInt(217, 20, 51, 255).ToColor;

	public static readonly Texture2D FastFillTex = Texture2D.whiteTexture;

	public static readonly GUIStyle FastFillStyle = new GUIStyle
	{
		normal = new GUIStyleState
		{
			background = FastFillTex
		}
	};

	public static readonly Texture2D TextBGBlack = ContentFinder<Texture2D>.Get("UI/Widgets/TextBGBlack");

	public static readonly Texture2D GrayTextBG = ContentFinder<Texture2D>.Get("UI/Overlays/GrayTextBG");

	public static readonly Texture2D FloatMenuOptionBG = ContentFinder<Texture2D>.Get("UI/Widgets/FloatMenuOptionBG");

	public static readonly Material GrayscaleGUI = MatLoader.LoadMat("Misc/GrayscaleGUI");

	public static Texture2D SteamDeck_ButtonA = ContentFinder<Texture2D>.Get("UI/Icons/SteamDeck/button_a");
}
