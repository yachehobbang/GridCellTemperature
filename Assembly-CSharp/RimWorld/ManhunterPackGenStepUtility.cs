using Verse;

namespace RimWorld;

public static class ManhunterPackGenStepUtility
{
	public static bool TryGetAnimalsKind(float points, int tile, out PawnKindDef animalKind)
	{
		if (!AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(points, tile, out animalKind))
		{
			return AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(points, -1, out animalKind);
		}
		return true;
	}
}
