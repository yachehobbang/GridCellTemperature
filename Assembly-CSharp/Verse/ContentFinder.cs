using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Verse;

public static class ContentFinder<T> where T : class
{
	public static T Get(string itemPath, bool reportFailure = true)
	{
		if (!UnityData.IsInMainThread)
		{
			Log.Error("Tried to get a resource \"" + itemPath + "\" from a different thread. All resources must be loaded in the main thread.");
			return null;
		}
		T val = null;
		List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
		for (int num = runningModsListForReading.Count - 1; num >= 0; num--)
		{
			val = runningModsListForReading[num].GetContentHolder<T>().Get(itemPath);
			if (val != null)
			{
				return val;
			}
		}
		if (typeof(T) == typeof(Texture2D))
		{
			val = (T)(object)Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + itemPath);
		}
		if (typeof(T) == typeof(AudioClip))
		{
			val = (T)(object)Resources.Load<AudioClip>(GenFilePaths.ContentPath<AudioClip>() + itemPath);
		}
		if (val != null)
		{
			return val;
		}
		string path = Path.Combine("Assets", "Data");
		for (int num2 = runningModsListForReading.Count - 1; num2 >= 0; num2--)
		{
			string path2 = Path.Combine(path, runningModsListForReading[num2].FolderName);
			for (int i = 0; i < runningModsListForReading[num2].assetBundles.loadedAssetBundles.Count; i++)
			{
				AssetBundle assetBundle = runningModsListForReading[num2].assetBundles.loadedAssetBundles[i];
				if (typeof(T) == typeof(Texture2D))
				{
					string text = Path.Combine(Path.Combine(path2, GenFilePaths.ContentPath<Texture2D>()), itemPath);
					for (int j = 0; j < ModAssetBundlesHandler.TextureExtensions.Length; j++)
					{
						val = (T)(object)assetBundle.LoadAsset<Texture2D>(text + ModAssetBundlesHandler.TextureExtensions[j]);
						if (val != null)
						{
							return val;
						}
					}
				}
				if (!(typeof(T) == typeof(AudioClip)))
				{
					continue;
				}
				string text2 = Path.Combine(Path.Combine(path2, GenFilePaths.ContentPath<AudioClip>()), itemPath);
				for (int k = 0; k < ModAssetBundlesHandler.AudioClipExtensions.Length; k++)
				{
					val = (T)(object)assetBundle.LoadAsset<AudioClip>(text2 + ModAssetBundlesHandler.AudioClipExtensions[k]);
					if (val != null)
					{
						return val;
					}
				}
			}
		}
		if (reportFailure)
		{
			Log.Error(string.Concat("Could not load ", typeof(T), " at ", itemPath, " in any active mod or in base resources."));
		}
		return null;
	}

	public static IEnumerable<T> GetAllInFolder(string folderPath)
	{
		if (!UnityData.IsInMainThread)
		{
			Log.Error("Tried to get all resources in a folder \"" + folderPath + "\" from a different thread. All resources must be loaded in the main thread.");
			yield break;
		}
		foreach (ModContentPack item in LoadedModManager.RunningModsListForReading)
		{
			foreach (T item2 in item.GetContentHolder<T>().GetAllUnderPath(folderPath))
			{
				yield return item2;
			}
		}
		T[] array = null;
		if (typeof(T) == typeof(Texture2D))
		{
			array = (T[])(object)Resources.LoadAll<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + folderPath);
		}
		if (typeof(T) == typeof(AudioClip))
		{
			array = (T[])(object)Resources.LoadAll<AudioClip>(GenFilePaths.ContentPath<AudioClip>() + folderPath);
		}
		if (array != null)
		{
			T[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				yield return array2[i];
			}
		}
		List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
		string modsDir = Path.Combine("Assets", "Data");
		for (int i = mods.Count - 1; i >= 0; i--)
		{
			string dirForBundle = Path.Combine(modsDir, mods[i].FolderName);
			for (int j = 0; j < mods[i].assetBundles.loadedAssetBundles.Count; j++)
			{
				AssetBundle assetBundle = mods[i].assetBundles.loadedAssetBundles[j];
				if (typeof(T) == typeof(Texture2D))
				{
					string text = Path.Combine(Path.Combine(dirForBundle, GenFilePaths.ContentPath<Texture2D>()).Replace('\\', '/'), folderPath).ToLower();
					if (text[text.Length - 1] != '/')
					{
						text += "/";
					}
					IEnumerable<string> byPrefix = mods[i].AllAssetNamesInBundleTrie(j).GetByPrefix(text);
					foreach (string item3 in byPrefix)
					{
						if (ModAssetBundlesHandler.TextureExtensions.Contains(Path.GetExtension(item3)))
						{
							yield return (T)(object)assetBundle.LoadAsset<Texture2D>(item3);
						}
					}
				}
				if (!(typeof(T) == typeof(AudioClip)))
				{
					continue;
				}
				string text2 = Path.Combine(Path.Combine(dirForBundle, GenFilePaths.ContentPath<AudioClip>()).Replace('\\', '/'), folderPath).ToLower();
				if (text2[text2.Length - 1] != '/')
				{
					text2 += "/";
				}
				IEnumerable<string> byPrefix2 = mods[i].AllAssetNamesInBundleTrie(j).GetByPrefix(text2);
				foreach (string item4 in byPrefix2)
				{
					if (ModAssetBundlesHandler.AudioClipExtensions.Contains(Path.GetExtension(item4)))
					{
						yield return (T)(object)assetBundle.LoadAsset<AudioClip>(item4);
					}
				}
			}
		}
	}
}
