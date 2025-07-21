using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleMemoryReading64and32
{
    public class SimpleMemoryReading
    {
        public SimpleMemoryReading(string process)
        {
            this.process = Process.GetProcessesByName(process)[0];
            this.handle = this.process.Handle;
            this.AllRegions = GetRegions();
            this.ModuleMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Image);
            this.MappedMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Mapped);
            this.PrivateMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Private);
            this.FreeOrReservedMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MemoryState.Free || r.State == Imports.MemoryState.Reserve);
        }

        public SimpleMemoryReading(int id)
        {
            this.process = Process.GetProcessById(id);
            this.handle = process.Handle;
            this.AllRegions = GetRegions();
            this.ModuleMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Image);
            this.MappedMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Mapped);
            this.PrivateMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Private);
            this.FreeOrReservedMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MemoryState.Free || r.State == Imports.MemoryState.Reserve);
        }

        public SimpleMemoryReading(Process process)
        {
            this.process = process;
            this.handle = process.Handle;
            this.AllRegions = GetRegions();
            this.ModuleMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Image);
            this.MappedMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Mapped);
            this.PrivateMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MemoryType.Private);
            this.FreeOrReservedMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MemoryState.Free || r.State == Imports.MemoryState.Reserve);
        }

        public Process process;
        private IntPtr handle;
        public List<Imports.MEMORY_BASIC_INFORMATION> AllRegions;
        public List<Imports.MEMORY_BASIC_INFORMATION> ModuleMemoryRegions;
        public List<Imports.MEMORY_BASIC_INFORMATION> MappedMemoryRegions;
        public List<Imports.MEMORY_BASIC_INFORMATION> PrivateMemoryRegions;
        public List<Imports.MEMORY_BASIC_INFORMATION> FreeOrReservedMemoryRegions;

        public ProcessModule GetModule(string module)
        {
            module = module.ToLower().Replace(".dll", "").Replace(".exe", "").Trim();
            process.Refresh();
            if (process.MainModule != null && module == process.ProcessName.ToLower().Replace(".exe", "").Trim()) return process.MainModule;
            return process.Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName.ToLower().Replace(".dll", "").Trim() == module);
        }

        public IntPtr GetModuleBase(string module)
        {
            ProcessModule processModule = GetModule(module);
            return processModule != null ? GetModule(module).BaseAddress : IntPtr.Zero;
        }

        public List<Imports.MEMORY_BASIC_INFORMATION> GetRegions()
        {
            List<Imports.MEMORY_BASIC_INFORMATION> regions = new List<Imports.MEMORY_BASIC_INFORMATION>();
            IntPtr address = IntPtr.Zero;
            int size = Marshal.SizeOf<Imports.MEMORY_BASIC_INFORMATION>();
            while (Imports.VirtualQueryEx(handle, address, out Imports.MEMORY_BASIC_INFORMATION memInfo, (uint)size) != 0)
            {
                regions.Add(memInfo);
                long next = address.ToInt64() + memInfo.RegionSize.ToInt64();
                if (next <= 0 || next >= long.MaxValue) break;
                address = new IntPtr(next);
            }
            return regions;
        }

        public byte[] ReadBytes(IntPtr baseAddress, int size, params IntPtr[] offsets)
        {
            IntPtr address = baseAddress;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                if (!Imports.ReadProcessMemory(handle, address + offsets[i], buffer, buffer.Length, IntPtr.Zero)) return Array.Empty<byte>();
                address = IntPtr.Size == 4 ? (IntPtr)BitConverter.ToInt32(buffer, 0) : (IntPtr)BitConverter.ToInt64(buffer, 0);
            }
            if (offsets.Length > 0) address += offsets[offsets.Length - 1];
            byte[] result = new byte[size];
            Imports.ReadProcessMemory(handle, address, result, result.Length, IntPtr.Zero);
            return result;
        }

        public IntPtr ReadPointer(IntPtr baseAddress, params IntPtr[] offsets)
        {
            IntPtr address = baseAddress;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length; i++)
            {
                if (!Imports.ReadProcessMemory(handle, address, buffer, buffer.Length, IntPtr.Zero)) return IntPtr.Zero;
                address = IntPtr.Size == 4 ? (IntPtr)BitConverter.ToInt32(buffer, 0) : (IntPtr)BitConverter.ToInt64(buffer, 0);
                address += offsets[i];
            }
            if (offsets.Length == 0)
            {
                if (!Imports.ReadProcessMemory(handle, address, buffer, buffer.Length, IntPtr.Zero)) return IntPtr.Zero;
                address = IntPtr.Size == 4 ? (IntPtr)BitConverter.ToInt32(buffer, 0) : (IntPtr)BitConverter.ToInt64(buffer, 0);
            }
            return address;
        }

        public T Read<T>(IntPtr baseAddress, params IntPtr[] offsets) where T : struct
        {
            return MemoryMarshal.Read<T>(ReadBytes(baseAddress, Marshal.SizeOf<T>(), offsets));
        }

        public bool ReadArray<T>(IntPtr baseAddress, T[] values, params IntPtr[] offsets) where T : struct
        {
            int size = Marshal.SizeOf<T>() * values.Length;
            byte[] buffer = ReadBytes(baseAddress, size, offsets);
            if (buffer.Length != size) return false;
            MemoryMarshal.Cast<byte, T>(buffer).CopyTo(values);
            return true;
        }

        public string ReadString(IntPtr address, int length, Encoding encoding, params IntPtr[] offsets)
        {
            return encoding.GetString(ReadBytes(address, length, offsets));
        }

        public bool WriteBytes(IntPtr baseAddress, byte[] bytes, params IntPtr[] offsets)
        {
            IntPtr address = baseAddress;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                if (!Imports.ReadProcessMemory(handle, address + offsets[i], buffer, buffer.Length, IntPtr.Zero)) return false;
                address = IntPtr.Size == 4 ? (IntPtr)BitConverter.ToInt32(buffer, 0) : (IntPtr)BitConverter.ToInt64(buffer, 0);
            }
            if (offsets.Length > 0) address += offsets[^1]; 
            return Imports.WriteProcessMemory(handle, address, bytes, bytes.Length, IntPtr.Zero);
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

        public List<IntPtr> AOBScanRegions(List<Imports.MEMORY_BASIC_INFORMATION> regions, byte?[] pattern, Masks mask = null)
        {
            uint uintMask = mask?.Value ?? Masks.ReadableMask.Value;
            int patternLength = pattern.Length;
            ConcurrentBag<IntPtr> results = new ConcurrentBag<IntPtr>();
            Parallel.ForEach(regions, region =>
            {
                if (((uint)region.Protect & uintMask) == 0) return;
                long regionSize = (long)region.RegionSize;
                if (regionSize < patternLength) return;
                byte[] buffer = new byte[(int)regionSize];
                if (!Imports.ReadProcessMemory(handle, region.BaseAddress, buffer, buffer.Length, IntPtr.Zero)) return;
                int limit = buffer.Length - patternLength + 1;
                for (int i = 0; i < limit; i++)
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
            });
            return results.ToList();
        }

        public List<IntPtr> AOBScanModuleRegions(string module, string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanModuleRegions(GetModule(module), pattern, patternMask, mask);
        }

        public List<IntPtr> AOBScanModuleRegions(ProcessModule module, string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(new List<Imports.MEMORY_BASIC_INFORMATION> { new Imports.MEMORY_BASIC_INFORMATION { BaseAddress = module.BaseAddress, RegionSize = module.ModuleMemorySize, State = Imports.MemoryState.Commit, Type = Imports.MemoryType.Image } }, PatternToBytes(pattern, patternMask), mask);
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

        public List<IntPtr> AOBScanFreeOrReservedRegions(string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(FreeOrReservedMemoryRegions, PatternToBytes(pattern, patternMask), mask);
        }

        public List<IntPtr> AOBScanRegion(Imports.MEMORY_BASIC_INFORMATION region, string pattern, string patternMask = null, Masks mask = null)
        {
            return AOBScanRegions(new List<Imports.MEMORY_BASIC_INFORMATION> { region }, PatternToBytes(pattern, patternMask), mask);
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