using System;
using System.Runtime.InteropServices;

namespace SimpleMemoryReading64and32
{
    public class Imports
    {
        [DllImport("Kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int size, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out Region lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64);

        [DllImport("kernel32.dll")]
        public static extern bool IsWow64Process(IntPtr hProcess, out ushort processMachine, out ushort nativeMachine);

        [StructLayout(LayoutKind.Sequential)]
        public struct Region
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public MemoryProtect AllocationProtect;
            public IntPtr RegionSize;
            public MemoryState State;
            public MemoryProtect Protect;
            public MemoryType Type;
        }

        [Flags]
        public enum MemoryState : uint
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Free = 0x10000
        }

        [Flags]
        public enum MemoryType : uint
        {
            Private = 0x20000,
            Mapped = 0x40000,
            Image = 0x1000000
        }

        [Flags]
        public enum MemoryProtect : uint
        {
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            Guard = 0x100,
            NoCache = 0x200,
            WriteCombine = 0x400
        }
    }

    public class Masks
    {
        public uint Value;

        public Masks(uint value)
        {
            Value = value;
        }

        public static readonly Masks ReadableMask = new Masks((uint)(Imports.MemoryProtect.ReadOnly | Imports.MemoryProtect.ReadWrite | Imports.MemoryProtect.ExecuteRead | Imports.MemoryProtect.ExecuteReadWrite | Imports.MemoryProtect.ExecuteWriteCopy | Imports.MemoryProtect.WriteCopy));

        public static readonly Masks WritableMask = new Masks((uint)(Imports.MemoryProtect.ReadWrite | Imports.MemoryProtect.ExecuteReadWrite | Imports.MemoryProtect.ExecuteWriteCopy | Imports.MemoryProtect.WriteCopy));
    }
}
