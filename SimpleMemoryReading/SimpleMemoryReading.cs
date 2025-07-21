using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
            this.ModuleMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_IMAGE);
            this.MappedMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_MAPPED);
            this.PrivateMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_PRIVATE);
            this.FreeOrReservedMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MEM_FREE || r.State == Imports.MEM_RESERVE);
        }

        public SimpleMemoryReading(int id)
        {
            this.process = Process.GetProcessById(id);
            this.handle = process.Handle;
            this.AllRegions = GetRegions();
            this.ModuleMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_IMAGE);
            this.MappedMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_MAPPED);
            this.PrivateMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_PRIVATE);
            this.FreeOrReservedMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MEM_FREE || r.State == Imports.MEM_RESERVE);
        }

        public SimpleMemoryReading(Process process)
        {
            this.process = process;
            this.handle = process.Handle;
            this.AllRegions = GetRegions();
            this.ModuleMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_IMAGE);
            this.MappedMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_MAPPED);
            this.PrivateMemoryRegions = AllRegions.FindAll(r => r.Type == Imports.MEM_PRIVATE);
            this.FreeOrReservedMemoryRegions = AllRegions.FindAll(r => r.State == Imports.MEM_FREE || r.State == Imports.MEM_RESERVE);
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

        public byte[] ReadBytes(IntPtr baseAddress, int bytes, params int[] offsets)
        {
            IntPtr address = baseAddress;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                Imports.ReadProcessMemory(handle, address + offsets[i], buffer, buffer.Length, IntPtr.Zero);
                address = IntPtr.Size == 4 ? (IntPtr)(BitConverter.ToInt32(buffer, 0)) : (IntPtr)(BitConverter.ToInt64(buffer, 0));
            }
            if (offsets.Length > 0) address += offsets[offsets.Length - 1];
            byte[] result = new byte[bytes];
            Imports.ReadProcessMemory(handle, address, result, result.Length, IntPtr.Zero);
            return result;
        }

        public byte[] ReadBytes(IntPtr baseAddress, int bytes, params IntPtr[] offsets)
        {
            return ReadBytes(baseAddress, bytes, offsets.Select(o => (int)o).ToArray());
        }

        public byte[] ReadBytes(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            Imports.ReadProcessMemory(handle, address, buffer, size, IntPtr.Zero);
            return buffer;
        }

        public IntPtr ReadPointer(IntPtr baseAddress, params int[] offsets)
        {
            IntPtr address = baseAddress;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length; i++)
            {
                if (!Imports.ReadProcessMemory(handle, address, buffer, buffer.Length, IntPtr.Zero)) return IntPtr.Zero;
                address = IntPtr.Size == 4 ? (IntPtr)BitConverter.ToInt32(buffer, 0) : (IntPtr)BitConverter.ToInt64(buffer, 0);
                address += offsets[i];
            }
            return address;
        }

        public IntPtr ReadPointer(IntPtr address, params IntPtr[] offsets)
        {
            return ReadPointer(address, offsets.Select(o => (int)o).ToArray());
        }

        public IntPtr ReadPointer(IntPtr address)
        {
            byte[] buffer = new byte[IntPtr.Size];
            return Imports.ReadProcessMemory(handle, address, buffer, buffer.Length, IntPtr.Zero) ? (IntPtr)(IntPtr.Size == 4 ? BitConverter.ToInt32(buffer, 0) : BitConverter.ToInt64(buffer, 0)) : IntPtr.Zero;
        }

        public int ReadInt(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToInt32(ReadBytes(address, 4, offsets), 0);
        }

        public int ReadInt(IntPtr address, params IntPtr[] offsets)
        {
            return ReadInt(address, offsets.Select(o => (int)o).ToArray());
        }

        public int ReadInt(IntPtr address)
        {
            return BitConverter.ToInt32(ReadBytes(address, 4), 0);
        }

        public long ReadLong(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToInt64(ReadBytes(address, 8, offsets), 0);
        }

        public long ReadLong(IntPtr address, params IntPtr[] offsets)
        {
            return ReadLong(address, offsets.Select(o => (int)o).ToArray());
        }

        public long ReadLong(IntPtr address)
        {
            return BitConverter.ToInt64(ReadBytes(address, 8), 0);
        }

        public float ReadFloat(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToSingle(ReadBytes(address, 4, offsets), 0);
        }

        public float ReadFloat(IntPtr address, params IntPtr[] offsets)
        {
            return ReadFloat(address, offsets.Select(o => (int)o).ToArray());
        }

        public float ReadFloat(IntPtr address)
        {
            return BitConverter.ToSingle(ReadBytes(address, 4), 0);
        }

        public double ReadDouble(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToDouble(ReadBytes(address, 8, offsets), 0);
        }

        public double ReadDouble(IntPtr address, params IntPtr[] offsets)
        {
            return ReadDouble(address, offsets.Select(o => (int)o).ToArray());
        }

        public double ReadDouble(IntPtr address)
        {
            return BitConverter.ToDouble(ReadBytes(address, 8), 0);
        }

        public Vector2 ReadVector2(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 8, offsets);
            return new Vector2(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4));
        }

        public Vector2 ReadVector2(IntPtr address, params IntPtr[] offsets)
        {
            return ReadVector2(address, offsets.Select(o => (int)o).ToArray());
        }

        public Vector2 ReadVector2(IntPtr address)
        {
            byte[] value = ReadBytes(address, 8);
            return new Vector2(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4));
        }

        public Vector3 ReadVector3(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 12, offsets);
            return new Vector3(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8));
        }

        public Vector3 ReadVector3(IntPtr address, params IntPtr[] offsets)
        {
            return ReadVector3(address, offsets.Select(o => (int)o).ToArray());
        }

        public Vector3 ReadVector3(IntPtr address)
        {
            byte[] value = ReadBytes(address, 12);
            return new Vector3(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8));
        }

        public Vector4 ReadVector4(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 16, offsets);
            return new Vector4(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8), BitConverter.ToSingle(value, 12));
        }

        public Vector4 ReadVector4(IntPtr address, params IntPtr[] offsets)
        {
            return ReadVector4(address, offsets.Select(o => (int)o).ToArray());
        }

        public Vector4 ReadVector4(IntPtr address)
        {
            byte[] value = ReadBytes(address, 16);
            return new Vector4(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8), BitConverter.ToSingle(value, 12));
        }

        public double[] ReadDoubleVector2(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 16, offsets);
            return [BitConverter.ToDouble(value, 0), BitConverter.ToDouble(value, 8)];
        }

        public double[] ReadDoubleVector2(IntPtr address, params IntPtr[] offsets)
        {
            return ReadDoubleVector2(address, offsets.Select(o => (int)o).ToArray());
        }

        public double[] ReadDoubleVector2(IntPtr address)
        {
            byte[] value = ReadBytes(address, 16);
            return [BitConverter.ToDouble(value, 0), BitConverter.ToDouble(value, 8)];
        }

        public double[] ReadDoubleVector3(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 24, offsets);
            return [BitConverter.ToDouble(value, 0), BitConverter.ToDouble(value, 8), BitConverter.ToDouble(value, 16)];
        }

        public double[] ReadDoubleVector3(IntPtr address, params IntPtr[] offsets)
        {
            return ReadDoubleVector3(address, offsets.Select(o => (int)o).ToArray());
        }

        public double[] ReadDoubleVector3(IntPtr address)
        {
            byte[] value = ReadBytes(address, 24);
            return [BitConverter.ToDouble(value, 0), BitConverter.ToDouble(value, 8), BitConverter.ToDouble(value, 16)];
        }

        public double[] ReadDoubleVector4(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 32, offsets);
            return [BitConverter.ToDouble(value, 0), BitConverter.ToDouble(value, 8), BitConverter.ToDouble(value, 16), BitConverter.ToDouble(value, 24)];
        }

        public double[] ReadDoubleVector4(IntPtr address, params IntPtr[] offsets)
        {
            return ReadDoubleVector4(address, offsets.Select(o => (int)o).ToArray());
        }

        public double[] ReadDoubleVector4(IntPtr address)
        {
            byte[] value = ReadBytes(address, 32);
            return [BitConverter.ToDouble(value, 0), BitConverter.ToDouble(value, 8), BitConverter.ToDouble(value, 16), BitConverter.ToDouble(value, 24)];
        }

        public Quaternion ReadQuaternion(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 16, offsets);
            return new Quaternion(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8), BitConverter.ToSingle(value, 12));
        }

        public Quaternion ReadQuaternion(IntPtr address, params IntPtr[] offsets)
        {
            return ReadQuaternion(address, offsets.Select(o => (int)o).ToArray());
        }

        public Quaternion ReadQuaternion(IntPtr address)
        {
            byte[] value = ReadBytes(address, 16);
            return new Quaternion(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8), BitConverter.ToSingle(value, 12));
        }

        public short ReadShort(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToInt16(ReadBytes(address, 2, offsets), 0);
        }

        public short ReadShort(IntPtr address, params IntPtr[] offsets)
        {
            return ReadShort(address, offsets.Select(o => (int)o).ToArray());
        }

        public short ReadShort(IntPtr address)
        {
            return BitConverter.ToInt16(ReadBytes(address, 2), 0);
        }

        public ushort ReadUShort(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToUInt16(ReadBytes(address, 2, offsets), 0);
        }

        public ushort ReadUShort(IntPtr address, params IntPtr[] offsets)
        {
            return ReadUShort(address, offsets.Select(o => (int)o).ToArray());
        }

        public ushort ReadUShort(IntPtr address)
        {
            return BitConverter.ToUInt16(ReadBytes(address, 2), 0);
        }

        public uint ReadUInt(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToUInt32(ReadBytes(address, 4, offsets), 0);
        }

        public uint ReadUInt(IntPtr address, params IntPtr[] offsets)
        {
            return ReadUInt(address, offsets.Select(o => (int)o).ToArray());
        }

        public uint ReadUInt(IntPtr address)
        {
            return BitConverter.ToUInt32(ReadBytes(address, 4), 0);
        }

        public ulong ReadULong(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToUInt64(ReadBytes(address, 8, offsets), 0);
        }

        public ulong ReadULong(IntPtr address, params IntPtr[] offsets)
        {
            return ReadULong(address, offsets.Select(o => (int)o).ToArray());
        }

        public ulong ReadULong(IntPtr address)
        {
            return BitConverter.ToUInt64(ReadBytes(address, 8), 0);
        }

        public bool ReadBool(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToBoolean(ReadBytes(address, 1, offsets), 0);
        }

        public bool ReadBool(IntPtr address, params IntPtr[] offsets)
        {
            return ReadBool(address, offsets.Select(o => (int)o).ToArray());
        }

        public bool ReadBool(IntPtr address)
        {
            return BitConverter.ToBoolean(ReadBytes(address, 1), 0);
        }

        public string ReadString(IntPtr address, int length, Encoding encoding, params int[] offsets)
        {
            return encoding.GetString(ReadBytes(address, length, offsets));
        }

        public string ReadString(IntPtr address, int length, Encoding encoding, params IntPtr[] offsets)
        {
            return ReadString(address, length, encoding, offsets.Select(o => (int)o).ToArray());
        }

        public string ReadString(IntPtr address, int length, Encoding encoding)
        {
            return encoding.GetString(ReadBytes(address, length));
        }

        public char ReadChar(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToChar(ReadBytes(address, 2, offsets), 0);
        }

        public char ReadChar(IntPtr address, params IntPtr[] offsets)
        {
            return ReadChar(address, offsets.Select(o => (int)o).ToArray());
        }

        public char ReadChar(IntPtr address)
        {
            return BitConverter.ToChar(ReadBytes(address, 2), 0);
        }

        public float[] ReadMatrix(IntPtr address, params int[] offsets)
        {
            byte[] array = ReadBytes(address, 64, offsets);
            float[] matrix = new float[16];
            for (int i = 0; i < 16; i++) matrix[i] = BitConverter.ToSingle(array, i * 4);
            return matrix;
        }

        public float[] ReadMatrix(IntPtr address, params IntPtr[] offsets)
        {
            return ReadMatrix(address, offsets.Select(o => (int)o).ToArray());
        }

        public float[] ReadMatrix(IntPtr address)
        {
            byte[] array = ReadBytes(address, 64);
            float[] matrix = new float[16];
            for (int i = 0; i < 16; i++) matrix[i] = BitConverter.ToSingle(array, i * 4);
            return matrix;
        }

        public bool WriteBytes(IntPtr baseAddress, byte[] bytes, params int[] offsets)
        {
            IntPtr address = baseAddress;
            byte[] buffer = new byte[IntPtr.Size];
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                Imports.ReadProcessMemory(handle, address + offsets[i], buffer, buffer.Length, IntPtr.Zero);
                address = (IntPtr)(IntPtr.Size == 4 ? BitConverter.ToInt32(buffer, 0) : BitConverter.ToInt64(buffer, 0));
            }
            if (offsets.Length > 0) address += offsets[offsets.Length - 1];
            return Imports.WriteProcessMemory(handle, address, bytes, bytes.Length, IntPtr.Zero);
        }

        public bool WriteBytes(IntPtr baseAddress, byte[] bytes, params IntPtr[] offsets)
        {
            return WriteBytes(baseAddress, bytes, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteBytes(IntPtr address, byte[] bytes)
        {
            return Imports.WriteProcessMemory(handle, address, bytes, bytes.Length, IntPtr.Zero);
        }

        public bool WriteHexBytes(IntPtr baseAddress, string hexBytes, params int[] offsets)
        {
            string[] array = hexBytes.Split(' ');
            byte[] bytes = new byte[array.Length];
            for (int i = 0; i < array.Length; i++) bytes[i] = Convert.ToByte(array[i], 16);
            return WriteBytes(baseAddress, bytes, offsets);
        }

        public bool WriteHexBytes(IntPtr baseAddress, string hexBytes, params IntPtr[] offsets)
        {
            return WriteHexBytes(baseAddress, hexBytes, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteHexBytes(IntPtr baseAddress, string hexBytes)
        {
            string[] array = hexBytes.Split(' ');
            byte[] bytes = new byte[array.Length];
            for (int i = 0; i < array.Length; i++) bytes[i] = Convert.ToByte(array[i], 16);
            return WriteBytes(baseAddress, bytes);
        }

        public bool WriteInt(IntPtr address, int value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteInt(IntPtr address, int value, params IntPtr[] offsets)
        {
            return WriteInt(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteInt(IntPtr address, int value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteLong(IntPtr address, long value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteLong(IntPtr address, long value, params IntPtr[] offsets)
        {
            return WriteLong(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteLong(IntPtr address, long value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteFloat(IntPtr address, float value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteFloat(IntPtr address, float value, params IntPtr[] offsets)
        {
            return WriteFloat(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteFloat(IntPtr address, float value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteDouble(IntPtr address, double value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteDouble(IntPtr address, double value, params IntPtr[] offsets)
        {
            return WriteDouble(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteDouble(IntPtr address, double value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteVector3(IntPtr address, Vector3 value, params int[] offsets)
        {
            return WriteBytes(address, new[] { value.X, value.Y, value.Z }.SelectMany(BitConverter.GetBytes).ToArray(), offsets);
        }

        public bool WriteVector3(IntPtr address, Vector3 value, params IntPtr[] offsets)
        {
            return WriteVector3(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteVector3(IntPtr address, Vector3 value)
        {
            return WriteBytes(address, new[] { value.X, value.Y, value.Z }.SelectMany(BitConverter.GetBytes).ToArray());
        }

        public bool WriteDoubleVec(IntPtr address, double[] value, params int[] offsets)
        {
            return WriteBytes(address, value.SelectMany(BitConverter.GetBytes).ToArray(), offsets);
        }

        public bool WriteDoubleVec(IntPtr address, double[] value, params IntPtr[] offsets)
        {
            return WriteDoubleVec(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteDoubleVec(IntPtr address, double[] value)
        {
            return WriteBytes(address, value.SelectMany(BitConverter.GetBytes).ToArray());
        }

        public bool WriteShort(IntPtr address, short value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteShort(IntPtr address, short value, params IntPtr[] offsets)
        {
            return WriteShort(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteShort(IntPtr address, short value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteUShort(IntPtr address, ushort value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteUShort(IntPtr address, ushort value, params IntPtr[] offsets)
        {
            return WriteUShort(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteUShort(IntPtr address, ushort value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteUInt(IntPtr address, uint value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteUInt(IntPtr address, uint value, params IntPtr[] offsets)
        {
            return WriteUInt(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteUInt(IntPtr address, uint value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteULong(IntPtr address, ulong value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteULong(IntPtr address, ulong value, params IntPtr[] offsets)
        {
            return WriteULong(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteULong(IntPtr address, ulong value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteBool(IntPtr address, bool value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteBool(IntPtr address, bool value, params IntPtr[] offsets)
        {
            return WriteBool(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteBool(IntPtr address, ulong value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteBool(IntPtr address, int value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteBool(IntPtr address, int value, params IntPtr[] offsets)
        {
            return WriteBool(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteBool(IntPtr address, int value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteString(IntPtr address, string value, params int[] offsets)
        {
            return WriteBytes(address, Encoding.UTF8.GetBytes(value), offsets);
        }

        public bool WriteString(IntPtr address, string value, params IntPtr[] offsets)
        {
            return WriteString(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteString(IntPtr address, string value)
        {
            return WriteBytes(address, Encoding.UTF8.GetBytes(value));
        }

        public bool WriteChar(IntPtr address, char value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteChar(IntPtr address, char value, params IntPtr[] offsets)
        {
            return WriteChar(address, value, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteChar(IntPtr address, char value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public bool WriteMatrix(IntPtr address, float[] matrix, params int[] offsets)
        {
            byte[] bytes = new byte[64];
            for (int i = 0; i < 16; i++) Array.Copy(BitConverter.GetBytes(matrix[i]), 0, bytes, i * 4, 4);
            return WriteBytes(address, bytes, offsets);
        }

        public bool WriteMatrix(IntPtr address, float[] matrix, params IntPtr[] offsets)
        {
            return WriteMatrix(address, matrix, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteChar(IntPtr address, float[] matrix)
        {
            byte[] bytes = new byte[64];
            for (int i = 0; i < 16; i++) Array.Copy(BitConverter.GetBytes(matrix[i]), 0, bytes, i * 4, 4);
            return WriteBytes(address, bytes);
        }

        public bool WriteNop(IntPtr address, int length, params int[] offsets)
        {
            return WriteBytes(address, Enumerable.Repeat((byte)144, length).ToArray(), offsets);
        }

        public bool WriteNop(IntPtr address, int length, params IntPtr[] offsets)
        {
            return WriteNop(address, length, offsets.Select(o => (int)o).ToArray());
        }

        public bool WriteChar(IntPtr address, int length)
        {
            return WriteBytes(address, Enumerable.Repeat((byte)144, length).ToArray());
        }

        public List<IntPtr> AOBScanRegions(List<Imports.MEMORY_BASIC_INFORMATION> regions, byte?[] pattern)
        {
            int patternLength = pattern.Length;
            ConcurrentBag<IntPtr> results = new ConcurrentBag<IntPtr>();
            Parallel.ForEach(regions, region =>
            {
                if (region.State != 0x1000 || (region.Protect & (0x04 | 0x02 | 0x20)) == 0) return;
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

        public List<IntPtr> AOBScanModuleRegions(string module, string pattern, string mask = null)
        {
            return AOBScanModuleRegions(GetModule(module), pattern, mask);
        }

        public List<IntPtr> AOBScanModuleRegions(ProcessModule module, string pattern, string mask = null)
        {
            Imports.MEMORY_BASIC_INFORMATION region = new Imports.MEMORY_BASIC_INFORMATION
            {
                BaseAddress = module.BaseAddress,
                RegionSize = module.ModuleMemorySize,
                State = 0x1000, 
                Protect = 0x04,
                Type = Imports.MEM_IMAGE
            };
            return AOBScanRegions(new List<Imports.MEMORY_BASIC_INFORMATION> { region }, PatternToBytes(pattern, mask));
        }

        public List<IntPtr> AOBScanModuleRegions(string pattern, string mask = null)
        {
            return AOBScanRegions(ModuleMemoryRegions, PatternToBytes(pattern, mask));
        }

        public List<IntPtr> AOBScanMappedRegions(string pattern, string mask = null)
        {
            return AOBScanRegions(MappedMemoryRegions, PatternToBytes(pattern, mask));
        }

        public List<IntPtr> AOBScanPrivateRegions(string pattern, string mask = null)
        {
            return AOBScanRegions(PrivateMemoryRegions, PatternToBytes(pattern, mask));
        }

        public List<IntPtr> AOBScanFreeOrReservedRegions(string pattern, string mask = null)
        {
            return AOBScanRegions(FreeOrReservedMemoryRegions, PatternToBytes(pattern, mask));
        }

        public List<IntPtr> AOBScanRegion(Imports.MEMORY_BASIC_INFORMATION region, string pattern, string mask = null)
        {
            return AOBScanRegions(new List<Imports.MEMORY_BASIC_INFORMATION> { region }, PatternToBytes(pattern, mask));
        }

        public byte?[] PatternToBytes(string pattern, string mask = null)
        {
            if (!string.IsNullOrEmpty(mask))
            {
                MatchCollection matches = Regex.Matches(pattern, @"\\x([0-9A-Fa-f]{2})");
                int len = mask?.Length ?? matches.Count;
                byte?[] result = new byte?[len];
                for (int i = 0; i < len; i++) result[i] = mask != null && mask[i] == '?' ? null : Convert.ToByte(matches[i].Groups[1].Value, 16);
                return result;
            }
            return pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(b => b == "?" || b == "??" ? (byte?)null : Convert.ToByte(b, 16)).ToArray();
        }
    }
}