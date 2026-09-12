# VIVIDIA technical details

How the colour settings are applied, which limits Windows imposes, how to build VIVIDIA and how
the source is laid out. For what the application does and how to use it, see the
[README](README.md).

## How it works

| Parameter | Applied through | Limits |
|---|---|---|
| Brightness, contrast, gamma | GPU LUT, `SetDeviceGammaRamp` (GDI) | works on any GPU; ignored while HDR is on; a game in exclusive fullscreen may overwrite the LUT with its own |
| Digital vibrance (saturation) | NVAPI on NVIDIA, ADL (`ADL2_Display_Color_Set`) on AMD | Intel is partially supported: everything except vibrance, so that slider does nothing there |

The foreground window is tracked with an `EVENT_SYSTEM_FOREGROUND` hook, so alt-tab is picked
up instantly; a one-second timer covers events the hook can miss, such as a game closing
without handing focus over. Applications are matched by process name.

Whichever application holds focus decides: switch to another application that has a profile and
its profile applies; switch to one without a profile and the baseline settings come back.

The baseline is captured from **every** monitor the first time a profile is applied and stored
in `state.json`, so a crash or a killed process cannot leave the screen stuck with a game's
gamma. The next launch restores it.

## Scales

The same scales as the NVIDIA control panel: brightness and contrast 0-100 (50 is neutral),
gamma 0.30-2.80 (1.00 is neutral), digital vibrance 0-100 (50 is neutral). Neutral values
produce exactly the identity ramp. Vibrance 50 always maps to the driver's own default, so
neutral does not depend on where that default sits inside the driver's range.

Contrast and brightness are applied to the linear signal and gamma last. With gamma applied
first, raising contrast at a high gamma also lifted the midtones and flattened the top of the
curve, which looked like the effect snapping back.

The LUT formula approximates the NVIDIA panel; the curves may differ slightly, so values are
not guaranteed to match it exactly. When Windows refuses a ramp, the status line says so
(see the gamma ramp limit below).

## Monitor names

The driver string is "Generic PnP Monitor" for every display, so the model is read from EDID:
`EnumDisplayDevices` with `EDD_GET_DEVICE_INTERFACE_NAME` gives an interface path, which locates
the EDID block in the registry, and descriptor `0xFC` inside it holds the model name. If EDID is
unavailable, the vendor is derived from the PnP code instead. Profiles store the monitor's
stable `DeviceID`, not the `DISPLAYn` number, so re-plugging a monitor does not break them.

## Building

Requires the .NET 8 SDK.

```powershell
dotnet build src\Vividia\Vividia.csproj
```

Two release builds, both a single file:

```powershell
dotnet publish src\Vividia\Vividia.csproj -c Release -r win-x64 `
    --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:UseSystemResourceKeys=true `
    -p:SatelliteResourceLanguages=en -p:EventSourceSupport=false -p:MetadataUpdaterSupport=false `
    -o dist\standalone

dotnet publish src\Vividia\Vividia.csproj -c Release -r win-x64 `
    --self-contained false -p:PublishSingleFile=true -p:DebugType=None `
    -p:UseSystemResourceKeys=true -p:SatelliteResourceLanguages=en -o dist\lite
```

`dist\standalone\VIVIDIA.exe` is about 63 MB and carries the .NET runtime, the NVAPI wrapper and
the logo inside it, so nothing else has to be installed. `dist\lite\VIVIDIA.exe` is about 0.9 MB
and needs the .NET 8 Desktop Runtime on the machine; rename it to `VIVIDIA-lite.exe` for the
release page.

Trimming is not an option here: the SDK refuses it for Windows Forms (`NETSDK1175`).
`InvariantGlobalization` changes nothing, since ICU ends up in the single-file bundle either way.

## Diagnostics

`tests/SelfTest` is a console program that prints the detected monitors, checks the EDID parser
and the LUT maths, reads/writes/restores the LUT, reports the saturation range each driver
offers, and exercises the profile list, themes, dialog sizing, the preview toggle, mouse wheel
handling, numeric input, footer height and the sidebar with many profiles. Pass `--no-watch` to
skip the five-second foreground-window block.

```powershell
dotnet run --project tests\SelfTest -- --no-watch
```

## Assets

`assets/mark.png` is the square mark with a transparent background; it is embedded into the
executable and drawn in the window and the tray. `assets/vividia.ico` is the executable icon,
`assets/logo.png` is the full banner. All three are ready to use, and no generation step is part
of the build.

## Windows gamma ramp limit

Until the registry value below is set, every ramp entry must stay within half the scale of the
linear ramp, and Windows silently rejects anything beyond that. Brightness 93 / contrast 100 /
gamma 2.80 deviates by 0.498 and still applies, brightness 94 deviates by 0.503 and does not,
so the screen keeps the previous curve and the slider looks like it snapped back.

The sliders therefore stay free across their whole range and VIVIDIA applies as much as Windows
allows: a curve past the limit is scaled down to it proportionally, keeping its shape and
monotonicity, and the status line says "Capped by the Windows gamma limit". Nothing is rejected
and no slider snaps back. If a write is refused anyway, the status says that instead. The
"Unlock range…" button writes the registry value for you (administrator rights, then sign out
and back in; some machines only pick it up after a full reboot):

```
HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ICM\GdiIcmGammaRange = 256
```

Moderate values work without it.

Not every display accepts a LUT at all. A Remote Desktop session hands the application a
virtual monitor whose LUT can be neither read nor written, so every write fails regardless of
the values; the same happens on a display running in HDR. Brightness, contrast and gamma all
travel through that one ramp, so all three stop working together, and the status line says
"This display ignores brightness, contrast and gamma". Digital vibrance goes through NVAPI or
ADL instead and may still apply. That is a different situation from the range cap above.

## Layout of the source

- `src/Vividia/Display/`: monitor enumeration, EDID, LUT, NVAPI/ADL, applying and removing settings
- `src/Vividia/Watch/`: foreground tracking, process paths
- `src/Vividia/Core/ProfileEngine.cs`: profile switching logic
- `src/Vividia/Storage/`: config, baseline snapshot, autostart
- `src/Vividia/Ui/`: window, tray and the custom controls (slider, checkbox, scrollbar, tiles)
- `tests/SelfTest/`: diagnostics

User data lives in `%APPDATA%\VIVIDIA`: `config.json`, `state.json` and cached profile icons.

## Dependencies

- [NvAPIWrapper.Net](https://github.com/falahati/NvAPIWrapper): a managed wrapper around NVAPI,
  used for NVIDIA digital vibrance. Licensed under **LGPL-3.0**; it is pulled from NuGet and, in
  the single-file build, bundled into the executable. If that matters for how you ship VIVIDIA,
  publish without `PublishSingleFile` so the wrapper stays a replaceable DLL.
- AMD ADL (`atiadlxx.dll`): loaded at runtime from the installed AMD driver. No AMD SDK code is
  included in this repository; only the public constants and signatures are declared in
  `src/Vividia/Display/AdlApi.cs`.
- Everything else is Windows API (GDI, user32, dwmapi, uxtheme) through P/Invoke.

## Licence

MIT, see [LICENSE](LICENSE).
