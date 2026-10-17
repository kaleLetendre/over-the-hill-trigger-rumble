using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OTH.ImpulseTriggers;

// Sends four-motor rumble (grips + impulse triggers) to Xbox One/Series pads
// through Steam Input's flat C API in the game's own steam_api64.dll.
internal static class SteamImpulse
{
    private const string Lib = "steam_api64";

    private const int XBoxOneController = 3; // ESteamInputType.k_ESteamInputType_XBoxOneController

    private const float ScanInterval = 2f;

    private static IntPtr steamInput;

    private static readonly ulong[] handles = new ulong[16];

    private static ulong[] xboxHandles = Array.Empty<ulong>();

    private static float nextScan;

    private static int lastTotal = -1;

    private static bool resolverSet;

    public static bool Ready => steamInput != IntPtr.Zero;

    public static int ControllerCount => xboxHandles.Length;

    [DllImport(Lib)]
    private static extern IntPtr SteamAPI_SteamInput_v006();

    [DllImport(Lib)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SteamAPI_ISteamInput_Init(IntPtr self, [MarshalAs(UnmanagedType.I1)] bool explicitlyCallRunFrame);

    [DllImport(Lib)]
    private static extern void SteamAPI_ISteamInput_RunFrame(IntPtr self, [MarshalAs(UnmanagedType.I1)] bool reserved);

    [DllImport(Lib)]
    private static extern int SteamAPI_ISteamInput_GetConnectedControllers(IntPtr self, [Out] ulong[] handles);

    [DllImport(Lib)]
    private static extern int SteamAPI_ISteamInput_GetInputTypeForHandle(IntPtr self, ulong handle);

    [DllImport(Lib)]
    private static extern void SteamAPI_ISteamInput_TriggerVibrationExtended(IntPtr self, ulong handle,
        ushort leftSpeed, ushort rightSpeed, ushort leftTriggerSpeed, ushort rightTriggerSpeed);

    // steam_api64.dll lives in the game's Plugins folder, not next to the exe.
    private static void SetResolver()
    {
        if (resolverSet)
        {
            return;
        }
        resolverSet = true;
        NativeLibrary.SetDllImportResolver(typeof(SteamImpulse).Assembly, ResolveSteamApi);
    }

    private static IntPtr ResolveSteamApi(string name, Assembly asm, DllImportSearchPath? path)
    {
        if (name != Lib)
        {
            return IntPtr.Zero;
        }
        string dll = Path.Combine(Application.dataPath, "Plugins", "x86_64", "steam_api64.dll");
        return NativeLibrary.TryLoad(dll, out IntPtr handle) ? handle : IntPtr.Zero;
    }

    public static bool Init()
    {
        try
        {
            SetResolver();
            steamInput = SteamAPI_SteamInput_v006();
            if (steamInput == IntPtr.Zero)
            {
                return false;
            }
            if (!SteamAPI_ISteamInput_Init(steamInput, explicitlyCallRunFrame: true))
            {
                steamInput = IntPtr.Zero;
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Mod.Log.Warning("Steam Input unavailable: " + ex.Message);
            steamInput = IntPtr.Zero;
            return false;
        }
    }

    public static void Tick(float now)
    {
        if (!Ready)
        {
            return;
        }
        SteamAPI_ISteamInput_RunFrame(steamInput, reserved: false);
        if (now < nextScan)
        {
            return;
        }
        nextScan = now + ScanInterval;

        int total = SteamAPI_ISteamInput_GetConnectedControllers(steamInput, handles);
        var found = new List<ulong>();
        var types = new System.Text.StringBuilder();
        for (int i = 0; i < total; i++)
        {
            int type = SteamAPI_ISteamInput_GetInputTypeForHandle(steamInput, handles[i]);
            types.Append(type).Append(' ');
            if (type == XBoxOneController)
            {
                found.Add(handles[i]);
            }
        }
        if (found.Count != xboxHandles.Length || total != lastTotal)
        {
            Mod.Log.Msg($"Steam Input controllers: {total} (types: {types}), Xbox One/Series: {found.Count}");
        }
        lastTotal = total;
        xboxHandles = found.ToArray();
    }

    // All levels 0..1.
    public static void Send(float leftGrip, float rightGrip, float leftTrigger, float rightTrigger)
    {
        if (!Ready)
        {
            return;
        }
        foreach (ulong handle in xboxHandles)
        {
            SteamAPI_ISteamInput_TriggerVibrationExtended(steamInput, handle,
                ToSpeed(leftGrip), ToSpeed(rightGrip), ToSpeed(leftTrigger), ToSpeed(rightTrigger));
        }
    }

    private static ushort ToSpeed(float level)
    {
        return (ushort)Mathf.RoundToInt(Mathf.Clamp01(level) * ushort.MaxValue);
    }
}
