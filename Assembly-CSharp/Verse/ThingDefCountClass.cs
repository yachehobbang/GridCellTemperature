using System.Xml;
using RimWorld;
using UnityEngine;

namespace Verse;

public sealed class ThingDefCountClass : IExposable
{
	public ThingDef thingDef;

	public int count = 1;

	public Color? color;

	public float? chance;

	public ThingDef stuff;

	public QualityCategory quality = QualityCategory.Normal;

	public string Label => GenLabel.ThingLabel(thingDef, null, count);

	public string LabelCap => Label.CapitalizeFirst(thingDef);

	public string Summary => count + "x " + ((thingDef != null) ? thingDef.label : "null");

	public float DropChance => chance ?? 1f;

	public bool IsChanceBased => chance.HasValue;

	public ThingDefCountClass()
	{
	}

	public ThingDefCountClass(ThingDef thingDef, int count)
	{
		if (count < 0)
		{
			Log.Warning("Tried to set ThingDefCountClass count to " + count + ". thingDef=" + thingDef);
			count = 0;
		}
		this.thingDef = thingDef;
		this.count = count;
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref thingDef, "thingDef");
		Scribe_Defs.Look(ref stuff, "stuff");
		Scribe_Values.Look(ref count, "count", 1);
		Scribe_Values.Look(ref quality, "quality", QualityCategory.Awful);
		Scribe_Values.Look(ref color, "color", null);
		Scribe_Values.Look(ref chance, "chance", null);
	}

	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		int num = xmlRoot.ChildNodes.Count;
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingDef", xmlRoot.Name);
		if (num == 1)
		{
			LoadFromSingleNode(xmlRoot.FirstChild);
		}
		else if (num > 1)
		{
			LoadMultipleNodes(xmlRoot);
		}
	}

	private void LoadFromSingleNode(XmlNode node)
	{
		if (node is XmlText xmlText)
		{
			count = ParseHelper.FromString<int>(xmlText.InnerText);
		}
		else if (node is XmlElement element)
		{
			ParseXmlElement(element);
		}
	}

	private void LoadMultipleNodes(XmlNode xmlRoot)
	{
		foreach (object childNode in xmlRoot.ChildNodes)
		{
			ParseXmlElement(childNode as XmlElement);
		}
	}

	private void ParseXmlElement(XmlElement element)
	{
		if (element.Name == "count")
		{
			count = ParseHelper.FromString<int>(element.InnerText);
		}
		else if (element.Name == "quality")
		{
			quality = ParseHelper.FromString<QualityCategory>(element.InnerText);
		}
		else if (element.Name == "color")
		{
			color = ParseHelper.FromString<Color>(element.InnerText);
		}
		else if (element.Name == "chance")
		{
			chance = ParseHelper.FromString<float>(element.InnerText);
		}
		else if (element.Name == "stuff")
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stuff", element.InnerText);
		}
	}

	public override string ToString()
	{
		return "(" + count + "x " + ((thingDef != null) ? thingDef.defName : "null") + ")";
	}

	public override int GetHashCode()
	{
		return thingDef.shortHash + count << 16;
	}

	public IngredientCount ToIngredientCount()
	{
		IngredientCount ingredientCount = new IngredientCount();
		ingredientCount.SetBaseCount(count);
		ingredientCount.filter.SetAllow(thingDef, allow: true);
		return ingredientCount;
	}

	public static implicit operator ThingDefCountClass(ThingDefCount t)
	{
		return new ThingDefCountClass(t.ThingDef, t.Count);
	}
}
