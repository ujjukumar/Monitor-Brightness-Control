# Monitor Brightness Control

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg?logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4.svg?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![GitHub Release](https://img.shields.io/github/v/release/ujjukumar/Monitor-Brightness-Control?logo=github)](https://github.com/ujjukumar/Monitor-Brightness-Control/releases)

A lightweight Windows utility to control the brightness of external monitors using DDC/CI.

---

## 📥 Download

Pre-built binaries are available on the [Releases](https://github.com/ujjukumar/Monitor-Brightness-Control/releases) page:

*   **Standalone (Recommended):** `BrightnessControl-vX.Y.Z-windows-x64-standalone.zip` (or `BrightnessControl-standalone.exe`)
    *   Runs out-of-the-box on any Windows 10/11 machine.
    *   No need to install the .NET runtime separately.
*   **Lightweight (Framework-Dependent):** `BrightnessControl-vX.Y.Z-windows-x64.zip` (or `BrightnessControl.exe`)
    *   Ultra-tiny (~250 KB).
    *   Requires [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) installed.

---

## Features
-   **Generic Logic**: Works with most external monitors supporting DDC/CI.
-   **Hotkeys**: Global shortcuts to adjust brightness from anywhere.
-   **Efficient**: Debounced hardware calls to prevent UI freezing.
-   **Tiny**: Lightweight single-file executable.

## Usage
1.  Run `BrightnessControl.exe`.
2.  Select your monitor from the dropdown.
3.  Use the slider to adjust brightness.

### Hotkeys
-   **Increase Brightness**: `Ctrl` + `Shift` + `Up Arrow`
-   **Decrease Brightness**: `Ctrl` + `Shift` + `Down Arrow`

## Requirements
-   **OS**: Windows 10/11 (x64)
-   **Hardware**: Monitor with DDC/CI enabled (usually on by default in monitor settings).
-   **Runtime**: [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (only if using the framework-dependent build).

## Build Instructions

### 1. Framework Dependent
Produces a tiny ~250 KB file (requires .NET 10 runtime installed):
```powershell
dotnet publish BrightnessControl/BrightnessControl.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false
```

### 2. Standalone (Self-Contained)
Produces a single executable that runs anywhere without installing .NET:
```powershell
dotnet publish BrightnessControl/BrightnessControl.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:EnableCompressionInSingleFile=true
```
