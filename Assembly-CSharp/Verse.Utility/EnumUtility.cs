using System;
using System.Collections.Generic;
using System.Linq;

namespace Verse.Utility;

public static class EnumUtility
{
	public static IEnumerable<T> GetValues<T>()
	{
		return Enum.GetValues(typeof(T)).Cast<T>();
	}

	public static IEnumerable<T> GetValuesReverse<T>()
	{
		return Enum.GetValues(typeof(T)).Cast<T>().Reverse();
	}
}
