using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace Verse;

public static class DirectXmlToObject
{
	private struct FieldAliasCache : IEquatable<FieldAliasCache>
	{
		public Type type;

		public string fieldName;

		public FieldAliasCache(Type type, string fieldName)
		{
			this.type = type;
			this.fieldName = fieldName.ToLower();
		}

		public bool Equals(FieldAliasCache other)
		{
			if (type == other.type)
			{
				return string.Equals(fieldName, other.fieldName);
			}
			return false;
		}
	}

	public static Stack<Type> currentlyInstantiatingObjectOfType = new Stack<Type>();

	public const string DictionaryKeyName = "key";

	public const string DictionaryValueName = "value";

	public const string LoadDataFromXmlCustomMethodName = "LoadDataFromXmlCustom";

	public const string PostLoadMethodName = "PostLoad";

	private static Dictionary<Type, Func<XmlNode, object>> listFromXmlMethods = new Dictionary<Type, Func<XmlNode, object>>();

	private static Dictionary<Type, Func<XmlNode, object>> dictionaryFromXmlMethods = new Dictionary<Type, Func<XmlNode, object>>();

	private static readonly Type[] tmpOneTypeArray = new Type[1];

	private static readonly Dictionary<Type, Func<XmlNode, bool, object>> objectFromXmlMethods = new Dictionary<Type, Func<XmlNode, bool, object>>();

	private static Dictionary<FieldAliasCache, FieldInfo> fieldAliases = new Dictionary<FieldAliasCache, FieldInfo>(EqualityComparer<FieldAliasCache>.Default);

	private static Dictionary<Type, MethodInfo> customDataLoadMethodCache = new Dictionary<Type, MethodInfo>();

	private static Dictionary<Type, MethodInfo> postLoadMethodCache = new Dictionary<Type, MethodInfo>();

	private static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoLookup = new Dictionary<Type, Dictionary<string, FieldInfo>>();

	private static Dictionary<(Type, string), FieldInfo> getFieldCache = new Dictionary<(Type, string), FieldInfo>();

	private static Dictionary<(Type, string), FieldInfo> getFieldIgnoreCaseCache = new Dictionary<(Type, string), FieldInfo>();

	public static Func<XmlNode, bool, object> GetObjectFromXmlMethod(Type type)
	{
		if (!objectFromXmlMethods.TryGetValue(type, out var value))
		{
			MethodInfo method = typeof(DirectXmlToObject).GetMethod("ObjectFromXmlReflection", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			tmpOneTypeArray[0] = type;
			value = (Func<XmlNode, bool, object>)Delegate.CreateDelegate(typeof(Func<XmlNode, bool, object>), method.MakeGenericMethod(tmpOneTypeArray));
			objectFromXmlMethods.Add(type, value);
		}
		return value;
	}

	private static object ObjectFromXmlReflection<T>(XmlNode xmlRoot, bool doPostLoad)
	{
		return ObjectFromXml<T>(xmlRoot, doPostLoad);
	}

	public static T ObjectFromXml<T>(XmlNode xmlRoot, bool doPostLoad)
	{
		XmlAttribute xmlAttribute = xmlRoot.Attributes["IsNull"];
		if (xmlAttribute != null && xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
		{
			return default(T);
		}
		Type typeFromHandle = typeof(T);
		MethodInfo methodInfo = CustomDataLoadMethodOf(typeFromHandle);
		if (methodInfo != null)
		{
			xmlRoot = XmlInheritance.GetResolvedNodeFor(xmlRoot);
			Type type = ClassTypeOf<T>(xmlRoot);
			currentlyInstantiatingObjectOfType.Push(type);
			T val;
			try
			{
				val = (T)Activator.CreateInstance(type);
			}
			finally
			{
				currentlyInstantiatingObjectOfType.Pop();
			}
			try
			{
				methodInfo.Invoke(val, new object[1] { xmlRoot });
			}
			catch (Exception ex)
			{
				Log.Error(string.Concat("Exception in custom XML loader for ", typeFromHandle, ". Node is:\n ", xmlRoot.OuterXml, "\n\nException is:\n ", ex.ToString()));
				val = default(T);
			}
			if (doPostLoad)
			{
				TryDoPostLoad(val);
			}
			return val;
		}
		if (GenTypes.IsSlateRef(typeFromHandle))
		{
			try
			{
				return ParseHelper.FromString<T>(InnerTextWithReplacedNewlinesOrXML(xmlRoot));
			}
			catch (Exception ex2)
			{
				Log.Error(string.Concat("Exception parsing ", xmlRoot.OuterXml, " to type ", typeFromHandle, ": ", ex2));
			}
			return default(T);
		}
		if (xmlRoot.ChildNodes.Count == 1 && xmlRoot.FirstChild.NodeType == XmlNodeType.Text)
		{
			try
			{
				return ParseHelper.FromString<T>(xmlRoot.InnerText);
			}
			catch (Exception ex3)
			{
				Log.Error(string.Concat("Exception parsing ", xmlRoot.OuterXml, " to type ", typeFromHandle, ": ", ex3));
			}
			return default(T);
		}
		if (xmlRoot.ChildNodes.Count == 1 && xmlRoot.FirstChild.NodeType == XmlNodeType.CDATA)
		{
			if (typeFromHandle != typeof(string))
			{
				Log.Error("CDATA can only be used for strings. Bad xml: " + xmlRoot.OuterXml);
				return default(T);
			}
			return (T)(object)xmlRoot.FirstChild.Value;
		}
		if (GenTypes.HasFlagsAttribute(typeFromHandle))
		{
			List<T> list = ListFromXml<T>(xmlRoot);
			int num = 0;
			foreach (T item in list)
			{
				int num2 = (int)(object)item;
				num |= num2;
			}
			return (T)(object)num;
		}
		if (GenTypes.IsList(typeFromHandle))
		{
			Func<XmlNode, object> value = null;
			if (!listFromXmlMethods.TryGetValue(typeFromHandle, out value))
			{
				MethodInfo method = typeof(DirectXmlToObject).GetMethod("ListFromXmlReflection", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				Type[] genericArguments = typeFromHandle.GetGenericArguments();
				value = (Func<XmlNode, object>)Delegate.CreateDelegate(typeof(Func<XmlNode, object>), method.MakeGenericMethod(genericArguments));
				listFromXmlMethods.Add(typeFromHandle, value);
			}
			return (T)value(xmlRoot);
		}
		if (GenTypes.IsDictionary(typeFromHandle))
		{
			Func<XmlNode, object> value2 = null;
			if (!dictionaryFromXmlMethods.TryGetValue(typeFromHandle, out value2))
			{
				MethodInfo method2 = typeof(DirectXmlToObject).GetMethod("DictionaryFromXmlReflection", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				Type[] genericArguments2 = typeFromHandle.GetGenericArguments();
				value2 = (Func<XmlNode, object>)Delegate.CreateDelegate(typeof(Func<XmlNode, object>), method2.MakeGenericMethod(genericArguments2));
				dictionaryFromXmlMethods.Add(typeFromHandle, value2);
			}
			return (T)value2(xmlRoot);
		}
		if (!xmlRoot.HasChildNodes)
		{
			if (typeFromHandle == typeof(string))
			{
				return (T)(object)"";
			}
			XmlAttribute xmlAttribute2 = xmlRoot.Attributes["IsNull"];
			if (xmlAttribute2 != null && xmlAttribute2.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
			{
				return default(T);
			}
			if (GenTypes.IsListHashSetOrDictionary(typeFromHandle))
			{
				return Activator.CreateInstance<T>();
			}
		}
		xmlRoot = XmlInheritance.GetResolvedNodeFor(xmlRoot);
		Type type2 = ClassTypeOf<T>(xmlRoot);
		Type type3 = Nullable.GetUnderlyingType(type2) ?? type2;
		currentlyInstantiatingObjectOfType.Push(type3);
		T val2;
		try
		{
			val2 = (T)Activator.CreateInstance(type3);
		}
		catch (InvalidCastException)
		{
			throw new InvalidCastException($"Cannot cast XML type {type3} to C# type {typeof(T)}.");
		}
		finally
		{
			currentlyInstantiatingObjectOfType.Pop();
		}
		HashSet<string> hashSet = null;
		if (xmlRoot.ChildNodes.Count > 1)
		{
			hashSet = new HashSet<string>();
		}
		XmlNodeList childNodes = xmlRoot.ChildNodes;
		for (int i = 0; i < childNodes.Count; i++)
		{
			XmlNode xmlNode = childNodes[i];
			if (xmlNode is XmlComment)
			{
				continue;
			}
			if (childNodes.Count > 1 && !hashSet.Add(xmlNode.Name))
			{
				Log.Error(string.Concat("XML ", typeFromHandle, " defines the same field twice: ", xmlNode.Name, ".\n\nField contents: ", xmlNode.InnerText, ".\n\nWhole XML:\n\n", xmlRoot.OuterXml));
			}
			FieldInfo value3 = GetFieldInfoForType(type3, xmlNode.Name, xmlRoot);
			if (value3 == null)
			{
				DeepProfiler.Start("Field search");
				try
				{
					FieldAliasCache key = new FieldAliasCache(type3, xmlNode.Name);
					if (!fieldAliases.TryGetValue(key, out value3))
					{
						FieldInfo[] fields = type3.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						foreach (FieldInfo fieldInfo in fields)
						{
							object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(LoadAliasAttribute), inherit: true);
							for (int k = 0; k < customAttributes.Length; k++)
							{
								if (((LoadAliasAttribute)customAttributes[k]).alias.EqualsIgnoreCase(xmlNode.Name))
								{
									value3 = fieldInfo;
									break;
								}
							}
							if (value3 != null)
							{
								break;
							}
						}
						fieldAliases.Add(key, value3);
					}
				}
				finally
				{
					DeepProfiler.End();
				}
			}
			if (value3 != null)
			{
				UnsavedAttribute unsavedAttribute = value3.TryGetAttribute<UnsavedAttribute>();
				if (unsavedAttribute != null && !unsavedAttribute.allowLoading)
				{
					Log.Error("XML error: " + xmlNode.OuterXml + " corresponds to a field in type " + type3.Name + " which has an Unsaved attribute. Context: " + xmlRoot.OuterXml);
					continue;
				}
			}
			if (value3 == null)
			{
				DeepProfiler.Start("Field search 2");
				try
				{
					bool flag = false;
					XmlAttribute xmlAttribute3 = xmlNode.Attributes?["IgnoreIfNoMatchingField"];
					if (xmlAttribute3 != null && xmlAttribute3.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
					{
						flag = true;
					}
					else
					{
						object[] customAttributes = type3.GetCustomAttributes(typeof(IgnoreSavedElementAttribute), inherit: true);
						for (int j = 0; j < customAttributes.Length; j++)
						{
							if (string.Equals(((IgnoreSavedElementAttribute)customAttributes[j]).elementToIgnore, xmlNode.Name, StringComparison.OrdinalIgnoreCase))
							{
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						Log.Error("XML error: " + xmlNode.OuterXml + " doesn't correspond to any field in type " + type3.Name + ". Context: " + xmlRoot.OuterXml);
					}
				}
				finally
				{
					DeepProfiler.End();
				}
			}
			else if (GenTypes.IsDef(value3.FieldType))
			{
				if (xmlNode.InnerText.NullOrEmpty())
				{
					value3.SetValue(val2, null);
					continue;
				}
				XmlAttribute xmlAttribute4 = xmlNode.Attributes["MayRequire"];
				XmlAttribute xmlAttribute5 = xmlNode.Attributes["MayRequireAnyOf"];
				DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(val2, value3, xmlNode.InnerText, xmlAttribute4?.Value.ToLower(), xmlAttribute5?.Value.ToLower());
			}
			else
			{
				object obj = null;
				try
				{
					obj = GetObjectFromXmlMethod(value3.FieldType)(xmlNode, doPostLoad);
				}
				catch (Exception ex5)
				{
					Log.Error("Exception loading from " + xmlNode.ToString() + ": " + ex5.ToString());
					continue;
				}
				if (!typeFromHandle.IsValueType)
				{
					value3.SetValue(val2, obj);
					continue;
				}
				object obj2 = val2;
				value3.SetValue(obj2, obj);
				val2 = (T)obj2;
			}
		}
		if (doPostLoad)
		{
			TryDoPostLoad(val2);
		}
		return val2;
	}

	private static Type ClassTypeOf<T>(XmlNode xmlRoot)
	{
		XmlAttribute xmlAttribute = xmlRoot.Attributes["Class"];
		if (xmlAttribute != null)
		{
			Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(xmlAttribute.Value, typeof(T).Namespace);
			if (typeInAnyAssembly == null)
			{
				Log.Error("Could not find type named " + xmlAttribute.Value + " from node " + xmlRoot.OuterXml);
				return typeof(T);
			}
			return typeInAnyAssembly;
		}
		return typeof(T);
	}

	private static void TryDoPostLoad(object obj)
	{
		DeepProfiler.Start("TryDoPostLoad");
		try
		{
			MethodInfo methodInfo = PostLoadMethodOf(obj.GetType());
			if (methodInfo != null)
			{
				methodInfo.Invoke(obj, null);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Exception while executing PostLoad on " + obj.ToStringSafe() + ": " + ex);
		}
		finally
		{
			DeepProfiler.End();
		}
	}

	private static object ListFromXmlReflection<T>(XmlNode listRootNode)
	{
		return ListFromXml<T>(listRootNode);
	}

	private static List<T> ListFromXml<T>(XmlNode listRootNode)
	{
		List<T> list = new List<T>();
		try
		{
			bool flag = GenTypes.IsDef(typeof(T));
			foreach (XmlNode childNode in listRootNode.ChildNodes)
			{
				if (!ValidateListNode(childNode, listRootNode, typeof(T)))
				{
					continue;
				}
				XmlAttribute xmlAttribute = childNode.Attributes["MayRequire"];
				XmlAttribute xmlAttribute2 = childNode.Attributes["MayRequireAnyOf"];
				if (flag)
				{
					DirectXmlCrossRefLoader.RegisterListWantsCrossRef(list, childNode.InnerText, listRootNode.Name, xmlAttribute?.Value, xmlAttribute2?.Value);
				}
				else if (xmlAttribute != null && !xmlAttribute.Value.NullOrEmpty() && !ModsConfig.AreAllActive(xmlAttribute.Value))
				{
					if (DirectXmlCrossRefLoader.MistypedMayRequire(xmlAttribute.Value))
					{
						Log.Error("Faulty MayRequire: " + xmlAttribute.Value);
					}
				}
				else if (xmlAttribute2 == null || xmlAttribute2.Value.NullOrEmpty() || ModsConfig.IsAnyActiveOrEmpty(xmlAttribute2.Value.Split(','), trimNames: true))
				{
					try
					{
						list.Add(ObjectFromXml<T>(childNode, doPostLoad: true));
					}
					catch (Exception arg)
					{
						Log.Error($"Exception loading list element {typeof(T)} from XML: {arg}\nXML:\n{listRootNode.OuterXml}");
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(string.Concat("Exception loading list from XML: ", ex, "\nXML:\n", listRootNode.OuterXml));
		}
		return list;
	}

	private static object DictionaryFromXmlReflection<K, V>(XmlNode dictRootNode)
	{
		return DictionaryFromXml<K, V>(dictRootNode);
	}

	private static Dictionary<K, V> DictionaryFromXml<K, V>(XmlNode dictRootNode)
	{
		Dictionary<K, V> dictionary = new Dictionary<K, V>();
		try
		{
			bool num = GenTypes.IsDef(typeof(K));
			bool flag = GenTypes.IsDef(typeof(V));
			if (!num && !flag)
			{
				foreach (XmlNode childNode in dictRootNode.ChildNodes)
				{
					if (ValidateListNode(childNode, dictRootNode, typeof(KeyValuePair<K, V>)))
					{
						K key = ObjectFromXml<K>(childNode["key"], doPostLoad: true);
						V value = ObjectFromXml<V>(childNode["value"], doPostLoad: true);
						dictionary.Add(key, value);
					}
				}
			}
			else
			{
				foreach (XmlNode childNode2 in dictRootNode.ChildNodes)
				{
					if (ValidateListNode(childNode2, dictRootNode, typeof(KeyValuePair<K, V>)))
					{
						DirectXmlCrossRefLoader.RegisterDictionaryWantsCrossRef(dictionary, childNode2, dictRootNode.Name);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error("Malformed dictionary XML. Node: " + dictRootNode.OuterXml + ".\n\nException: " + ex);
		}
		return dictionary;
	}

	private static MethodInfo CustomDataLoadMethodOf(Type type)
	{
		if (customDataLoadMethodCache.TryGetValue(type, out var value))
		{
			return value;
		}
		MethodInfo method = type.GetMethod("LoadDataFromXmlCustom", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		customDataLoadMethodCache.Add(type, method);
		return method;
	}

	private static MethodInfo PostLoadMethodOf(Type type)
	{
		if (postLoadMethodCache.TryGetValue(type, out var value))
		{
			return value;
		}
		MethodInfo method = type.GetMethod("PostLoad");
		postLoadMethodCache.Add(type, method);
		return method;
	}

	private static bool ValidateListNode(XmlNode listEntryNode, XmlNode listRootNode, Type listItemType)
	{
		if (listEntryNode is XmlComment)
		{
			return false;
		}
		if (listEntryNode is XmlText)
		{
			Log.Error("XML format error: Raw text found inside a list element. Did you mean to surround it with list item <li> tags? " + listRootNode.OuterXml);
			return false;
		}
		if (listEntryNode.Name != "li" && CustomDataLoadMethodOf(listItemType) == null)
		{
			Log.Error("XML format error: List item found with name that is not <li>, and which does not have a custom XML loader method, in " + listRootNode.OuterXml);
			return false;
		}
		return true;
	}

	private static FieldInfo GetFieldInfoForType(Type type, string token, XmlNode debugXmlNode)
	{
		if (!fieldInfoLookup.TryGetValue(type, out var value))
		{
			value = new Dictionary<string, FieldInfo>();
			fieldInfoLookup.Add(type, value);
		}
		if (!value.TryGetValue(token, out var value2))
		{
			value2 = SearchTypeHierarchy(type, token, ignoreCase: false);
			if (value2 == null)
			{
				value2 = SearchTypeHierarchy(type, token, ignoreCase: true);
				if (value2 != null && !type.HasAttribute<CaseInsensitiveXMLParsing>())
				{
					string text = $"Attempt to use string {token} to refer to field {value2.Name} in type {type}; xml tags are now case-sensitive";
					if (debugXmlNode != null)
					{
						text = text + ". XML: " + debugXmlNode.OuterXml;
					}
					Log.Error(text);
				}
			}
			value.Add(token, value2);
		}
		return value2;
	}

	private static FieldInfo SearchTypeHierarchy(Type type, string token, bool ignoreCase)
	{
		Dictionary<(Type, string), FieldInfo> dictionary = (ignoreCase ? getFieldIgnoreCaseCache : getFieldCache);
		FieldInfo value = null;
		while (true)
		{
			if (!dictionary.TryGetValue((type, token), out value))
			{
				value = type.GetField(token, (BindingFlags)((ignoreCase ? 1 : 0) | 0x10 | 0x20 | 4));
				dictionary.Add((type, token), value);
			}
			if (!(value == null) || !(type.BaseType != typeof(object)))
			{
				break;
			}
			type = type.BaseType;
		}
		return value;
	}

	public static string InnerTextWithReplacedNewlinesOrXML(XmlNode xmlNode)
	{
		if (xmlNode.ChildNodes.Count == 1 && xmlNode.FirstChild.NodeType == XmlNodeType.Text)
		{
			return xmlNode.InnerText.Replace("\\n", "\n");
		}
		return xmlNode.InnerXml;
	}
}
