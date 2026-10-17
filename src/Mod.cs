using System.Collections.Generic;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace OTH.ImpulseTriggers;

// Adds Xbox One/Series impulse-trigger rumble to Over the Hill.
//
// The game already turns vehicle physics into haptic events and sends grip
// rumble (low/high frequency) through Unity's dual-motor API, which has no
// trigger channel. This mod listens to the same events the game's
// ControllerVibrationManager receives, maps them onto the two trigger motors,
// and sends grips + triggers together through Steam Input.
//
// Right trigger (throttle): wheelspin, engine strain, gear changes, winch tension.
// Left trigger (brake): lateral slides, sudden deceleration.
// Both: impacts, bottoming out, rollover contact.
//
// Each trigger has its own gain (0 = off, 1 = default, up to 2), set from
// Settings > Controls or MelonPreferences.cfg.
public class Mod : MelonMod
{
    [HarmonyPatch(typeof(ControllerVibrationManager), nameof(ControllerVibrationManager.SubmitPhysical))]
    private static class SubmitPhysicalPatch
    {
        private static void Postfix(ControllerHapticEventId eventId, float severity)
        {
            if (!prefEnabled.Value)
            {
                return;
            }
            long t = Perf.Now;
            pulses[eventId] = (Mathf.Clamp01(severity), Time.unscaledTime + PulseLength(eventId));
            if (prefDebug.Value)
            {
                Log.Msg($"pulse {eventId} sev={severity:0.00}");
            }
            Perf.AddHook(t);
        }
    }

    [HarmonyPatch(typeof(ControllerVibrationManager), nameof(ControllerVibrationManager.SetRepeatingCondition))]
    private static class RepeatingConditionPatch
    {
        private static void Postfix(ControllerHapticEventId eventId, float severity)
        {
            if (!prefEnabled.Value)
            {
                return;
            }
            long t = Perf.Now;
            conditions[eventId] = (Mathf.Clamp01(severity), Time.unscaledTime);
            Perf.AddHook(t);
        }
    }

    internal static MelonLogger.Instance Log;

    private static MelonPreferences_Entry<bool> prefEnabled;

    private static MelonPreferences_Entry<float> prefLeftGain;

    private static MelonPreferences_Entry<float> prefRightGain;

    private static MelonPreferences_Entry<bool> prefDebug;

    private static MelonPreferences_Entry<bool> prefPerf;

    // A repeating condition counts as active this long after its last refresh.
    private const float ConditionTimeout = 0.25f;

    private const float UpdateInterval = 1f / 60f;

    // Resend unchanged non-zero levels this often, in case Steam drops a command.
    private const float KeepAlive = 0.25f;

    private const int InitAttempts = 30;

    // How long a slider change buzzes its trigger, and how hard at 1.0x.
    private const float PreviewLength = 0.4f;

    private const float PreviewLevel = 0.5f;

    private static float previewUntil;

    private static bool previewLeft;

    private const float InitRetry = 2f;

    private static readonly Dictionary<ControllerHapticEventId, (float severity, float time)> conditions = new();

    private static readonly Dictionary<ControllerHapticEventId, (float severity, float until)> pulses = new();

    private float nextUpdate;

    private float lastSend;

    private (float lf, float hf, float lt, float rt) last;

    private float nextInitTry;

    private int initTries;

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        MelonPreferences_Category cat = MelonPreferences.CreateCategory("ImpulseTriggers");
        prefEnabled = cat.CreateEntry("Enabled", true, description: "Turn impulse-trigger rumble on or off");
        prefLeftGain = cat.CreateEntry("LeftTriggerGain", 1f, description: "Left (brake) trigger rumble gain: 0 = off, 1 = default, up to 2 (also in Settings > Controls)");
        prefRightGain = cat.CreateEntry("RightTriggerGain", 1f, description: "Right (throttle) trigger rumble gain: 0 = off, 1 = default, up to 2 (also in Settings > Controls)");
        prefDebug = cat.CreateEntry("DebugLog", false, description: "Log every haptic pulse and trigger change");
        prefPerf = cat.CreateEntry("PerfLog", false, description: "Every 10 s, log game fps and the mod's own cost per frame");
        Perf.On = prefPerf.Value;
    }

    internal static float LeftGain
    {
        get => Mathf.Clamp(prefLeftGain.Value, 0f, SettingsMenu.MaxGain);
        set => prefLeftGain.Value = Mathf.Clamp(value, 0f, SettingsMenu.MaxGain);
    }

    internal static float RightGain
    {
        get => Mathf.Clamp(prefRightGain.Value, 0f, SettingsMenu.MaxGain);
        set => prefRightGain.Value = Mathf.Clamp(value, 0f, SettingsMenu.MaxGain);
    }

    // Buzz one trigger briefly at its current gain, so moving a slider is felt.
    internal static void Preview(bool left, float now)
    {
        previewLeft = left;
        previewUntil = now + PreviewLength;
    }

    public override void OnApplicationQuit()
    {
        SteamImpulse.Send(0f, 0f, 0f, 0f);
    }

    public override void OnUpdate()
    {
        float now = Time.unscaledTime;
        if (prefEnabled.Value)
        {
            long t = Perf.Now;
            Update(now);
            Perf.AddUpdate(t);
        }
        if (Perf.On)
        {
            Perf.Frame(now);
        }
    }

    private void Update(float now)
    {
        try
        {
            SettingsMenu.Tick(now);
        }
        catch (System.Exception ex)
        {
            Log.Warning("Settings menu: " + ex.Message);
        }
        EnsureSteam(now);
        SteamImpulse.Tick(now);
        if (now < nextUpdate || SteamImpulse.ControllerCount == 0)
        {
            return;
        }
        nextUpdate = now + UpdateInterval;

        ControllerVibrationManager vm = ControllerVibrationManager.Instance;
        float lf = 0f, hf = 0f, lt = 0f, rt = 0f;
        if (vm != null)
        {
            ControllerHapticOutput grips = vm.lastOutput;
            lf = grips.LF;
            hf = grips.HF;
            float master = Mathf.Clamp01(vm.MasterIntensity);
            if (prefEnabled.Value && now < previewUntil)
            {
                // Menus pause the game, so the preview skips the pause check.
                if (previewLeft)
                {
                    lt = PreviewLevel * LeftGain * master;
                }
                else
                {
                    rt = PreviewLevel * RightGain * master;
                }
            }
            else if (prefEnabled.Value && !vm.pauseMenuOpen && !vm.applicationPaused)
            {
                ComputeTriggers(now, out lt, out rt);
                lt *= LeftGain * master;
                rt *= RightGain * master;
            }
        }

        // With both triggers idle, leave grip rumble entirely to the game.
        bool active = lt > 0f || rt > 0f;
        bool wasActive = last.lt > 0f || last.rt > 0f;
        if (!active && !wasActive)
        {
            return;
        }
        var next = (Q(lf), Q(hf), Q(lt), Q(rt));
        if (next == last && now - lastSend < KeepAlive)
        {
            return;
        }
        SteamImpulse.Send(next.Item1, next.Item2, next.Item3, next.Item4);
        Perf.CountSend();
        if (prefDebug.Value && (next.Item3 != last.lt || next.Item4 != last.rt))
        {
            Log.Msg($"LT={next.Item3:0.00} RT={next.Item4:0.00} (grips {next.Item1:0.00}/{next.Item2:0.00})");
        }
        last = next;
        lastSend = now;
    }

    private void EnsureSteam(float now)
    {
        if (SteamImpulse.Ready || now < nextInitTry || initTries >= InitAttempts)
        {
            return;
        }
        nextInitTry = now + InitRetry;
        initTries++;
        if (SteamImpulse.Init())
        {
            Log.Msg($"Steam Input ready (attempt {initTries})");
        }
        else if (initTries == InitAttempts)
        {
            Log.Warning($"Steam Input could not start after {InitAttempts * InitRetry:0} s; trigger rumble disabled");
        }
    }

    private static void ComputeTriggers(float now, out float lt, out float rt)
    {
        lt = 0f;
        rt = 0f;

        // Throttle side.
        float spin = Condition(ControllerHapticEventId.LongitudinalWheelspin, now);
        if (spin > 0.15f)
        {
            rt = Mathf.Max(rt, 0.3f + 0.5f * spin);
        }
        float strain = Condition(ControllerHapticEventId.EngineStrain, now);
        if (strain > 0.55f)
        {
            rt = Mathf.Max(rt, 0.3f * strain);
        }
        float winch = Condition(ControllerHapticEventId.WinchTension, now);
        if (winch > 0.05f)
        {
            rt = Mathf.Max(rt, 0.2f + 0.4f * winch);
        }
        float gear = Pulse(ControllerHapticEventId.GearChange, now);
        if (gear > 0f)
        {
            rt = Mathf.Max(rt, 0.6f + 0.4f * gear);
        }

        // Brake side.
        float slide = Condition(ControllerHapticEventId.LateralSlide, now);
        if (slide > 0.15f)
        {
            lt = Mathf.Max(lt, 0.3f + 0.5f * slide);
        }
        float decel = Pulse(ControllerHapticEventId.SuddenDeceleration, now);
        if (decel > 0f)
        {
            lt = Mathf.Max(lt, 0.3f + 0.7f * decel);
        }

        // Both: hits through the chassis.
        float impact = Mathf.Max(
            Mathf.Max(Pulse(ControllerHapticEventId.Collision, now), Pulse(ControllerHapticEventId.SuspensionBottomOut, now)),
            Mathf.Max(Pulse(ControllerHapticEventId.RolloverBodyContact, now), Pulse(ControllerHapticEventId.StrongSuspensionCompression, now)));
        if (impact > 0f)
        {
            lt = Mathf.Max(lt, impact);
            rt = Mathf.Max(rt, impact);
        }
    }

    private static float Condition(ControllerHapticEventId id, float now)
    {
        return conditions.TryGetValue(id, out var c) && now - c.time < ConditionTimeout ? c.severity : 0f;
    }

    private static float Pulse(ControllerHapticEventId id, float now)
    {
        return pulses.TryGetValue(id, out var p) && now < p.until ? p.severity : 0f;
    }

    private static float PulseLength(ControllerHapticEventId id)
    {
        return id switch
        {
            ControllerHapticEventId.GearChange => 0.09f,
            ControllerHapticEventId.Collision => 0.18f,
            _ => 0.12f,
        };
    }

    // Quantise to 1/64 steps so tiny fluctuations don't spam Steam.
    private static float Q(float v)
    {
        return Mathf.Round(Mathf.Clamp01(v) * 64f) / 64f;
    }
}
