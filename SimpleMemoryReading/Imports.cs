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
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
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

        [Flags]
        public enum Opcode : byte
        {
            Nop = 0x90,
            Int3 = 0xCC,
            Ret = 0xC3,
            PushEbp = 0x55,
            Mov = 0x8B,
            MovStore = 0x89,
            MovEaxImm = 0xB8,
            MovEcXImm = 0xB9,
            MovEdXImm = 0xBA,
            MovEbXImm = 0xBB,
            MovEspImm = 0xBC,
            MovEbPImm = 0xBD,
            MovESIImm = 0xBE,
            MovEDIImm = 0xBF,
            Call = 0xE8,
            Jmp = 0xE9,
            JmpShort = 0xEB,
            Je = 0x74,
            Jne = 0x75,
            Jo = 0x70,
            Jno = 0x71,
            Jc = 0x72,
            Jnc = 0x73,
            RexW = 0x48,
            Rep = 0xF3,
            RepNe = 0xF2,
            PushImm8 = 0x6A,
            PushImm32 = 0x68,
            PopEax = 0x58,
            PopEcX = 0x59,
            PopEdX = 0x5A,
            PopEbX = 0x5B,
            PopEsp = 0x5C,
            PopEbP = 0x5D,
            PopESI = 0x5E,
            PopEDI = 0x5F,
            PushEax = 0x50,
            PushEcX = 0x51,
            PushEdX = 0x52,
            PushEbX = 0x53,
            PushEsp = 0x54,
            PushEbP = 0x56,
            PushESI = 0x57,
            Add = 0x01,
            Sub = 0x29,
            Xor = 0x31,
            Cmp = 0x39,
            Ff = 0xFF,
            C7 = 0xC7,
            Cd = 0xCD,
            F7 = 0xF7
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
