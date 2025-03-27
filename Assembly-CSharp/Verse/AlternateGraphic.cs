using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace Verse;

public class AlternateGraphic
{
	private float weight = 0.5f;

	private string texPath;

	private string dessicatedTexPath;

	private Color? color;

	private Color? colorTwo;

	public GraphicData graphicData;

	public GraphicData dessicatedGraphicData;

	public List<AttachPoint> attachPoints;

	public float Weight => weight;

	public Graphic GetGraphic(Graphic other)
	{
		if (graphicData == null)
		{
			graphicData = new GraphicData();
		}
		graphicData.CopyFrom(other.data);
		if (!texPath.NullOrEmpty())
		{
			graphicData.texPath = texPath;
		}
		graphicData.color = color ?? other.color;
		graphicData.colorTwo = colorTwo ?? other.colorTwo;
		return graphicData.Graphic;
	}

	public Graphic GetDessicatedGraphic(Graphic other)
	{
		if (dessicatedGraphicData == null)
		{
			dessicatedGraphicData = new GraphicData();
		}
		dessicatedGraphicData.CopyFrom(other.data);
		if (!dessicatedTexPath.NullOrEmpty())
		{
			dessicatedGraphicData.texPath = dessicatedTexPath;
		}
		return dessicatedGraphicData.Graphic;
	}
}
