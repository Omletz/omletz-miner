# Building OmletzMiner GUI

## Prerequisites
- .NET 8 SDK (https://aka.ms/dotnet/download)
  OR Visual Studio 2022 with the ".NET desktop development" workload

---

## Quick build (command line)

```
cd "D:\Miner Project\OmletzMiner"
dotnet build -c Release
```

## Publish — single self-contained .exe (recommended for distribution)

```
cd "D:\Miner Project\OmletzMiner"
dotnet publish OmletzMiner/OmletzMiner.csproj /p:PublishProfile=Release-win-x64
```

Output lands in `OmletzMiner/publish/OmletzMiner.exe` (~12 MB, no runtime needed).

---

## Adding branding assets

Before building, drop these into `OmletzMiner/Resources/`:

| File         | Use                                                  |
|--------------|------------------------------------------------------|
| `logo.png`   | Header logo — any size, displayed at 64×64           |
| `omletz.ico` | .exe taskbar/window icon (multi-resolution ICO file) |

Without them the app still builds: the fallback draws "H\O" text as the logo,
and no icon is shown in the taskbar. Remove `<ApplicationIcon>` from the
.csproj if you don't have the .ico and don't want the warning.

---

## Distribution layout

Copy these two files into the same folder and ship that folder:

```
OmletzMiner/
  OmletzMiner.exe    ← GUI (self-contained, built above)
  ccminer.exe        ← engine (built from ccminer VS solution, Release|x64)
```

The GUI searches for `ccminer.exe` in:
1. The same folder as `OmletzMiner.exe`  ← production layout
2. `../ccminer/x64/Release/ccminer.exe`  ← dev layout (VS output)
3. `../ccminer/Release/ccminer.exe`      ← dev layout (VS output, x86)

---

## Running during development (no build)

You can also open `OmletzMiner.sln` in Visual Studio 2022 and press F5.
Set the Debug working directory to a folder containing `ccminer.exe` first.
