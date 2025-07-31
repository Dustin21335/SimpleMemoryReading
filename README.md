# Simple memory reading and writing library by [Dustin](https://github.com/Dustin21335)

[![](https://img.shields.io/nuget/v/SimpleMemoryReading.svg?style=for-the-badge)](https://www.nuget.org/packages/SimpleMemoryReading/)
[![](https://img.shields.io/nuget/dt/SimpleMemoryReading.svg?style=for-the-badge)](https://www.nuget.org/packages/SimpleMemoryReading/)

## Features
<details>

  ### Simple Memory Reading

  #### Initializing
  - **Simple Memory Reading (String) (SimpleMemoryReading):** Initializes using the name of the process.
  - **Simple Memory Reading (Int) (SimpleMemoryReading):** Initializes using the process ID.
  - **Simple Memory Reading (Process) (SimpleMemoryReading):** Initializes using Process.
  
  #### Properties
  - **Process () (Process):** Gets the process
  - **Handle () (IntPtr):** Gets the process handle
  - **Is 64 () (Bool):** Gets if the process is 64 bit
  - **All Regions () (List<Imports.Region>):** Gets all regions of the process
  - **Module Memory Regions () (List<Imports.Region>):** Gets module regions of the process
  - **Mapped Memory Regions () (List<Imports.Region>):** Gets mapped regions of the process
  - **Private Memory Regions () (List<Imports.Region>):** Gets private regions of the process
  - **Free Memory Regions () (List<Imports.Region>):** Gets free regions of the process
  - **Reserved Memory Regions () (List<Imports.Region>):** Gets reserved regions of the process
  
  #### Methods
  - **Initialize (Process) ():** Used for initializing
  - **Get Module (String) (ProcessModule):** Gets Process Module
  - **Get Module Base (String) (IntPtr):** Gets Process Module address base intptr
  - **Get Regions () (Imports.Region):** Gets all regions in the process
  - **Read Bytes (Byte[]) (IntPtr, Int, IntPtr[]):** Gets bytes from an address 
  - **Read Bytes (Byte[]) (IntPtr, IntPtr[]):** Gets bytes from an address
  - **Read Pointer (IntPtr) (IntPtr, IntPtr[]):** Gets the address the provided address points to
  - **Read (T) (IntPtr, IntPtr[]):** Gets the value of any single value excluding strings
  - **Read Array (T[]) (IntPtr, IntPtr[]):** Gets the value of any array value
  - **Read String (String) (IntPtr, Int, Encoding, IntPtr[]):** Gets the value of a string
  - **Write Bytes (Bool) (IntPtr, Byte[], Int, IntPtr[]):** Write bytes to an address
  - **Write Bytes (Bool) (IntPtr, Byte[], IntPtr[]):** Write bytes to an address
  - **Write (bool) (IntPtr, T, IntPtr[]):** Writes the value of any single value excluding strings
  - **Write Array (Bool) (IntPtr, T[], IntPtr[]):** Write the value of any array value
  - **Write String (Bool) (IntPtr, String, Encoding, IntPtr[]):** Writes the value of a string
  - **AOB Scan Region (List<IntPtr>) (Imports.Region, byte[], Masks):** Scans a region that use a specific array of bytes
  - **AOB Scan Region (List<IntPtr>) (Imports.Region, String, String, Masks):** Scans a region that use a specific array of bytes 
  - **AOB Scan Regions (List<IntPtr>) (Imports.Region, byte[], Masks):** Scans regions that use a specific array of bytes
  - **AOB Scan Regions (List<IntPtr>) (Imports.Region, String, String, Masks):** Scans regions that use a specific array of bytes 
  - **AOB Scan Module Regions (List<IntPtr>) (String, String, String, Masks):** Scans the regions of a specific module
  - **AOB Scan Module Regions (List<IntPtr>) (ProcessModule, String, String, Masks):** Scans the regions of a specific module
  - **AOB Scan Module Regions (List<IntPtr>) (String, String, Masks):** Scans all module regions that use a specific array of bytes
  - **AOB Scan Mapped Regions (List<IntPtr>) (String, String, Masks):** Scans all mapped regions that use a specific array of bytes
  - **AOB Scan Private Regions (List<IntPtr>) (String, String, Masks):** Scans all private regions that use a specific array of bytes
  - **AOB Scan Free Regions (List<IntPtr>) (String, String, Masks):** Scans all free regions that use a specific array of bytes
  - **AOB Scan Reserved Regions (List<IntPtr>) (String, String, Masks):** Scans all reserved regions that use a specific array of bytes
  - **Pattern To Bytes (List<IntPtr>) (String, String):** Converts a string to bytes

  ### Imports

  - **Region (IntPtr, IntPtr, MemoryProtect, IntPtr, MemoryState, MemoryProtect, MemoryType):** This contains BaseAddress, AllocationBase, AllocationProtect, RegionSize, State, Protect, Type
  - **MemoryState (Uint):** This contains Commit, Reserve, Free
  - **MemoryType (Uint):** This contains Private, Mapped, Image
  - **MemoryProtect (Uint):** This contains NoAccess, ReadOnly, ReadWrite, WriteCopy, Execute, ExecuteRead, ExecuteReadCopy, Guard, NoCache, WriteCombine

  ### Masks

  #### Initializing
  - **Masks (Uint):** Initializes using Uint
  
  #### Properties
  - **ReadableMask (Mask):** This contains the ReadOnly, ReadWrite, ExecuteRead, ExecuteReadWrite, ExecuteWriteCopy, and WriteCopy from MemoryProtect
  - **WritableMask (Mask):** This contains the ReadWrite, ExecuteReadWrite, ExecuteReadCopy, ExecuteWriteCopy, WriteCopy from MemoryProtect

</details>
