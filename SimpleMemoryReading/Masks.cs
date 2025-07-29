namespace SimpleMemoryReading64and32
{
    public class Masks
    {
        public uint Value;

        public Masks(uint value)
        {
            Value = value;
        }

        public static Masks ReadableMask { get; } = new Masks((uint)(Imports.MemoryProtect.ReadOnly | Imports.MemoryProtect.ReadWrite | Imports.MemoryProtect.ExecuteRead | Imports.MemoryProtect.ExecuteReadWrite | Imports.MemoryProtect.ExecuteWriteCopy | Imports.MemoryProtect.WriteCopy));

        public static Masks WritableMask { get; } = new Masks((uint)(Imports.MemoryProtect.ReadWrite | Imports.MemoryProtect.ExecuteReadWrite | Imports.MemoryProtect.ExecuteWriteCopy | Imports.MemoryProtect.WriteCopy)); 
    }
}
