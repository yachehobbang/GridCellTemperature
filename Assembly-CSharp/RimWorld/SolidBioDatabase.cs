using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class SolidBioDatabase
{
	public static List<PawnBio> allBios = new List<PawnBio>();

	public static void Clear()
	{
		allBios.Clear();
		PawnNameDatabaseSolid.Clear();
	}

	public static void LoadAllBios()
	{
		foreach (PawnBio item in DirectXmlLoader.LoadXmlDataInResourcesFolder<PawnBio>("Backstories/Solid"))
		{
			DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
			item.name.ResolveMissingPieces();
			if (item.childhood == null || item.adulthood == null)
			{
				PawnNameDatabaseSolid.AddPlayerContentName(item.name, item.gender);
				continue;
			}
			item.ResolveReferences();
			allBios.Add(item);
		}
	}
}
