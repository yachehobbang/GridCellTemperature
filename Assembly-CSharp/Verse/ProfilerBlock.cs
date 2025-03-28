using System;
using System.Runtime.InteropServices;

namespace Verse;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct ProfilerBlock : IDisposable
{
	public ProfilerBlock(string blockName)
	{
	}

	public void Dispose()
	{
	}
}
