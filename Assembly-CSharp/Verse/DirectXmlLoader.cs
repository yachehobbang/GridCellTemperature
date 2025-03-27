using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RimWorld.IO;
using UnityEngine;

namespace Verse;

public static class DirectXmlLoader
{
	private static LoadableXmlAsset[] emptyXmlAssetsArray = new LoadableXmlAsset[0];

	public static LoadableXmlAsset[] XmlAssetsInModFolder(ModContentPack mod, string folderPath, List<string> foldersToLoadDebug = null)
	{
		List<string> list = foldersToLoadDebug ?? mod.foldersToLoadDescendingOrder;
		Dictionary<string, FileInfo> dictionary = new Dictionary<string, FileInfo>();
		for (int j = 0; j < list.Count; j++)
		{
			string text = list[j];
			DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(text, folderPath));
			if (!directoryInfo.Exists)
			{
				continue;
			}
			FileInfo[] files = directoryInfo.GetFiles("*.xml", SearchOption.AllDirectories);
			foreach (FileInfo fileInfo in files)
			{
				string key = fileInfo.FullName.Substring(text.Length + 1);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, fileInfo);
				}
			}
		}
		if (dictionary.Count == 0)
		{
			return emptyXmlAssetsArray;
		}
		List<FileInfo> fileList = dictionary.Values.ToList();
		LoadableXmlAsset[] assets = new LoadableXmlAsset[fileList.Count];
		GenThreading.ParallelFor(0, fileList.Count, delegate(int i)
		{
			FileInfo fileInfo2 = fileList[i];
			LoadableXmlAsset loadableXmlAsset = new LoadableXmlAsset(fileInfo2.Name, fileInfo2.Directory.FullName, File.ReadAllText(fileInfo2.FullName))
			{
				mod = mod
			};
			assets[i] = loadableXmlAsset;
		});
		return assets;
	}

	public static IEnumerable<T> LoadXmlDataInResourcesFolder<T>(string folderPath) where T : new()
	{
		XmlInheritance.Clear();
		DeepProfiler.Start("Resources.LoadAll<TextAsset>");
		TextAsset[] source = Resources.LoadAll<TextAsset>(folderPath);
		DeepProfiler.End();
		DeepProfiler.Start("Load XML");
		List<LoadableXmlAsset> assets = (from x in source.Select((TextAsset x) => new { x.name, x.text }).ToList().AsParallel()
			select new LoadableXmlAsset(x.name, "", x.text)).ToList();
		DeepProfiler.End();
		DeepProfiler.Start("Resolve inheritance");
		foreach (LoadableXmlAsset item in assets)
		{
			XmlInheritance.TryRegisterAllFrom(item, null);
		}
		XmlInheritance.Resolve();
		DeepProfiler.End();
		DeepProfiler.Start("Read game items from XML");
		for (int i = 0; i < assets.Count; i++)
		{
			foreach (T item2 in AllGameItemsFromAsset<T>(assets[i]))
			{
				yield return item2;
			}
		}
		DeepProfiler.End();
		XmlInheritance.Clear();
	}

	public static T ItemFromXmlFile<T>(string filePath, bool resolveCrossRefs = true) where T : new()
	{
		if (!new FileInfo(filePath).Exists)
		{
			return new T();
		}
		return ItemFromXmlString<T>(File.ReadAllText(filePath), filePath, resolveCrossRefs);
	}

	public static T ItemFromXmlFile<T>(VirtualDirectory directory, string filePath, bool resolveCrossRefs = true) where T : new()
	{
		if (!directory.FileExists(filePath))
		{
			return new T();
		}
		return ItemFromXmlString<T>(directory.ReadAllText(filePath), directory.FullPath + "/" + filePath, resolveCrossRefs);
	}

	public static T ItemFromXmlString<T>(string xmlContent, string filePath, bool resolveCrossRefs = true) where T : new()
	{
		if (resolveCrossRefs && DirectXmlCrossRefLoader.LoadingInProgress)
		{
			Log.Error("Cannot call ItemFromXmlString with resolveCrossRefs=true while loading is already in progress (forgot to resolve or clear cross refs from previous loading?).");
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xmlContent);
			T result = DirectXmlToObject.ObjectFromXml<T>(xmlDocument.DocumentElement, doPostLoad: false);
			if (resolveCrossRefs)
			{
				try
				{
					DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
				}
				finally
				{
					DirectXmlCrossRefLoader.Clear();
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			Log.Error("Exception loading file at " + filePath + ". Loading defaults instead. Exception was: " + ex.ToString());
			return new T();
		}
	}

	public static Def DefFromNode(XmlNode node, LoadableXmlAsset loadingAsset)
	{
		if (node.NodeType != XmlNodeType.Element)
		{
			return null;
		}
		XmlAttribute xmlAttribute = node.Attributes["Abstract"];
		if (xmlAttribute != null && xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
		{
			return null;
		}
		Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(node.Name);
		if (typeInAnyAssembly == null)
		{
			return null;
		}
		if (!GenTypes.IsDef(typeInAnyAssembly))
		{
			return null;
		}
		Func<XmlNode, bool, object> objectFromXmlMethod = DirectXmlToObject.GetObjectFromXmlMethod(typeInAnyAssembly);
		Def result = null;
		try
		{
			result = (Def)objectFromXmlMethod(node, arg2: true);
		}
		catch (Exception ex)
		{
			Log.Error("Exception loading def from file " + ((loadingAsset != null) ? loadingAsset.name : "(unknown)") + ": " + ex);
		}
		return result;
	}

	public static IEnumerable<T> AllGameItemsFromAsset<T>(LoadableXmlAsset asset) where T : new()
	{
		if (asset.xmlDoc == null)
		{
			yield break;
		}
		XmlNodeList xmlNodeList = asset.xmlDoc.DocumentElement.SelectNodes(typeof(T).Name);
		bool gotData = false;
		foreach (XmlNode item in xmlNodeList)
		{
			XmlAttribute xmlAttribute = item.Attributes["Abstract"];
			if (xmlAttribute == null || !xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
			{
				T val;
				try
				{
					val = DirectXmlToObject.ObjectFromXml<T>(item, doPostLoad: true);
					gotData = true;
				}
				catch (Exception ex)
				{
					Log.Error("Exception loading data from file " + asset.name + ": " + ex);
					continue;
				}
				yield return val;
			}
		}
		if (!gotData)
		{
			Log.Error(string.Concat("Found no usable data when trying to get ", typeof(T), "s from file ", asset.name));
		}
	}
}
