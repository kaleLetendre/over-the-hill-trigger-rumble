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

## What you need

- [ ] *Over the Hill* on Steam
- [ ] An **Xbox One or Xbox Series controller**. Other controllers don't have
      trigger motors. For a DualSense, see [TriggerFX](https://www.nexusmods.com/overthehill/mods/1).
- [ ] **Linux only:** the controller on a USB cable, with
      [xbox-trigger-rumble](https://github.com/kaleLetendre/xbox-trigger-rumble)
      set up first. Linux's stock driver can't run the trigger motors at all.

Then follow the guide for your system:
[Linux](#setup-on-linux-steam--proton) or [Windows](#setup-on-windows-untested).

---

## Setup on Linux (Steam + Proton)

About 15 minutes, most of it waiting for the game's first start.

### Step 1: Set up the controller

Follow the step-by-step guide in
[xbox-trigger-rumble](https://github.com/kaleLetendre/xbox-trigger-rumble#step-by-step-setup)
and come back when `./trigger-rumble status` looks right. Without it, this mod
runs but the triggers stay silent.

### Step 2: Turn on Steam Input for the game

1. In your Steam library, right-click **Over the Hill** → **Properties**.
2. Click **Controller**.
3. Set the override to **Enable Steam Input**.

Skip this and the game won't react to your controller at all.

### Step 3: Open the game folder

In Steam, right-click **Over the Hill** → **Manage** → **Browse local files**.
A file manager opens on a folder containing `over the hill.exe`. Keep it open;
the next steps all happen in this folder.

### Step 4: Install MelonLoader (the mod loader)

1. Download **MelonLoader v0.7.3**:
   [MelonLoader.x64.zip](https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip).
   It has to be this version; newer or older ones may not work with this mod.
2. Open the ZIP and extract **everything** into the game folder from Step 3.

Check: the game folder now has a `version.dll` file and a `MelonLoader` folder,
right next to `over the hill.exe`:

    MelonLoader/
    over the hill_Data/
    over the hill.exe
    UnityPlayer.dll
    version.dll
    ...

If you see a `MelonLoader.x64` folder instead, the files were extracted one level
too deep. Move its contents up into the game folder.

### Step 5: Tell Proton to load MelonLoader

1. In Steam, right-click **Over the Hill** → **Properties** → **General**.
2. In **Launch Options**, paste exactly:

       WINEDLLOVERRIDES="version=n,b" %command%

3. Close the window. Steam saves it automatically.

### Step 6: Start the game once, without the mod

1. Start Over the Hill from Steam.
2. A MelonLoader console window with scrolling text appears before the game.
   **The first start is slow, often several minutes,** while MelonLoader prepares
   itself. It can look frozen; it isn't. Wait.
3. When the game's main menu appears, quit the game.

Check: the game folder now has a `Mods` folder. If it doesn't, create one with
exactly that name.

### Step 7: Install the mod

1. Download [OTH.ImpulseTriggers.dll](https://github.com/kaleLetendre/over-the-hill-trigger-rumble/releases/latest/download/OTH.ImpulseTriggers.dll).
2. Put it in the `Mods` folder inside the game folder.

### Step 8: Start the game and check it loaded

Start the game again. In the MelonLoader console window, look for:

    1 Mods loaded.
    ...
    ImpulseTriggers v0.2.2
    ...
    [ImpulseTriggers] Steam Input ready (attempt 1)
    [ImpulseTriggers] Steam Input controllers: 1 (types: 3 ), Xbox One/Series: 1

`Xbox One/Series: 1` is the important part: the mod has found your controller.
It can take a few seconds after the menu appears. The same text is saved in
`MelonLoader/Latest.log` in the game folder.

### Step 9: Feel it

1. In the game, open **Settings → Controls**, then the **Advanced** options.
2. Below *Controller Vibration* are two new sliders, **Left Trigger Rumble** and
   **Right Trigger Rumble**.
3. Move one. That trigger gives a short buzz at the new strength.

Then go for a drive. Spin the wheels for the right trigger, slide sideways for the
left, and hit something to feel both.

Done. You only do all this once. Next time, just start the game.

---

## Setup on Windows (untested)

> Nobody has tried this on Windows yet. The steps are what *should* work. If you try
> it, please [open an issue](https://github.com/kaleLetendre/over-the-hill-trigger-rumble/issues)
> and say whether the triggers worked, and whether you needed Step 2.

### Step 1: Turn on Steam Input for the game

In your Steam library, right-click **Over the Hill** → **Properties** → **Controller**,
and set the override to **Enable Steam Input**.

### Step 2: Turn on Steam's extended Xbox controller support (probably needed)

In Steam, go to **Steam → Settings → Controller** and turn on the extended Xbox
controller support option, if it's offered. It installs a driver and asks you to
restart your PC. Valve added it to give Steam access to Xbox trigger motors on
Windows. Without it, Windows may only run the grip motors.

### Step 3: Install MelonLoader v0.7.3

1. Download the latest
   [MelonLoader Installer](https://github.com/LavaGang/MelonLoader.Installer/releases/latest)
   and run it.
2. Pick **Over the Hill** from the list. If it isn't listed, add it by browsing to
   `over the hill.exe`. To find that file: in Steam, right-click the game →
   **Manage** → **Browse local files**.
3. Choose version **v0.7.3**, then click **Install**.

No launch option is needed on Windows.

### Step 4: Start the game once, then install the mod

1. Start the game, wait for the main menu (the first start is slow), then quit.
2. Download [OTH.ImpulseTriggers.dll](https://github.com/kaleLetendre/over-the-hill-trigger-rumble/releases/latest/download/OTH.ImpulseTriggers.dll) and put it in the `Mods` folder in
   the game folder.
3. If Windows marks the file as downloaded from the internet: right-click it →
   **Properties** → tick **Unblock** → **OK**.

Then continue with [Step 8 of the Linux guide](#step-8-start-the-game-and-check-it-loaded):
the checks are the same.

---

## Troubleshooting

Most answers are in `MelonLoader/Latest.log` in the game folder. Search it for `ImpulseTriggers`.

**No MelonLoader console window, and no `MelonLoader/Latest.log`**
MelonLoader isn't loading. On Linux, check the launch option from
[Step 5](#step-5-tell-proton-to-load-melonloader) is exactly
`WINEDLLOVERRIDES="version=n,b" %command%`, and that `version.dll` is next to
`over the hill.exe`.

**The log says `0 Mods loaded`**
The DLL isn't in the `Mods` folder, or it's inside a subfolder. It must be
`Mods/OTH.ImpulseTriggers.dll`. On Windows, also check it's unblocked
(Step 4.3 above).

**The game doesn't react to the controller at all**
Steam Input is off for the game ([Step 2](#step-2-turn-on-steam-input-for-the-game)).
On Linux, also check `./trigger-rumble status` in xbox-trigger-rumble. Most often,
Steam needs restarting with the session setting.

**The log says `Xbox One/Series: 0`**
Steam Input doesn't see an Xbox controller for this game. Turn on Steam Input for
the game, check the controller is an Xbox One/Series one, and restart the game.
`types:` lists what Steam sees: 3 is Xbox One/Series, 2 is Xbox 360 (no trigger
motors), 5/13 are PlayStation.

**The log says `Steam Input could not start after 60 s`**
The game wasn't started through Steam. Start it from your Steam library.

**Grips rumble, but the triggers stay silent**
- On Linux, xbox-trigger-rumble isn't in `on` mode, or Steam was started without
  its session setting. Run `./trigger-rumble status`.
- The trigger sliders are at **Off**, or the game's *Controller Vibration* slider
  is at 0. That one scales the triggers too.
- On Windows, try [Step 2](#step-2-turn-on-steams-extended-xbox-controller-support-probably-needed).

**The two sliders don't show up in Settings → Controls**
They sit in the **Advanced** controller options, directly below *Controller
Vibration*. If they're missing there, check the log for
`Added trigger rumble sliders` or `Could not add trigger sliders`.

**Still stuck?** [Open an issue](https://github.com/kaleLetendre/over-the-hill-trigger-rumble/issues)
and attach `MelonLoader/Latest.log`. For more detail, set `DebugLog = true` in
the config file (below) first.

---

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
The section appears after the first run with the mod. **Edit it only while the
game is closed.** MelonLoader rewrites the file when the game quits.

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Turn trigger rumble on or off entirely |
| `LeftTriggerGain` | `1.0` | 0 = off, 1 = default, up to 2 (same as the in-game slider) |
| `RightTriggerGain` | `1.0` | 0 = off, 1 = default, up to 2 (same as the in-game slider) |
| `DebugLog` | `false` | Log every haptic event and trigger change to `MelonLoader/Latest.log` |
| `PerfLog` | `false` | Every 10 s, log the game's fps, worst frame, and the mod's own cost per frame |

Gains above 1.0 make weaker effects stronger. Effects already at full strength stay at full.

---

## Updating

Download the new `OTH.ImpulseTriggers.dll` from
[Releases](https://github.com/kaleLetendre/over-the-hill-trigger-rumble/releases)
and replace the old one in `Mods`. Your settings are kept.

## Uninstall

- **Just this mod:** delete `Mods/OTH.ImpulseTriggers.dll` from the game folder.
- **MelonLoader too:** delete `version.dll` and the `MelonLoader` folder from the
  game folder. On Linux, also clear the launch option from Step 5. `Mods` and
  `UserData` hold mods and settings; delete them if you want.

---

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

## Building from source

Only needed to change the mod. Players should use the DLL from Releases.

- Install MelonLoader and run the game once (Linux Steps 3–6), so
  `MelonLoader/Il2CppAssemblies` exists to build against.
- `scripts/install-melonloader.sh` does Linux Step 4 for you; `scripts/uninstall-melonloader.sh` reverses it.
- `scripts/build.sh [--no-copy]` builds with the .NET SDK (8+; `~/.dotnet` is
  picked up automatically) against the game's MelonLoader assemblies.
- `build/reference/` (not committed) holds decompiled TriggerFX and game types,
  for reading only.
