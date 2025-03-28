using System;

namespace Verse;

public struct AnyEnum
{
	public Type enumType;

	public int enumCode;

	public static AnyEnum None => default(AnyEnum);

	public T? As<T>() where T : struct
	{
		if (typeof(T) == enumType)
		{
			return GenericConverter.Convert<int, T>(enumCode);
		}
		return null;
	}

	public static AnyEnum FromEnum<T>(T reasonCode) where T : struct, Enum
	{
		AnyEnum result = default(AnyEnum);
		result.enumType = typeof(T);
		result.enumCode = GenericConverter.Convert<T, int>(reasonCode);
		return result;
	}

	public override string ToString()
	{
		return Enum.ToObject(enumType, enumCode).ToStringSafe();
	}

	public static bool operator ==(AnyEnum lhs, AnyEnum rhs)
	{
		if (lhs.enumCode == rhs.enumCode)
		{
			return lhs.enumType == rhs.enumType;
		}
		return false;
	}

	public static bool operator !=(AnyEnum lhs, AnyEnum rhs)
	{
		return !(lhs == rhs);
	}

	public override bool Equals(object obj)
	{
		if (obj is AnyEnum anyEnum)
		{
			return this == anyEnum;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Gen.HashCombineInt(Gen.HashCombine(0, enumType), enumCode);
	}
}
