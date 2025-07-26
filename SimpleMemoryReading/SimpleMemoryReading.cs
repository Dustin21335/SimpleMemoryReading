using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleMemoryReading64and32
{
    public class SimpleMemoryReading
    {
        public SimpleMemoryReading(string processName) => Initialize(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName)).FirstOrDefault());

        public SimpleMemoryReading(int id) => Initialize(Process.GetProcessById(id));

        public SimpleMemoryReading(Process process) => Initialize(process);

        public Process Process { get; private set; }
        public IntPtr Handle { get; private set; }
        public bool Is64Bit { get; private set; }
        public List<Imports.Region> AllRegions { get; private set; }
        public List<Imports.Region> ModuleMemoryRegions { get; private set; }
        public List<Imports.Region> MappedMemoryRegions { get; private set; }
        public List<Imports.Region> PrivateMemoryRegions { get; private set; }
        public List<Imports.Region> FreeMemoryRegions { get; private set; }
        public List<Imports.Region> ReservedMemoryRegions { get; private set; }

        private void Initialize(Process process)
        {
            if (process == null) return;
            this.Process = process;
            this.Handle = process.Handle;
            this.Is64Bit = Environment.Is64BitOperatingSystem && (Imports.IsWow64Process(Handle, out ushort pm, out _) ? pm == 0 : Imports.IsWow64Process(Handle, out bool w) && !w);
            this.AllRegions = GetRegions();
            this.ModuleMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Image);
            this.MappedMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Mapped);
            this.PrivateMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Private);
            this.FreeMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MemoryState.Free);
            this.ReservedMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MemoryState.Reserve);
        }

        public ProcessModule GetModule(string module)
        {
            Process.Refresh();
            module = Path.GetFileNameWithoutExtension(module).ToLower().Trim();
            if (Process.MainModule != null && module == Path.GetFileNameWithoutExtension(Process.ProcessName).ToLower().Trim()) return Process.MainModule;
            return Process.Modules.Cast<ProcessModule>().FirstOrDefault(m => Path.GetFileNameWithoutExtension(m.ModuleName).ToLower().Trim() == module);
        }

        public IntPtr GetModuleBase(string module)
        {
            return GetModule(module)?.BaseAddress ?? IntPtr.Zero;
        }

        public List<Imports.Region> GetRegions()
        {
            List<Imports.Region> regions = new List<Imports.Region>();
            IntPtr address = IntPtr.Zero;
            while (Imports.VirtualQueryEx(Handle, address, out Imports.Region region, (uint)Marshal.SizeOf<Imports.Region>()) != 0)
            {
                regions.Add(region);
                long next = address.ToInt64() + region.RegionSize.ToInt64();
                if (next <= 0 || next >= long.MaxValue) break;
                address = new IntPtr(next);
            }
            return regions;
        }

        public byte[] ReadBytes(IntPtr address, int size, params IntPtr[] offsets)
        {
            IntPtr baseAddress = address;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                if (!Imports.ReadProcessMemory(Handle, baseAddress + offsets[i], buffer, buffer.Length, IntPtr.Zero)) return Array.Empty<byte>();
                baseAddress = IntPtr.Size == 4 ? (IntPtr)BitConverter.ToInt32(buffer, 0) : (IntPtr)BitConverter.ToInt64(buffer, 0);
            }
            if (offsets.Length > 0) baseAddress += offsets[offsets.Length - 1];
            byte[] result = new byte[size];
            Imports.ReadProcessMemory(Handle, baseAddress, result, result.Length, IntPtr.Zero);
            return result;
        }

        public byte[] ReadBytes(IntPtr address, params IntPtr[] offsets)
        {
            return ReadBytes(address, Is64Bit ? 8 : 4, offsets);
        }

        public IntPtr ReadPointer(IntPtr address, params IntPtr[] offsets)
        {
            return Is64Bit ? (IntPtr)BitConverter.ToInt64(ReadBytes(address, offsets), 0) : (IntPtr)BitConverter.ToInt32(ReadBytes(address, offsets), 0);
        }

        public T Read<T>(IntPtr address, params IntPtr[] offsets) where T : struct
        {
            return MemoryMarshal.Read<T>(ReadBytes(address, Marshal.SizeOf<T>(), offsets));
        }

        public bool ReadArray<T>(IntPtr address, T[] values, params IntPtr[] offsets) where T : struct
        {
            int size = Marshal.SizeOf<T>() * values.Length;
            byte[] buffer = ReadBytes(address, size, offsets);
            if (buffer.Length != size) return false;
            MemoryMarshal.Cast<byte, T>(buffer).CopyTo(values);
            return true;
        }

        public string ReadString(IntPtr address, int size, Encoding encoding, params IntPtr[] offsets)
        {
            return encoding.GetString(ReadBytes(address, size, offsets));
        }

        public bool WriteBytes(IntPtr address, byte[] bytes, int size, params IntPtr[] offsets)
        {
            IntPtr baseAddress = address;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                if (!Imports.ReadProcessMemory(Handle, baseAddress + offsets[i], buffer, buffer.Length, IntPtr.Zero)) return false;
                baseAddress = IntPtr.Size == 4 ? (IntPtr)BitConverter.ToInt32(buffer, 0) : (IntPtr)BitConverter.ToInt64(buffer, 0);
            }
            if (offsets.Length > 0) baseAddress += offsets[^1];
            return Imports.WriteProcessMemory(Handle, baseAddress, bytes, size, IntPtr.Zero);
        }

        public bool WriteBytes(IntPtr address, byte[] bytes, params IntPtr[] offsets)
        {
            return WriteBytes(address, bytes, Is64Bit ? 8 : 4, offsets);
        }

        public bool Write<T>(IntPtr address, T value, params IntPtr[] offsets) where T : struct
        {
            return WriteBytes(address, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)).ToArray(), offsets);
        }

        public bool WriteArray<T>(IntPtr address, T[] values, params IntPtr[] offsets) where T : struct
        {
            return WriteBytes(address, MemoryMarshal.AsBytes(new ReadOnlySpan<T>(values)).ToArray(), offsets);
        }

        public bool WriteString(IntPtr address, string value, Encoding encoding, params IntPtr[] offsets)
        {
            return WriteBytes(address, encoding.GetBytes(value), offsets);
        }

        public List<IntPtr> AOBScanRegion(Imports.Region region, byte?[] pattern, Masks mask = null)
        {
            List<IntPtr> results = new List<IntPtr>();
            uint uintMask = mask?.Value ?? Masks.ReadableMask.Value;
            if (((uint)region.Protect & uintMask) == 0) return results;
            long regionSize = (long)region.RegionSize;
            int patternLength = pattern.Length;
            if (regionSize < patternLength) return results;
            byte[] buffer = new byte[(int)regionSize];
            if (!Imports.ReadProcessMemory(Handle, region.BaseAddress, buffer, buffer.Length, IntPtr.Zero)) return results;
            for (int i = 0; i < buffer.Length - patternLength + 1; i++)
            {
                bool match = true;
                for (int j = 0; j < patternLength; j++)
                {
                    byte? p = pattern[j];
                    if (p.HasValue && buffer[i + j] != p.Value)
                    {
                        match = false;
                        break;
                    }
                }
                if (match) results.Add(IntPtr.Add(region.BaseAddress, i));
            }
            return results;
        }

        public List<IntPtr> AOBScanRegion(Imports.Region region, string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegion(region, pattern, patternMask, mask);
        }

        public List<IntPtr> AOBScanRegions(List<Imports.Region> regions, byte?[] pattern, Masks mask = null)
        {
            return regions.SelectMany(r => AOBScanRegion(r, pattern, mask)).ToList();
        }

        public List<IntPtr> AOBScanRegions(List<Imports.Region> regions, string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(regions, PatternToBytes(pattern, patternMask), mask);
        }

        public List<IntPtr> AOBScanModuleRegions(string module, string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanModuleRegions(GetModule(module), pattern, patternMask, mask);
        }

        public List<IntPtr> AOBScanModuleRegions(ProcessModule module, string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegion(new Imports.Region { BaseAddress = module.BaseAddress, RegionSize = module.ModuleMemorySize, State = Imports.MemoryState.Commit, Type = Imports.MemoryType.Image }, PatternToBytes(pattern, patternMask), mask);
        }

        public List<IntPtr> AOBScanModuleRegions(string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(ModuleMemoryRegions, PatternToBytes(pattern, patternMask), mask);
        }

        public List<IntPtr> AOBScanMappedRegions(string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(MappedMemoryRegions, PatternToBytes(pattern, patternMask), mask);
        }

        public List<IntPtr> AOBScanPrivateRegions(string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(PrivateMemoryRegions, PatternToBytes(pattern, patternMask), mask);
        }

        public List<IntPtr> AOBScanFreeeRegions(string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(FreeMemoryRegions, PatternToBytes(pattern, patternMask), mask);
        }

        public List<IntPtr> AOBScanReservedRegions(string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(ReservedMemoryRegions, PatternToBytes(pattern, patternMask), mask);
        }

        public byte?[] PatternToBytes(string pattern, string patternMask = null)
        {
            if (!string.IsNullOrEmpty(patternMask))
            {
                byte?[] result = new byte?[patternMask.Length];
                byte[] bytes = Encoding.Default.GetBytes(pattern);
                for (int i = 0; i < patternMask.Length; i++)
                {
                    if (patternMask[i] == '?') result[i] = null;
                    else result[i] = bytes[i];
                }
                return result;
            }
            return pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(b => b == "?" || b == "??" ? (byte?)null : Convert.ToByte(b, 16)).ToArray();
        }
    }
}