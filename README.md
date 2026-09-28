# AdsSyncClient

A .NET library for synchronized bidirectional data exchange between .NET applications and TwinCAT ADS clients.


## Overview

**AdsSyncClient** provides a seamless synchronization mechanism between `.NET` objects (implementing `INotifyPropertyChanged`) and **Beckhoff TwinCAT** ADS variables. It handles the complete lifecycle of ADS connections, automatic reconnection, data marshalling, and real-time property synchronization with full async/await support. The goal of this library is to drastically simplify the integration of ADS into .NET applications.

Perfect for industrial automation scenarios where you need reliable communication between HMI applications (WPF, Blazor, WinForms etc.) and PLC systems.

The entire code is annotated with XML comments to make everything as clear as possible. Below is a quick-start guide that explains how to implement this library in your application.


## Key Features

| Feature                   | Description                                                   |
|---------------------------|---------------------------------------------------------------|
| **Bidirectional Sync**    | Automatic data flow .NET ↔ TwinCAT via property notifications |
| **Auto-Reconnection**     | Built-in reconnection logic with cyclic state monitoring      |
| **ObservableCollections** | Native support for dynamic array synchronization              |
| **Async/Await**           | Full asynchronous design throughout                           |
| **MVVM Compatible**       | Works seamlessly with CommunityToolkit.Mvvm                   |
| **Zero Boilerplate**      | Minimal setup, automatic variable handle management           |
| **Error Handling**        | Event-based exception propagation for graceful error recovery |


## Requirements

 * .NET 8.0 or higher
 * Microsoft Visual Studio 2022 or higher
 * Beckhoff TwinCAT 3 XAR or Beckhoff TwinCAT 3 XAE (tested with version 4024.78)
 * Beckhoff.TwinCAT.Ads NuGet package
 * CommunityToolkit.Mvvm NuGet package


## Core Components

| Component                              | Responsibility                                     |
|----------------------------------------|----------------------------------------------------|
| **`AdsSyncClient`**                    | Entry point, public API, lifecycle management      |
| **`AdsConnectionManager`**             | Connection pooling, reconnection, state polling    |
| **`AdsDataSynchronizer`**              | Data mapping, notification handling, serialization |
| **`AdsReadMapper` / `AdsWriteMapper`** | Direction-specific ADS operations                  |
| **`Marshalling`**                      | Dynamic type generation for P/Invoke compatibility |


## Solution Structure

| Project / Folder                | Description                                                                                                               |
|---------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| **`AdsSync`**                   | The library itself                                                                                                        |
| **`AdsSyncClientWpfDemo`**      | Simple WPF MVVM application to illustrate the integration of the library                                                  |
| **`AdsSyncClientAvaloniaDemo`** | Simple Avalonia MVVM application to illustrate the integration of the library (for future cross plattform functionality)  |
| **`AdsSyncClientPlcDemo`**      | Simple TwinCAT PLC application and the counterpart to AdsSyncClientWpfDemo / AdsSyncClientPlcDemo                         |
| **`UnitTest`**                  | Unit Test (work in progress) using XUnit                                                                                  |


## .NET Data Object Requirements

It is necessary for the properties to have the same names as the variables in the PLC's data structure.
The supported data types are listed below (“Currently Supported Data Types”)

    public partial class MyDataModel : ObservableObject
    {
        // ✅ Must implement INotifyPropertyChanged / ObservableObjekt
        // ✅ Must be a class (not struct)
        // ✅ Must have matching public properties AND private fields
        
        [ObservableProperty] 
        private double temperature;  // ← Private backing field required
        
        [ObservableProperty]
        private bool motorEnabled;
        
        // ❌ No indexer properties
        // ❌ No properties without private fields
        // ❌ Types must match exactly between property and field
    }


## Quick Start (.NET)

Create a data object using `INotifyPropertyChanged` or `ObservableObject`. 
You can use the same one for both reading and writing, or split it into two separate data objects

    // Your data models (must implement INotifyPropertyChanged)
    public partial class PlcDataModel : ObservableObject
    {
        [ObservableProperty] private double temperature;
        [ObservableProperty] private bool motorRunning;
        [ObservableProperty] private ObservableCollection<string> alarmMessages;

        public PlcDataModel()
        {
            for (int i1 = 0; i1 < AlarmMessages.Length; i1++)
            {
                AlarmMessages.Add(string.Empty);
            }
        }
    }

In the application:

    // Initialize the sync client
    PlcDataModel dataToSend = new();
    PlcDataModel dataFromPlc = new();

    AdsSyncClient adsClient = new(new AmsAddress("127.0.0.1.1.1", 851),
                                  dataToSend,
                                  dataFromPlc,
                                  "MainProgram.DataToPLC",      // Struct name in TwinCAT
                                  "MainProgram.DataFromPLC"     // Struct name in TwinCAT
    );

    // Activate synchronization
    bool connected = await adsClient.ActivateSync();

    if (connected)
    {
        Console.WriteLine("Synchronization active!");
    }

    // Monitor connection state
    adsClient.IsConnected = true/false;
    adsClient.IsActive = true/false;
    adsClient.StateHardware = AdsState.Run;
    adsClient.StateSoftware = AdsState.Run;

    // Enable Error Handling
    adsClient.ConnectionError += (sender, e) =>
    {
        Console.WriteLine($"Connection error: {e.Exception.Message}");
    };

    adsClient.CommunicationError += (sender, e) =>
    {
        Console.WriteLine($"Communication error on '{e.PropertyName}': {e.Exception.Message}");
    };

    // Cleanup
    await adsClient.StopSyncAsync();
    await adsClient.DisposeAsync();


## Quick Start (TwinCAT)

Create a `STRUCT` type:

    TYPE PlcDataModel :
    STRUCT
        Temperature 	: LREAL;
        MotorRunning	: BOOL;
        AlarmMessages	: ARRAY[0..9] OF STRING;
    END_STRUCT
    END_TYPE

In header of `Main.prg`:

    PROGRAM MAIN
    VAR
        DataToPlc	: PlcDataModel;
        DataFromPlc	: PlcDataModel;
    END_VAR


## Architecture Overview

    ┌─────────────────────────────────────────────────────────────┐
    │                      AdsSyncClient                          │
    │  - Public API for sync activation/deactivation              │
    │  - Connection state proxy (IsConnected, IsActive)           │
    │  - Event aggregation (ConnectionError, CommunicationError)  │
    └───────────────────────┬─────────────────────────────────────┘
                            │
                ┌───────────┴─────────────────────────┐
                ▼                                     ▼
    ┌─────────────────────────┐             ┌─────────────────────┐
    │ AdsConnectionManager    │             │ AdsDataSynchronizer │
    │ - Lifecycle management  │             │ - Variable mapping  │
    │ - Reconnection logic    │             │ - Handle generation │
    │ - State monitoring (2s) │             │ - Read/Write ops    │
    └─────────────────────────┘             └─────────────────────┘
                                                      │
                 ┌──────────────────┌─────────────────┼──┐
                 ▼                  ▼                    ▼
          ┌────────────┐    ┌───────────────┐    ┌────────────────┐
          │Marshalling │    │ AdsReadMapper │    │ AdsWriteMapper │
          └────────────┘    └───────────────┘    └────────────────┘


## Currently supported Data Types

The `Marshalling.PropertyClassToMarshalledFieldClass` method automatically converts your .NET properties to ADS-compatible field structures at runtime using `System.Reflection.Emit`.

| .NET Type              | ADS type      | Notes                             |
|------------------------|---------------|-----------------------------------|
| `bool`                 | `BOOL (U1)`  | 1 byte                            |
| `byte`                 | `BYTE (U1)` | 1 byte                            |
| `short`                | `INT (I2)`    | 2 bytes, signed                   |
| `int`                  | `DINT (I4)`   | 4 bytes, signed                   |
| `long`                 | `LINT (I8)`   | 8 bytes, signed                   |
| `ushort`               | `USINT (U2)`  | 2 bytes, unsigned                 |
| `uint`                 | `UDINT (U4)`  | 4 bytes, unsigned                 |
| `ulong`                | `ULINT (U8)`  | 8 bytes, unsigned                 |
| `float`                | `REAL (R4)`   | 4 bytes, IEEE 754                 |
| `double`               | `LREAL (R8)`  | 8 bytes, IEEE 754                 |
| `string`               | `STRING[80]`  | 80 chars + null terminator        |
| `ObservableCollection` | `ARRAY`       | Dynamic arrays with type tracking |


## License 

MIT License - Feel free to use in commercial and open-source projects.
