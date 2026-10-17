# Over the Hill: Impulse Triggers

A MelonLoader mod that adds Xbox One / Xbox Series impulse-trigger rumble to
*Over the Hill*. The game only drives the two grip motors. This mod also runs the
small motors behind the triggers, using the haptic events the game already produces:

| Trigger | Feels |
|---|---|
| Right (throttle) | wheelspin, engine strain, gear changes, winch tension |
| Left (brake) | sliding sideways, sudden deceleration |
| Both | collisions, bottoming out, rollover body contact |

It's modelled on [TriggerFX](https://www.nexusmods.com/overthehill/mods/1) (DualSense
adaptive triggers), which hooks the same game events.

**Status:** works on Linux (Proton, wired Xbox Series X|S controller, Steam Input on).

> **Untested on Windows.** It should work there through the same Steam Input call,
> probably with Steam's extended Xbox controller support driver turned on (see
> [Platforms](#platforms)), but nobody has tried it yet. Reports welcome.

## Requirements

- The Windows version of Over the Hill on Steam (works under Proton).
- [MelonLoader v0.7.3](https://github.com/LavaGang/MelonLoader/releases/tag/v0.7.3).
- An Xbox One / Series controller, with **Steam Input enabled** for the game
  (Properties → Controller → Enable Steam Input). The mod sends rumble through
  Steam Input. On Linux with the host setup below, the game also gets no
  controller input at all without it.
- **Linux:** trigger motors on a wired controller also need the host-side setup
  in [xbox-trigger-rumble](https://github.com/kaleLetendre/xbox-trigger-rumble), so Steam can reach them.
  Stock Linux drivers only drive the grip motors.

## Install (Linux / Proton)

    scripts/install-melonloader.sh

1. In Steam, set the game's launch options (Properties → General) to:

       WINEDLLOVERRIDES="version=n,b" %command%

2. Start the game once and wait for the main menu (the first start is slow while
   MelonLoader generates assemblies), then quit.
3. Build and install the mod:

       scripts/build.sh

## Settings

### In game

**Settings → Controls → Advanced**, directly below *Controller Vibration*:

| Slider | Range | Default |
|---|---|---|
| Left Trigger Rumble | Off – 2.0x | 1.0x |
| Right Trigger Rumble | Off – 2.0x | 1.0x |

Moving a slider gives a short buzz on that trigger at the new strength. The game's
own *Controller Vibration* slider scales both triggers too, so setting it to 0
silences everything.

### Config file

`UserData/MelonPreferences.cfg` in the game folder, section `[ImpulseTriggers]`.
The section appears after the first run with the mod.

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Turn trigger rumble on or off entirely |
| `LeftTriggerGain` | `1.0` | 0 = off, 1 = default, up to 2 (same as the in-game slider) |
| `RightTriggerGain` | `1.0` | 0 = off, 1 = default, up to 2 (same as the in-game slider) |
| `DebugLog` | `false` | Log every haptic event and trigger change to `MelonLoader/Latest.log` |

Gains above 1.0 make weaker effects stronger. Effects already at full strength stay at full.

## Uninstall

Delete `Mods/OTH.ImpulseTriggers.dll` from the game folder. To remove MelonLoader
too, run `scripts/uninstall-melonloader.sh` and clear the launch option.

## How it works

- Harmony postfixes on `ControllerVibrationManager.SubmitPhysical` (one-off
  pulses) and `SetRepeatingCondition` (ongoing conditions) record each haptic
  event and its severity.
- At 60 Hz the mod maps active events to left/right trigger levels and reads the
  game's current grip levels (`ControllerVibrationManager.lastOutput`). It sends
  all four through `ISteamInput::TriggerVibrationExtended` in the game's `steam_api64.dll`.
- While both triggers are idle it sends nothing, so grip rumble stays exactly as
  the game made it.

## Platforms

The mod is a Windows .NET assembly either way (the game is a Windows build), so the
same DLL is meant to run on Windows and under Proton. What differs is how rumble
reaches the controller.

| | Linux (Proton) | Windows |
|---|---|---|
| Mod loader | MelonLoader + `WINEDLLOVERRIDES="version=n,b"` launch option | MelonLoader, no launch option needed |
| Trigger path today | Steam Input `TriggerVibrationExtended` → Steam's SDL over libusb | Same call; Steam forwards it to the controller |
| Host setup | [xbox-trigger-rumble](https://github.com/kaleLetendre/xbox-trigger-rumble) `on` mode + Steam Input enabled | Steam Input enabled for the game, plus most likely Steam's extended Xbox controller support driver (Steam → Settings → Controller; needs a driver install and a reboot). Valve added it in 2021 for trigger rumble. *Untested.* |
| Without Steam Input | No controller at all in `on` mode | Planned fallback: Windows.Gaming.Input `Gamepad.Vibration` (has `LeftTrigger`/`RightTrigger`), which Windows supports natively for Xbox controllers |

Planned structure, so the second backend is a drop-in:

- `TriggerMixer` (pure logic): haptic events → trigger levels. No platform code.
- An `IRumbleBackend` with `SteamInputBackend` (current `SteamImpulse`) and
  `WgiBackend` (Windows only, via WinRT activation of `Windows.Gaming.Input.Gamepad`).
- At startup, pick Steam Input if it reports an Xbox One/Series controller, otherwise
  WGI if available (it is on real Windows; unknown under Proton). Log which one is used.

Nothing here assumes Linux paths. `steam_api64.dll` is loaded from the game's
`Plugins/x86_64` folder on both.

## Development

- `scripts/build.sh [--no-copy]` builds with the .NET SDK (8+; `~/.dotnet` is
  picked up automatically) against the game's MelonLoader assemblies.
- `build/reference/` (not committed) holds decompiled TriggerFX and game types,
  for reading only.
