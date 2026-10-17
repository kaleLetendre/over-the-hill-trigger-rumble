using System.Diagnostics;
using UnityEngine;

namespace OTH.ImpulseTriggers;

// Optional performance log (PerfLog = true): every Interval seconds, the game's
// frame rate and worst frame, and what the mod itself costs per frame.
internal static class Perf
{
    private const float Interval = 10f;

    public static bool On;

    private static readonly Stopwatch watch = Stopwatch.StartNew();

    private static long updateTicks;

    private static long hookTicks;

    private static int hookCalls;

    private static int steamSends;

    private static int frames;

    private static float frameTime;

    private static float worstFrame;

    private static float nextReport;

    public static long Now => watch.ElapsedTicks;

    public static void AddUpdate(long start)
    {
        updateTicks += watch.ElapsedTicks - start;
    }

    public static void AddHook(long start)
    {
        hookTicks += watch.ElapsedTicks - start;
        hookCalls++;
    }

    public static void CountSend()
    {
        steamSends++;
    }

    public static void Frame(float now)
    {
        float dt = Time.unscaledDeltaTime;
        frames++;
        frameTime += dt;
        if (dt > worstFrame)
        {
            worstFrame = dt;
        }
        if (nextReport == 0f)
        {
            nextReport = now + Interval;
        }
        if (now < nextReport)
        {
            return;
        }
        float secs = Interval + (now - nextReport);
        nextReport = now + Interval;
        if (frames > 0)
        {
            double us = 1e6 / Stopwatch.Frequency;
            Mod.Log.Msg($"perf: {frames / frameTime:0} fps, worst frame {worstFrame * 1000f:0.0} ms | " +
                        $"mod {(updateTicks + hookTicks) * us / frames:0.0} us/frame " +
                        $"(update {updateTicks * us / frames:0.0}, hooks {hookTicks * us / frames:0.0}) | " +
                        $"{hookCalls / secs:0} hook calls/s, {steamSends / secs:0} Steam sends/s");
        }
        updateTicks = hookTicks = 0;
        hookCalls = steamSends = frames = 0;
        frameTime = worstFrame = 0f;
    }
}
