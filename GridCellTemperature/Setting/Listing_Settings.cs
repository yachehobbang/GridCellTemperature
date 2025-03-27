using System;
using System.Globalization;
using UnityEngine;
using Verse;

namespace GridCellTemperature
{
	public class Listing_Settings : Listing_Standard
	{
		private const float ScrollAreaWidth = 24f;

		public void BeginScrollView(Rect rect, ref Vector2 scrollPosition, ref Rect viewRect)
		{
			if (viewRect == default)
			{
				viewRect = new Rect(rect.x, rect.y, rect.width - 24f, 99999f);
			}
			Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
			this.Begin(viewRect);
		}

		public void EndScrollView(ref Rect viewRect)
		{
			End();
			Widgets.EndScrollView();
			viewRect.height = CurHeight;
		}

		public void SliderLabeled(
		  string label,
		  ref float value,	
		  float min,
		  float max,
		  float roundTo = -1f,
		  string display = null)
		{
			Rect rect = ((Listing)this).GetRect(Text.LineHeight, 1f);
			Widgets.Label(GenUI.LeftHalf(rect), label);
			var anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(GenUI.RightHalf(rect), display ?? value.ToString(CultureInfo.InvariantCulture));
			Text.Anchor = anchor;
			value = Slider(value, min, max);
			if ((double)roundTo <= 0.0)
				return;
			value = Mathf.Round(value / roundTo) * roundTo;
		}

		public void SliderLabeled(
		  string label,
		  ref int value,
		  int min,
		  int max,
		  int roundTo = -1,
		  string display = null)
		{
			float num = (float)value;
			this.SliderLabeled(label, ref num, (float)min, (float)max, (float)roundTo, display);
			value = (int)num;
		}
	}
}
