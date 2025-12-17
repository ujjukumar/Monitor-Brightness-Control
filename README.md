# Monitor Brightness Control

A lightweight Windows utility to control the brightness of external monitors using DDC/CI.

## Features
-   **Generic Logic**: Works with most external monitors supporting DDC/CI.
-   **Hotkeys**: Global shortcuts to adjust brightness from anywhere.
-   **Efficient**: Debounced hardware calls to prevent UI freezing.
-   **Tiny**: ~175 KB executable (requires .NET 10).

## Usage
1.  Run `BrightnessControl.exe`.
2.  Select your monitor from the dropdown.
3.  Use the slider to adjust brightness.

### Hotkeys
-   **Increase Brightness**: `Ctrl` + `Shift` + `Up Arrow`
-   **Decrease Brightness**: `Ctrl` + `Shift` + `Down Arrow`

## Requirements
-   **OS**: Windows 10/11 (x64)
-   **Runtime**: [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
-   **Hardware**: Monitor with DDC/CI enabled (usually on by default in monitor settings).

## Build Instructions

### 1. Framework Dependent (Recommended for Dev)
Produces a tiny ~175 KB file (requires .NET 10 installed).
```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false
```

### 2. Native AOT (Standalone)
Produces a ~18.5 MB file that runs without installing .NET.
*Edit `BrightnessControl.csproj` to enable `<PublishAot>true</PublishAot>` first.*
```powershell
dotnet publish -c Release -r win-x64
```
