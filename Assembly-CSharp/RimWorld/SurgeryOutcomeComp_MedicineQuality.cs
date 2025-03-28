using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class SurgeryOutcomeComp_MedicineQuality : SurgeryOutcomeComp_Curve
{
	protected override float XGetter(RecipeDef recipe, Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part, Bill bill)
	{
		ThingDef thingDef = null;
		if (bill is Bill_Medical bill_Medical)
		{
			thingDef = bill_Medical.consumedInitialMedicineDef;
		}
		int num = 0;
		float num2 = 0f;
		if (thingDef != null)
		{
			num++;
			num2 += thingDef.GetStatValueAbstract(StatDefOf.MedicalPotency);
		}
		for (int i = 0; i < ingredients.Count; i++)
		{
			if (ingredients[i] is Medicine medicine)
			{
				num += medicine.stackCount;
				num2 += medicine.GetStatValue(StatDefOf.MedicalPotency) * (float)medicine.stackCount;
			}
		}
		if (num == 0)
		{
			return 1f;
		}
		return num2 / (float)num;
	}
}
