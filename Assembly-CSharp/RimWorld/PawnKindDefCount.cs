using System.Xml;
using Verse;

namespace RimWorld;

public class PawnKindDefCount
{
	public PawnKindDef kindDef;

	public int count;

	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "kindDef", xmlRoot.Name);
		count = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
	}
}
