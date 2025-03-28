using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class CreditsAssembler
{
	public static IEnumerable<CreditsEntry> AllCredits()
	{
		yield return new CreditRecord_Space(200f);
		yield return new CreditRecord_Title("Credits_Design".Translate());
		yield return new CreditRecord_Role("", "Tynan Sylvester");
		yield return new CreditRecord_Role("", "Will Stacey");
		yield return new CreditRecord_Role("", "Thorsten Klotz");
		yield return new CreditRecord_Title("Credits_Developers".Translate());
		yield return new CreditRecord_Role("", "Piotr Walczak");
		yield return new CreditRecord_Role("", "Tynan Sylvester");
		yield return new CreditRecord_Role("", "Igor Lebedev");
		yield return new CreditRecord_Role("", "Matt Ritchie");
		yield return new CreditRecord_Role("", "Alex Mulford");
		yield return new CreditRecord_Role("", "Kenneth Ellersdorfer");
		yield return new CreditRecord_Role("", "Joe Gasparich");
		yield return new CreditRecord_Role("", "Ben Rog-Wilhelm");
		yield return new CreditRecord_Role("", "Máté Mészáros");
		yield return new CreditRecord_Role("", "Liam Harrison");
		yield return new CreditRecord_Role("", "Matt Quail");
		yield return new CreditRecord_Role("", "Nick Barrash");
		yield return new CreditRecord_Role("", "Don Bellenger");
		yield return new CreditRecord_Role("", "Jay Lemmon");
		yield return new CreditRecord_Space(50f);
		yield return new CreditRecord_Title("Credit_MusicAndSound".Translate());
		yield return new CreditRecord_Role("", "Alistair Lindsay");
		yield return new CreditRecord_Space(50f);
		yield return new CreditRecord_Title("Credit_GameArt".Translate());
		yield return new CreditRecord_Role("", "Oskar Potocki");
		yield return new CreditRecord_Role("", "Tynan Sylvester");
		yield return new CreditRecord_Role("", "Hayden Duvall");
		yield return new CreditRecord_Role("", "Mehran Iranloo");
		yield return new CreditRecord_Role("", "Ricardo Tomé");
		yield return new CreditRecord_Role("", "Rhopunzel");
		yield return new CreditRecord_Role("", "Tamara Osborn");
		yield return new CreditRecord_Space(50f);
		yield return new CreditRecord_Title("Credit_Testers".Translate());
		yield return new CreditRecord_Role("", "Fey Nickel");
		yield return new CreditRecord_Role("", "Morg");
		yield return new CreditRecord_Role("", "ItchyFlea");
		yield return new CreditRecord_Role("", "Elliott");
		yield return new CreditRecord_Role("", "James Miura");
		yield return new CreditRecord_Space(50f);
		yield return new CreditRecord_Title("Credits_AdditionalDevelopment".Translate());
		List<CreditsEntry> list = new List<CreditsEntry>();
		list.Add(new CreditRecord_Role("", "Gavan Woolery").Compress());
		list.Add(new CreditRecord_Role("", "David 'Rez' Graham").Compress());
		list.Add(new CreditRecord_Role("", "Kay Fedewa").Compress());
		list.Add(new CreditRecord_Role("", "Jon Larson").Compress());
		list.Add(new CreditRecord_Role("", "Tia Young").Compress());
		list.Add(new CreditRecord_Role("", "Simon Warrener").Compress());
		list.Add(new CreditRecord_Role("", "Marta Fijak").Compress());
		list.Add(new CreditRecord_Role("", "Zhentar").Compress());
		list.Add(new CreditRecord_Role("", "Haplo").Compress());
		list.Add(new CreditRecord_Role("", "iame6162013").Compress());
		list.Add(new CreditRecord_Role("", "Shinzy").Compress());
		list.Add(new CreditRecord_Role("", "John Woolley").Compress());
		list.Add(new CreditRecord_Role("", "Ben Grob").Compress());
		foreach (CreditsEntry item in Reformat2Cols(list))
		{
			yield return item;
		}
		yield return new CreditRecord_Space(50f);
		yield return new CreditRecord_Title("Credits_TitleTester".Translate());
		List<CreditsEntry> list2 = new List<CreditsEntry>();
		list2.Add(new CreditRecord_Role("", "Ramsis").Compress());
		list2.Add(new CreditRecord_Role("", "Haplo").Compress());
		list2.Add(new CreditRecord_Role("", "DubskiDude").Compress());
		list2.Add(new CreditRecord_Role("", "Harry Bryant").Compress());
		list2.Add(new CreditRecord_Role("", "ChJees").Compress());
		list2.Add(new CreditRecord_Role("", "Sneaks").Compress());
		list2.Add(new CreditRecord_Role("", "AWiseCorn").Compress());
		list2.Add(new CreditRecord_Role("", "Zero747").Compress());
		list2.Add(new CreditRecord_Role("", "Mehni").Compress());
		list2.Add(new CreditRecord_Role("", "XeoNovaDan").Compress());
		list2.Add(new CreditRecord_Role("", "alphaBeta").Compress());
		list2.Add(new CreditRecord_Role("", "DubskiDude").Compress());
		list2.Add(new CreditRecord_Role("", "TheDee05").Compress());
		list2.Add(new CreditRecord_Role("", "Oglis").Compress());
		list2.Add(new CreditRecord_Role("", "Vas").Compress());
		list2.Add(new CreditRecord_Role("", "Kiaayo").Compress());
		list2.Add(new CreditRecord_Role("", "JimmyAgnt007").Compress());
		list2.Add(new CreditRecord_Role("", "Gouda Quiche").Compress());
		list2.Add(new CreditRecord_Role("", "Drb89").Compress());
		list2.Add(new CreditRecord_Role("", "Jimyoda").Compress());
		list2.Add(new CreditRecord_Role("", "Semmy").Compress());
		list2.Add(new CreditRecord_Role("", "DianaWinters").Compress());
		list2.Add(new CreditRecord_Role("", "Goldenpotatoes").Compress());
		list2.Add(new CreditRecord_Role("", "Skissor").Compress());
		list2.Add(new CreditRecord_Role("", "Laos").Compress());
		list2.Add(new CreditRecord_Role("", "Evul").Compress());
		list2.Add(new CreditRecord_Role("", "SorjaHjort").Compress());
		list2.Add(new CreditRecord_Role("", "Coenmjc").Compress());
		list2.Add(new CreditRecord_Role("", "Boris(Eqz)").Compress());
		list2.Add(new CreditRecord_Role("", "MarvinKosh").Compress());
		list2.Add(new CreditRecord_Role("", "Gaesatae").Compress());
		list2.Add(new CreditRecord_Role("", "Letharion").Compress());
		list2.Add(new CreditRecord_Role("", "HeftySmurf").Compress());
		list2.Add(new CreditRecord_Role("", "Skullywag").Compress());
		list2.Add(new CreditRecord_Role("", "Jaxxa").Compress());
		list2.Add(new CreditRecord_Role("", "Helixien").Compress());
		list2.Add(new CreditRecord_Role("", "DeeGee").Compress());
		list2.Add(new CreditRecord_Role("", "ReZpawner").Compress());
		list2.Add(new CreditRecord_Role("", "Doomdrvk").Compress());
		list2.Add(new CreditRecord_Role("", "tedvs").Compress());
		list2.Add(new CreditRecord_Role("", "OneSpellPerDay").Compress());
		list2.Add(new CreditRecord_Role("", "Turbulent Caterwocky").Compress());
		list2.Add(new CreditRecord_Role("", "RawCode").Compress());
		list2.Add(new CreditRecord_Role("", "Enystrom8734").Compress());
		list2.Add(new CreditRecord_Role("", "TeiXeR").Compress());
		list2.Add(new CreditRecord_Role("", "MortalSmurph").Compress());
		list2.Add(new CreditRecord_Role("", "AdamVsEverything").Compress());
		foreach (CreditsEntry item2 in Reformat2Cols(list2))
		{
			yield return item2;
		}
		yield return new CreditRecord_Space(25f);
		yield return new CreditRecord_Role("", "... and many other gracious volunteers!");
		yield return new CreditRecord_Space(200f);
		foreach (LoadedLanguage lang in LanguageDatabase.AllLoadedLanguages)
		{
			lang.LoadMetadata();
			if (lang.info.credits.Count > 0)
			{
				yield return new CreditRecord_Title("Credits_TitleLanguage".Translate(lang.FriendlyNameEnglish));
			}
			foreach (CreditsEntry item3 in Reformat2Cols(lang.info.credits))
			{
				yield return item3;
			}
		}
		if (ModLister.AnomalyInstalled)
		{
			yield return new CreditRecord_Space(50f);
			yield return new CreditRecord_Title("Credits_Localization_Anomaly".Translate());
			yield return new CreditRecord_Title("Local Heroes Worldwide B.V.");
			yield return new CreditRecord_Role("Credits_LeadLocalizationProjectManager".Translate(), "Iris Kuppen");
			yield return new CreditRecord_Role("Credits_LocalizationProjectManager".Translate(), "Maikel Roelofs");
			yield return new CreditRecord_Space(25f);
			yield return new CreditRecord_Title("Credits_French".Translate() + ": Loc-3 Ltd");
			yield return new CreditRecord_Role("Credits_ProjectManager".Translate(), "Eric Emanuel");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Florie Abélard");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Ophélie Colin");
			yield return new CreditRecord_Role("Credits_Editor".Translate(), "Claire Deiller");
			yield return new CreditRecord_Space(25f);
			yield return new CreditRecord_Title("Credits_Japanese".Translate() + ": DICO Co., Ltd.");
			yield return new CreditRecord_Role("Credits_ProjectManager".Translate(), "Nilgül Durali");
			yield return new CreditRecord_Role("Credits_ProjectManager".Translate(), "Ziya Sarper Ekim");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Karien Harimoto");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Keigo Yonemura");
			yield return new CreditRecord_Role("Credits_Editor".Translate(), "Henry Buckley");
			yield return new CreditRecord_Space(25f);
			yield return new CreditRecord_Title("Credits_Korean".Translate() + ": DICO Co., Ltd.");
			yield return new CreditRecord_Role("Credits_ProjectManager".Translate(), "Nilgül Durali");
			yield return new CreditRecord_Role("Credits_ProjectManager".Translate(), "Ziya Sarper Ekim");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Doyeon Jeong");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Junglim Kim");
			yield return new CreditRecord_Role("Credits_Editor".Translate(), "Lim Yoon");
			yield return new CreditRecord_Space(25f);
			yield return new CreditRecord_Title("Credits_Polish".Translate() + ": Albion Localisations");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Aleksandra Lubińska");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Łukasz Gładkowski");
			yield return new CreditRecord_Role("Credits_Editor".Translate(), "Marcin Bojko");
			yield return new CreditRecord_Space(25f);
			yield return new CreditRecord_Title("Credits_Russian".Translate() + ": Levsha");
			yield return new CreditRecord_Role("Credits_ProjectManager".Translate(), "Ekaterina Yamenskova");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Vitaliy Hristyuk");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Polina Karpova");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Artyom Petrov");
			yield return new CreditRecord_Space(25f);
			yield return new CreditRecord_Title("Credits_ChineseSimplified".Translate() + ": Yeehe");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Jean-Luc Wu");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Roy Liu");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Twig Yu (爽朗的kk23)");
			yield return new CreditRecord_Role("Credits_Editor".Translate(), "Marcos Wang");
			yield return new CreditRecord_Role("Credits_Editor".Translate(), "ZISHA");
			yield return new CreditRecord_Space(25f);
			yield return new CreditRecord_Title("Credits_ChineseTraditional".Translate() + ": Cowbay Entertainment");
			yield return new CreditRecord_Role("Credits_ProjectManager".Translate(), "Sean Chen");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Shiou Chen");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Michelle Wu");
			yield return new CreditRecord_Role("Credits_Translator".Translate(), "Teddy Wu");
			yield return new CreditRecord_Role("Credits_Editor".Translate(), "Jerry Lee");
			yield return new CreditRecord_Space(50f);
		}
		bool firstModCredit = false;
		HashSet<string> allModders = new HashSet<string>();
		List<string> tmpModders = new List<string>();
		foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder.InRandomOrder())
		{
			if (mod.Official)
			{
				continue;
			}
			tmpModders.Clear();
			tmpModders.AddRange(mod.Authors);
			for (int num = tmpModders.Count - 1; num >= 0; num--)
			{
				tmpModders[num] = tmpModders[num].Trim();
				if (!allModders.Add(tmpModders[num].ToLowerInvariant()))
				{
					tmpModders.RemoveAt(num);
				}
			}
			if (tmpModders.Count <= 0)
			{
				continue;
			}
			foreach (string modder in tmpModders)
			{
				if (!firstModCredit)
				{
					yield return new CreditRecord_Title("Credits_TitleMods".Translate());
					firstModCredit = true;
				}
				yield return new CreditRecord_Role(mod.Name, modder).Compress();
			}
		}
		static IEnumerable<CreditsEntry> Reformat2Cols(List<CreditsEntry> entries)
		{
			string crediteePrev = null;
			for (int i = 0; i < entries.Count; i++)
			{
				CreditsEntry langCred = entries[i];
				if (langCred is CreditRecord_Role creditRecord_Role)
				{
					if (crediteePrev != null)
					{
						yield return new CreditRecord_RoleTwoCols(crediteePrev, creditRecord_Role.creditee).Compress();
						crediteePrev = null;
					}
					else
					{
						crediteePrev = creditRecord_Role.creditee;
					}
				}
				else
				{
					if (crediteePrev != null)
					{
						yield return new CreditRecord_RoleTwoCols(crediteePrev, "").Compress();
						crediteePrev = null;
					}
					yield return langCred;
				}
			}
			if (crediteePrev != null)
			{
				yield return new CreditRecord_RoleTwoCols(crediteePrev, "").Compress();
			}
		}
	}
}
