# VIVIDIA

![VIVIDIA](./assets/logo.jpg)

## [**Download latest**](https://github.com/zeddo-dev/VIVIDIA/releases/latest)

## How it works?

- Changes brightness, contrast and gamma through the GPU LUT ([SetDeviceGammaRamp](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-setdevicegammaramp))
- Changes digital vibrance through NVAPI (NVIDIA) and ADL (AMD)
- Watches the foreground window, so colors switch the moment you alt-tab
- Your normal colors are saved before the first change and restored when the game closes, so a
  crash cannot leave the screen stuck with a game's gamma

## Supported graphic cards

- Nvidia GPU **fully supported.** (Brightness/Contrast/Gamma/Vibrance)
- AMD GPU **supported.** (Brightness/Contrast/Gamma/Vibrance)
- Intel **partially supported.** (Except Vibrance)

## What does it do?

You can set any of the following per application:

1. Brightness
2. Contrast
3. Gamma
4. Digital vibrance (aka. Saturation)
5. All displays or only the ones you pick
6. Only affects the display while that application is focused (it also prevents **sudden flash
   during Alt-tabbing**)

Light and dark themes, start with Windows and start minimized to tray are in Settings.

![screenshot](./assets/UI-screenshot.jpg)


Two builds, both a single file, no installer:

- **VIVIDIA-lite.exe** (~1 MB): needs the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime)
- **VIVIDIA.exe** (~63 MB): everything inside, just run it

Per-application color settings for Windows. Set brightness, contrast, gamma and digital vibrance
for a game, and they apply only while that game is in focus.

## How to Use

1. Open the application (SmartScreen might warn you as it's not signed)
2. **Add profile**: type the process name or pick a running application
3. Set the color values. **Preview** shows them live, **Reset to neutral** puts them back
4. Minimize to tray and play
5. Profiles are saved automatically to `%APPDATA%\VIVIDIA`

## Warning

1. It does not work while **HDR is on**, because Windows ignores the LUT there
2. A game in **exclusive fullscreen** may overwrite the LUT with its own; borderless is safer
3. Windows caps the brightness/contrast/gamma range. **Unlock range…** in the app lifts the cap
   (administrator rights, then sign out and back in; **some machines need a full reboot** before
   it takes effect). Moderate values work without it
4. Vibrance needs an NVIDIA or AMD desktop driver; on Intel that slider does nothing
5. Applications are matched by **process name**
6. In a **Remote Desktop session** the monitor is virtual and ignores the LUT, so brightness,
   contrast and gamma do nothing there, and the status line says so

## Technical details

How the LUT and vibrance are applied, monitor detection through EDID, the Windows gamma limit,
building and diagnostics are covered in [TECHNICAL.md](TECHNICAL.md).

## Licence

MIT, see [LICENSE](LICENSE).
