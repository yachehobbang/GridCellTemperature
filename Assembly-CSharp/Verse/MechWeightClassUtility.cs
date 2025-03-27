using System;

namespace Verse;

public static class MechWeightClassUtility
{
	public static string ToStringHuman(this MechWeightClass wc)
	{
		return wc switch
		{
			MechWeightClass.Light => "MechWeightClass_Light".Translate(), 
			MechWeightClass.Medium => "MechWeightClass_Medium".Translate(), 
			MechWeightClass.Heavy => "MechWeightClass_Heavy".Translate(), 
			MechWeightClass.UltraHeavy => "MechWeightClass_Ultraheavy".Translate(), 
			_ => throw new Exception("Unknown mech weight class " + wc), 
		};
	}
}
