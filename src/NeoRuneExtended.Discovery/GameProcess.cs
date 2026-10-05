using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace NeoRuneExtended.Discovery;

public sealed class GameProcess : IDisposable
{
	private const uint PROCESS_QUERY_INFORMATION = 1024u;

	private const uint PROCESS_VM_READ = 16u;

	private readonly nint handle;

	public ulong Base { get; }

	public int ImageSize { get; }

	public string ExePath { get; }

	[DllImport("kernel32", SetLastError = true)]
	private static extern nint OpenProcess(uint access, bool inherit, int pid);

	[DllImport("kernel32", SetLastError = true)]
	private static extern bool ReadProcessMemory(nint h, nint addr, byte[] buf, nint size, out nint read);

	[DllImport("kernel32")]
	private static extern bool CloseHandle(nint h);

	private GameProcess(Process p)
	{
		handle = OpenProcess(1040u, inherit: false, p.Id);
		if (handle == IntPtr.Zero)
		{
			throw new InvalidOperationException($"OpenProcess failed ({Marshal.GetLastWin32Error()})");
		}
		ProcessModule mainModule = p.MainModule;
		Base = (ulong)mainModule.BaseAddress;
		ImageSize = mainModule.ModuleMemorySize;
		ExePath = mainModule.FileName;
	}

	public static GameProcess Attach()
	{
		return new GameProcess(GameInstall.RunningProcesses().FirstOrDefault() ?? throw new InvalidOperationException("Minecraft Dungeons II is not running."));
	}

	public byte[]? Read(ulong addr, int n)
	{
		byte[] array = new byte[n];
		if (!ReadProcessMemory(handle, (nint)addr, array, n, out var read) || read == 0)
		{
			return null;
		}
		if (read < n)
		{
			Array.Resize(ref array, (int)read);
		}
		return array;
	}

	public ulong U64(ulong a)
	{
		byte[] array = Read(a, 8);
		if (array == null || array.Length != 8)
		{
			return 0uL;
		}
		return BitConverter.ToUInt64(array);
	}

	public uint U32(ulong a)
	{
		byte[] array = Read(a, 4);
		if (array == null || array.Length != 4)
		{
			return 0u;
		}
		return BitConverter.ToUInt32(array);
	}

	public int I32(ulong a)
	{
		return (int)U32(a);
	}

	public ushort U16(ulong a)
	{
		byte[] array = Read(a, 2);
		if (array == null || array.Length != 2)
		{
			return 0;
		}
		return BitConverter.ToUInt16(array);
	}

	public byte U8(ulong a)
	{
		byte[] array = Read(a, 1);
		if (array == null || array.Length != 1)
		{
			return 0;
		}
		return array[0];
	}

	public void Dispose()
	{
		CloseHandle(handle);
	}
}
