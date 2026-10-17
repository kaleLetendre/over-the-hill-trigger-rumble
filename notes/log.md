# Log

## 2026-10-17

- Reference: TriggerFX 0.2.1 (Nexus overthehill/mods/1), MelonLoader 0.7.3, DualSense.
  Hooks `ControllerVibrationManager.SubmitPhysical` / `SetRepeatingCondition` and calls the
  Steam Input flat API from the game's `steam_api64.dll`.
- Game: Unity 6000.0.75f1, IL2CPP metadata v31. Grip rumble goes through Unity Input System
  dual-motor (`UnityGamepadHapticOutput.Write(ControllerHapticOutput{LF,HF})`); Player.log says
  `Using Windows.Gaming.Input`.
- Tried BepInEx 6 be.789 first (generated interop fine), then switched to MelonLoader 0.7.3
  to match the game's existing mods.
- Launch option was left at the BepInEx `winhttp=n,b`, so MelonLoader didn't load. Fixed to `version=n,b`.
- With xbox-trigger-rumble `on` mode, the game had no controller until Steam Input was enabled
  per game (see that repo's log).
- v0.1.0 first build loaded as "0 Mods loaded": missing `[assembly: MelonInfo]` / `MelonGame`.
  MelonLoader skips such DLLs silently. Added `src/AssemblyInfo.cs`.
- With the attributes: `Steam Input ready (attempt 1)`, then `controllers: 1 (types: 3), Xbox
  One/Series: 1` about 5 s later. User: triggers work in game.
- MelonLoader rewrote `UserData/MelonPreferences.cfg` on quit of a session without the mod,
  dropping the hand-added `[ImpulseTriggers]` section. Entries only persist once the mod has run.
- v0.2.0: per-trigger gain sliders (0–2, default 1) cloned from the game's Controller
  Vibration row in Settings > Controls > Advanced, same technique as TriggerFX. Value text
  set via a postfix on `UISliderSelector.UpdateText`. `MinMax<float>` is a boxed IL2CPP struct,
  so the rows keep the original range and map position → gain. Replaced `TriggerStrength`
  with `LeftTriggerGain` / `RightTriggerGain`. Needed refs: UnityEngine.UI, Unity.TextMeshPro,
  Unity.Localization, Il2CppFunselektor.FunCore.Runtime.
- v0.2.0 bug: slider values didn't persist. `SettingsMenu.Tick` threw an IL2CPP NRE every frame
  in `FunTextTMP.set_TextLocalization` (setting null when localization was already cleared),
  before reaching the value-change code. 0.2.1: only clear it while set, wrapped in try/catch,
  and store the value before the cosmetic label refresh.
- v0.2.0 bug: slider values didn't persist. `SettingsMenu.Tick` threw an IL2CPP NRE every frame
  in `FunTextTMP.set_TextLocalization` (setting null when localization was already cleared),
  before reaching the value-change code. 0.2.1: only clear it while set (ReferenceEquals; the
  `!=` operator on LocalizedString pulls in UIElementsModule), wrapped in try/catch, and store
  the value before the cosmetic label refresh.
- Perf (0.2.2, PerfLog): steady driving 57–60 fps; mod 12–26 us/frame (~0.1% of a 16.7 ms
  frame); hooks ~250 calls/s at ~5 us/frame; Steam sends 0–29/s. Worst frames 17–44 ms occur
  with 0 Steam sends too, so they aren't from the mod's Steam calls. Multi-second worst
  frames only during loading.
- The v0.2.0 session logged 2790 NREs (two per frame while the settings rows existed, each
  with an IL2CPP stack trace), which costs real frame time. 0.2.1 and 0.2.2 sessions: 0.
