# Firewatch Fly

A small single-player mod for **Firewatch** that lets you fly through the Wyoming wilderness. Press a key, take off, and press it again to fall back to the ground with real gravity and a soft landing.

> 🇩🇪 Deutsche Anleitung: [README.de](README.de)

> **Unofficial fan mod.** Not affiliated with or endorsed by Campo Santo, Panic or Valve. Use it at your own risk (see [Disclaimer](#disclaimer)).

---

## Features

- **Fly mode on a key press** – toggle flying with `K`.
- **Free movement** – fly forward, backward, sideways, up and down, with a sprint speed.
- **Realistic fall** – when you stop flying you fall straight down with acceleration, and the game takes over again once you touch the ground.
- **Soft landing** – the camera dips slightly when you land, stronger after a longer fall.
- **Body stays visible** – Henry's body turns toward where you look and leans slightly in the direction you fly, with a subtle idle sway.
- **No menu, no clutter** – no UI, no config files, no extra windows.
- **Safe cleanup** – the mod re-enables everything it pauses when you land or when the plugin is removed.
- **No clip** – You can fly through anything.
---

## Requirements

| Requirement | Details |
|---|---|
| Game | Firewatch (Steam, Windows) |
| Mod loader | **BepInEx 5.x, x64** (tested with 5.4.23.5) |
| Platform | **Windows only** (the mod reads keys through the Windows API) |

Tested with Firewatch running on Unity 2017.4.40 and together with the *Firewatch Enhanced* / High FPS Fix plugin.

> ⚠️ Do **not** use BepInEx 6 or the IL2CPP builds. Firewatch uses Mono, so you need **BepInEx 5**.

---

## Installation (step by step)

### 1. Find your Firewatch folder

In Steam: right-click **Firewatch** in your library → **Manage** → **Browse local files**.
This is the folder that contains `Firewatch.exe`. On most PCs it looks like this:

```
C:\Program Files (x86)\Steam\steamapps\common\Firewatch
```

### 2. Install BepInEx 5

1. Open the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases).
2. Download the **x64** build of BepInEx **5.4.x** for Windows (the file name looks like `BepInEx_win_x64_5.4.23.x.zip`or `BepInEx_x64_5.4.23.6.zip`).
3. Extract **all contents of the zip directly into the Firewatch folder**, next to `Firewatch.exe`.

After this, your Firewatch folder should contain these items:

```
Firewatch
├── BepInEx
├── Firewatch_Data
├── .doorstop_version
├── doorstop_config.ini
├── Firewatch.exe
├── UnityPlayer.dll
└── winhttp.dll
```

Make sure `winhttp.dll` and `doorstop_config.ini` sit **next to `Firewatch.exe`**, not inside a subfolder.

### 3. Start the game once

Launch Firewatch from Steam, wait for the main menu, then quit.
This creates the folders `BepInEx\plugins`, `BepInEx\config` and others.

### 4. Install the mod

1. Download `FirewatchFly.dll` from the release page.
2. *(Recommended)* Right-click the file → **Properties** → tick **Unblock** if it appears at the bottom → **OK**. Windows sometimes blocks downloaded DLL files.
3. Copy `FirewatchFly.dll` into:

```
Firewatch\BepInEx\plugins
```

### 5. Play

Start Firewatch, load a save game and press **K**. 🎉

---

## Controls

| Key | Action |
|---|---|
| `K`| Turn flying **on / off** |
| `W` `A` `S` `D` | Fly forward / left / backward / right (in the direction you look) |
| `Space` | Fly **up** |
| `Left Ctrl` | Fly **down** |
| `Left Shift` | Fly **faster** (3×) |

When you press the toggle key again while flying, you **fall** with gravity and land softly. If you press it again during the fall, you take off again immediately.

> 💡 `K` is the default toggle key that works everywhere.

---

## Configuration

There is no config file. If you want to change values (speed, gravity, landing strength, …), edit the constants at the top of `FlyMod.cs` and rebuild the mod (see [Building from source](#building-from-source)).

| Constant | Default | Meaning |
|---|---|---|
| `BaseSpeed` | `6` | Flying speed in meters per second |
| `FastMultiplier` | `3` | Speed multiplier while holding Shift |
| `Gravity` | `18` | How strongly you accelerate downwards while falling |
| `MaxFallSpeed` | `35` | Maximum fall speed |
| `TurnSpeed` | `540` | How fast the body turns toward your view (degrees per second) |
| `MaxLeanForward` / `MaxLeanSide` | `10` / `7` | Maximum body lean in degrees |
| `BobAmount` | `0.015` | Strength of the subtle idle sway |
| `DipDuration` | `0.45` | Duration of the landing camera dip (seconds) |
| `HideBodyWhileFlying` | `false` | `true` hides Henry's body while flying |
| `EnableAnimationControl` | `false` | **Experimental, keep off.** Can interfere with game animations (for example the walkie-talkie) |

---

## Troubleshooting

**Nothing happens when I press K.**
- Open `Firewatch\BepInEx\LogOutput.log` and search for `Firewatch Fly`. If the line is missing, the mod was not loaded: check that `FirewatchFly.dll` is directly in `BepInEx\plugins`.
- If the log file does not exist or is empty, BepInEx is not running. Check that `winhttp.dll` and `doorstop_config.ini` are next to `Firewatch.exe`.
- Right-click `FirewatchFly.dll` → **Properties** → **Unblock**.
- Try `F4` or `F6` instead, and hold **Fn** if your keyboard needs it.

**I get a BepInEx or version error.**
Make sure you installed **BepInEx 5.x x64** and not version 6 or an IL2CPP build.

**The walkie-talkie or another game feature is missing after flying.**
Keep `EnableAnimationControl` set to `false` (default). Restart the game and load a save from before the problem. If it keeps happening, remove `FirewatchFly.dll` and check whether the feature returns, then please report it with your `LogOutput.log`.

**I get stuck in the world or fall under the map.**
Load your last save. Avoid flying through the map boundaries.

**Build error "file is in use" (when building from source).**
Close Firewatch completely, also in the Windows Task Manager, then build again.

---

## Known issues and tips

- **Single-player only.** Firewatch has no multiplayer, but the mod is only meant for normal single-player play.
- **Don't save while flying or falling.** Wait until you have landed.
- **Story sequences:** Flying can skip areas or trigger scenes out of order. If something looks broken, load an earlier save.
- **Back up your saves** before trying any mod.
- The body animation is intentionally simple while flying: the mod does not drive Henry's walk animations, to avoid breaking game features.

---

## Uninstall

Delete `FirewatchFly.dll` from `Firewatch\BepInEx\plugins`.
That is all. The mod does not write any game files. To remove BepInEx completely, delete the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini` and `.doorstop_version` from the Firewatch folder (this also disables other BepInEx plugins you may have installed).

---

## Building from source

You only need this if you want to change the mod.

1. Install the [.NET SDK](https://dotnet.microsoft.com/download).
2. Install BepInEx 5 and start the game once (see above), so the DLLs this mod references exist.
3. Put `FlyMod.cs` and `FirewatchFly.csproj` into one folder.
4. Open `FirewatchFly.csproj` and set `GameDir` to your Firewatch folder if it is not at the default Steam path.
5. Close Firewatch, open a terminal in the folder and run:

```
dotnet build
```

The finished `FirewatchFly.dll` is copied to `BepInEx\plugins` automatically and is also in `bin\Debug\net35`.

The project references `BepInEx.dll` and the `UnityEngine` DLLs from your own game installation. They are not included in this repository.

---

## Changelog

### 1.10.1 – first public release
- Fly mode with toggle on `K`
- Gravity fall with soft landing
- Body turns toward your view, with slight lean and sway
- Fixed a case where paused game scripts were not re-enabled after pressing the key again during a fall
- Animation control disabled by default

---

## Disclaimer

This is a fan-made, unofficial mod provided **as is**, without any warranty. You use it at your own risk. Firewatch is a trademark of Campo Santo. This project is not affiliated with Campo Santo, Panic or Valve. No game assets are included.

## Credits

- [BepInEx](https://github.com/BepInEx/BepInEx) – the plugin framework this mod runs on
- Thanks to everyone who tested this mod

## License

MIT License – see [LICENSE](LICENSE).
MIT License

Copyright (c) 2026 Doan534

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
