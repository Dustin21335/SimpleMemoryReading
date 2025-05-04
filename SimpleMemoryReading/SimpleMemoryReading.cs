using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SimpleMemoryReading64and32
{
    public class SimpleMemoryReading
    {
        public SimpleMemoryReading(string process)
        {
            this.process = Process.GetProcessesByName(process)[0];
            this.handle = this.process.Handle;
        }

        public SimpleMemoryReading(int id)
        {
            this.process = Process.GetProcessById(id);
            this.handle = process.Handle;
        }

        public SimpleMemoryReading(Process process)
        {
            this.process = process;
            this.handle = process.Handle;
        }

        public Process process;
        private IntPtr handle;

        public ProcessModule GetModule(string module)
        {
            process.Refresh();
            if (module.Contains(".exe") && process.MainModule != null) return process.MainModule;
            return process.Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName == module);
        }

        public IntPtr GetModuleBase(string module)
        {
            return GetModule(module).BaseAddress;
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

        public int ReadInt(IntPtr baseAddress, params int[] offsets)
        {
            return BitConverter.ToInt32(ReadBytes(baseAddress, 4, offsets), 0);
        }

        public long ReadLong(IntPtr baseAddress, params int[] offsets)
        {
            return BitConverter.ToInt64(ReadBytes(baseAddress, 8, offsets), 0);
        }

        public float ReadFloat(IntPtr baseAddress, params int[] offsets)
        {
            return BitConverter.ToSingle(ReadBytes(baseAddress, 4, offsets), 0);
        }

        public double ReadDouble(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToDouble(ReadBytes(address, 8, offsets), 0);
        }

        public Vector3 ReadVec(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 12, offsets);
            return new Vector3(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8));
        }

        public double[] ReadDoubleVec(IntPtr address, params int[] offsets)
        {
            byte[] value = ReadBytes(address, 24, offsets);
            return new[] { BitConverter.ToDouble(value, 0), BitConverter.ToDouble(value, 8), BitConverter.ToDouble(value, 16) };
        }

        public short ReadShort(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToInt16(ReadBytes(address, 2, offsets), 0);
        }

        public ushort ReadUShort(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToUInt16(ReadBytes(address, 2, offsets), 0);
        }

        public uint ReadUInt(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToUInt32(ReadBytes(address, 4, offsets), 0);
        }

        public ulong ReadULong(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToUInt64(ReadBytes(address, 8, offsets), 0);
        }

        public bool ReadBool(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToBoolean(ReadBytes(address, 1, offsets), 0);
        }

        public string ReadString(IntPtr address, int length, params int[] offsets)
        {
            return Encoding.UTF8.GetString(ReadBytes(address, length, offsets));
        }

        public char ReadChar(IntPtr address, params int[] offsets)
        {
            return BitConverter.ToChar(ReadBytes(address, 2, offsets), 0);
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

        public bool WriteHexBytes(IntPtr baseAddress, string hexBytes, params int[] offsets)
        {
            string[] array = hexBytes.Split(' ');
            byte[] bytes = new byte[array.Length];
            for (int i = 0; i < array.Length; i++) bytes[i] = Convert.ToByte(array[i], 16);
            return WriteBytes(baseAddress, bytes, offsets);
        }

        public bool WriteInt(IntPtr address, int value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteLong(IntPtr address, long value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteFloat(IntPtr address, float value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteDouble(IntPtr address, double value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteVec(IntPtr address, Vector3 value, params int[] offsets)
        {
            return WriteBytes(address, new[] { value.X, value.Y, value.Z }.SelectMany(BitConverter.GetBytes).ToArray(), offsets);
        }

        public bool WriteDoubleVec(IntPtr address, double[] value, params int[] offsets)
        {
            return WriteBytes(address, value.SelectMany(BitConverter.GetBytes).ToArray(), offsets);
        }

        public bool WriteShort(IntPtr address, short value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteUShort(IntPtr address, ushort value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteUInt(IntPtr address, uint value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteULong(IntPtr address, ulong value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteBool(IntPtr address, bool value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteString(IntPtr address, string value, params int[] offsets)
        {
            return WriteBytes(address, Encoding.UTF8.GetBytes(value), offsets);
        }

        public bool WriteChar(IntPtr address, char value, params int[] offsets)
        {
            return WriteBytes(address, BitConverter.GetBytes(value), offsets);
        }

        public bool WriteMatrix(IntPtr address, float[] matrix, params int[] offsets)
        {
            byte[] bytes = new byte[64];
            for (int i = 0; i < 16; i++) Array.Copy(BitConverter.GetBytes(matrix[i]), 0, bytes, i * 4, 4);
            return WriteBytes(address, bytes, offsets);
        }

        public bool WriteNop(IntPtr address, int length, params int[] offsets)
        {
            return WriteBytes(address, Enumerable.Repeat((byte)144, length).ToArray());
        }

        public IntPtr ScanForBytes(string module, string needle)
        {
            return ScanForBytes(File.ReadAllBytes(GetModule(module).FileName), needle.Split(' ').Select(b => Convert.ToByte(b, 16)).ToArray());
        }

        public IntPtr ScanForBytes(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (haystack.Skip(i).Take(needle.Length).SequenceEqual(needle)) return (IntPtr)i;
            }
            return IntPtr.Zero;
        }
    }
}